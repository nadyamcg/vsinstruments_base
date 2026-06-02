using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Network.Playback;
using VSInstrumentsBase.src.Players;
using Melanchall.DryWetMidi.Core;
using System;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VSInstrumentsBase.src.Playback;

public class PlaybackManagerServer : PlaybackManager
{
  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected ICoreServerAPI ServerAPI { get; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected IServerNetworkChannel ServerChannel { get; private set; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected FileManagerServer ServerFileManager { get; private set; }

  public PlaybackManagerServer(ICoreServerAPI api, FileManagerServer fileManager)
    : base((ICoreAPI) api, (FileManager) fileManager)
  {
    this.ServerAPI = api;
    this.ServerChannel = api.Network.RegisterChannel("PlaybackChannel")
      .RegisterMessageType<StartPlaybackRequest>()
      .RegisterMessageType<StartPlaybackBroadcast>()
      .RegisterMessageType<StartPlaybackOwner>()
      .RegisterMessageType<StartPlaybackDenyOwner>()
      .RegisterMessageType<StopPlaybackRequest>()
      .RegisterMessageType<StopPlaybackBroadcast>()
      .SetMessageHandler<StartPlaybackRequest>(OnStartPlaybackRequest)
      .SetMessageHandler<StopPlaybackRequest>(OnStopPlaybackRequest);
    this.ServerFileManager = fileManager;
    this.ServerAPI.Event.PlayerJoin += player =>
    {
      if (!this.HasPlaybackState(player.ClientId))
      {
        this.AddPlaybackState<PlaybackStateServer>(new PlaybackStateServer(api, player));
      }
    };
    this.ServerAPI.Event.PlayerLeave += player =>
    {
      this.RemovePlaybackState<PlaybackStateServer>(player.ClientId, out _);
    };
    this.ServerAPI.Event.PlayerDeath += (player, damageSource) =>
    {
      if (this.HasPlaybackState(player.ClientId) && this.GetPlaybackState(player.ClientId).IsPlaying)
      {
        this.StopPlayback(player.ClientId, StopPlaybackReason.Died);
      }
    };
    ((IEventAPI) this.ServerAPI.Event).RegisterGameTickListener(new Action<float>(((PlaybackManager) this).Update), 33, 0);
  }

  // negative deterministic slot id per block position. avoids collision with positive player client ids.
  public static int BlockPosToSlotId(int x, int y, int z)
  {
    unchecked
    {
      uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(z * 83492791);
      return (int)(0x80000000u | (h & 0x7FFFFFFFu));
    }
  }

  protected void OnStartPlaybackRequest(IServerPlayer source, StartPlaybackRequest packet)
  {
    ((ICoreAPI) this.ServerAPI).Logger.Notification($"[PlaybackManagerServer] Received playback request from {((IPlayer) source).PlayerName}: file={packet.File}, channel={packet.Channel}, instrument={packet.Instrument}, block={packet.IsBlockSource}");
    if (!ValidatePlaybackRequest(source, packet))
      return;
    string bandName = packet.BandName ?? "";
    bool isBlock = packet.IsBlockSource;
    int bx = packet.BlockX, by = packet.BlockY, bz = packet.BlockZ;
    this.ServerFileManager.RequestFile((IPlayer) source, packet.File, (FileManager.RequestFileCallback) ((node, context) =>
      this.StartPlayback(source, packet.File, node, packet.Channel, packet.Instrument, bandName, isBlock, bx, by, bz)));
  }

  protected void StartPlayback(
    IServerPlayer source,
    string sourceFile,
    FileTree.Node serverFile,
    int channel,
    int instrumentType,
    string bandName,
    bool isBlockSource,
    int blockX,
    int blockY,
    int blockZ)
  {
    int slotId = isBlockSource
      ? BlockPosToSlotId(blockX, blockY, blockZ)
      : ((IPlayer) source).ClientId;

    PlaybackStateServer playbackState = this.GetPlaybackState(slotId) as PlaybackStateServer;
    if (playbackState == null)
    {
      // first time we see this block. lazy-create its slot.
      playbackState = new PlaybackStateServer(this.ServerAPI, slotId);
      this.AddPlaybackState<PlaybackStateServer>(playbackState);
    }

    long now = ((IWorldAccessor) this.ServerAPI.World).ElapsedMilliseconds;
    if (now - playbackState.LastRequestTime < 1000L)
    {
      if (!isBlockSource)
        this.ServerChannel.SendPacket(new StartPlaybackDenyOwner() { Reason = DenyPlaybackReason.TooManyRequests }, [source]);
      return;
    }
    playbackState.BumpLastRequestTime();

    if (playbackState.IsPlaying)
    {
      if (!isBlockSource)
        this.ServerChannel.SendPacket(new StartPlaybackDenyOwner() { Reason = DenyPlaybackReason.OperationInProgress }, [source]);
      return;
    }

    double durationSeconds;
    try
    {
      durationSeconds = MidiFile.Read(serverFile.FullPath, (ReadingSettings) null).ReadTrackDuration(channel);
    }
    catch (Exception ex)
    {
      ((ICoreAPI) this.ServerAPI).Logger.Error($"[PlaybackManagerServer] Failed to parse MIDI file {serverFile.FullPath}: {ex.Message}");
      if (!isBlockSource)
        this.ServerChannel.SendPacket(new StartPlaybackDenyOwner() { Reason = DenyPlaybackReason.InvalidFile }, [source]);
      return;
    }

    double bandOffsetSec = this.GetBandOffsetSec(bandName);

    var broadcast = new StartPlaybackBroadcast()
    {
      ClientId = slotId,
      Channel = channel,
      File = serverFile.RelativePath,
      Instrument = instrumentType,
      StartTimeOffsetSec = bandOffsetSec,
      IsBlockSource = isBlockSource,
      BlockX = blockX,
      BlockY = blockY,
      BlockZ = blockZ
    };

    if (isBlockSource)
    {
      // block playback: everyone (including activator) gets the broadcast.
      this.ServerChannel.BroadcastPacket(broadcast, []);
    }
    else
    {
      // player playback: broadcast to others, send owner packet to activator.
      this.ServerChannel.BroadcastPacket(broadcast, [source]);
      this.ServerChannel.SendPacket(new StartPlaybackOwner()
      {
        Channel = channel,
        File = sourceFile,
        Instrument = instrumentType,
        StartTimeOffsetSec = bandOffsetSec
      }, [source]);
    }

    ((ICoreAPI) this.ServerAPI).Logger.Notification($"[PlaybackManagerServer] Broadcasting playback (slot={slotId}, block={isBlockSource}, band='{bandName}', offset={bandOffsetSec:0.000}s)");
    playbackState.StartPlayback(durationSeconds, bandName, bandOffsetSec);
  }

  // returns the song-time offset (seconds) a new joiner should seek to.
  // if a band with this name is already playing, sync to its elapsed time.
  // empty band name means solo, no sync.
  protected double GetBandOffsetSec(string bandName)
  {
    if (string.IsNullOrEmpty(bandName))
      return 0.0;
    long now = ((IWorldAccessor) this.ServerAPI.World).ElapsedMilliseconds;
    foreach (var s in this.PlaybackStates.Values)
    {
      if (s is PlaybackStateServer pss && pss.IsPlaying && pss.BandName == bandName)
        return (double)(now - pss.PlaybackStartTime) / 1000.0;
    }
    return 0.0;
  }

  protected static bool ValidatePlaybackRequest(IServerPlayer source, StartPlaybackRequest packet)
  {
    return true;
  }

  protected void OnStopPlaybackRequest(IServerPlayer source, StopPlaybackRequest packet)
  {
    if (!(this.GetPlaybackState(((IPlayer) source).ClientId) is PlaybackStateServer state) || !state.IsPlaying)
      return;
    this.StopPlayback(((IPlayer) source).ClientId, StopPlaybackReason.Cancelled);
  }

  public void StopPlayback(int slotId, StopPlaybackReason reason)
  {
    if (!(this.GetPlaybackState(slotId) is PlaybackStateServer state) || !state.IsPlaying)
      return;
    this.ServerChannel.BroadcastPacket(new StopPlaybackBroadcast()
    {
      ClientId = slotId,
      Reason = reason
    }, []);
    state.StopPlayback();
  }

  public override void Update(float deltaTime)
  {
    foreach (var s in this.PlaybackStates.Values)
    {
      if (s.IsPlaying)
      {
        s.Update(deltaTime);
        if (s.IsFinished)
          ((PlaybackStateServer) s).StopPlayback();
      }
    }
  }

  protected class PlaybackStateServer : PlaybackStateBase
  {
    private long _lastRequestTime = 0;
    private bool _isPlaying = false;
    private long _finishTime;
    private long _startTime;
    private string _bandName = "";

    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected ICoreServerAPI ServerAPI { get; private set; }

    public PlaybackStateServer(ICoreServerAPI api, IServerPlayer player) : base((IPlayer) player)
    {
      this.ServerAPI = api;
    }

    // block-source slot: no player attached.
    public PlaybackStateServer(ICoreServerAPI api, int slotId) : base(slotId)
    {
      this.ServerAPI = api;
    }

    public string BandName => this._bandName;

    // server-time (ms) the band's playhead was at song-time 0.
    public long PlaybackStartTime => this._startTime;

    public void StartPlayback(double durationSeconds, string bandName = "", double bandOffsetSec = 0.0)
    {
      this._isPlaying = true;
      long now = ((IWorldAccessor) this.ServerAPI.World).ElapsedMilliseconds;
      this._startTime = now - (long)(bandOffsetSec * 1000.0);
      this._finishTime = now + (long)((durationSeconds - bandOffsetSec) * 1000.0);
      this._bandName = bandName ?? "";
    }

    public void StopPlayback()
    {
      this._isPlaying = false;
      this._finishTime = 0L;
      this._bandName = "";
    }

    public override bool IsPlaying => this._isPlaying;

    public override bool IsFinished
      => this._isPlaying && ((IWorldAccessor) this.ServerAPI.World).ElapsedMilliseconds >= this._finishTime;

    public long LastRequestTime => this._lastRequestTime;

    public void BumpLastRequestTime()
      => this._lastRequestTime = ((IWorldAccessor) this.ServerAPI.World).ElapsedMilliseconds;
  }
}
