using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Network.Playback;
using VSInstrumentsBase.src.Players;
using VSInstrumentsBase.src.Types;
using VSInstrumentsBase.src.Utils;
using Melanchall.DryWetMidi.Core;
using System;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;


namespace VSInstrumentsBase.src.Playback;

public class PlaybackManagerClient : PlaybackManager
{
  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected ICoreClientAPI ClientAPI { get; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected IClientNetworkChannel ClientChannel { get; private set; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected FileManagerClient ClientFileManager { get; private set; }

  public PlaybackManagerClient(ICoreClientAPI api, FileManagerClient fileManager)
    : base((ICoreAPI) api, (FileManager) fileManager)
  {
    this.ClientAPI = api;
    this.ClientChannel = api.Network.RegisterChannel("PlaybackChannel")
      .RegisterMessageType<StartPlaybackRequest>()
      .RegisterMessageType<StartPlaybackBroadcast>()
      .RegisterMessageType<StartPlaybackOwner>()
      .RegisterMessageType<StartPlaybackDenyOwner>()
      .RegisterMessageType<StopPlaybackRequest>()
      .RegisterMessageType<StopPlaybackBroadcast>()
      .SetMessageHandler<StartPlaybackBroadcast>(OnStartPlaybackBroadcast)
      .SetMessageHandler<StartPlaybackOwner>(OnStartPlaybackOwner)
      .SetMessageHandler<StartPlaybackDenyOwner>(OnStartPlaybackDenyOwner)
      .SetMessageHandler<StopPlaybackBroadcast>(OnStopPlaybackBroadcast);
    this.ClientFileManager = fileManager;
    this.ClientAPI.Event.PlayerJoin += player =>
    {
      if (!this.HasPlaybackState(player.ClientId))
      {
        this.AddPlaybackState<PlaybackStateClient>(new PlaybackStateClient(api, player as IClientPlayer));
      }
    };
    foreach (IPlayer p in ((IWorldAccessor) this.ClientAPI.World).AllOnlinePlayers)
    {
      if (!this.HasPlaybackState(p.ClientId))
      {
        this.AddPlaybackState<PlaybackStateClient>(new PlaybackStateClient(api, p as IClientPlayer));
      }
    }
    this.ClientAPI.Event.PlayerLeave += player =>
    {
      this.RemovePlaybackState<PlaybackStateClient>(player.ClientId, out _);
    };
    ((IEventAPI) this.ClientAPI.Event).RegisterGameTickListener(new Action<float>(((PlaybackManager) this).Update), 33, 0);
  }

  public void RequestStartPlayback(string file, int channel, InstrumentType instrumentType, string bandName = "", BlockPos blockPos = null)
  {
    var packet = new StartPlaybackRequest()
    {
      File = file,
      Channel = channel,
      Instrument = instrumentType != null ? instrumentType.ID : -1,
      BandName = bandName ?? "",
      IsBlockSource = blockPos != null,
      BlockX = blockPos?.X ?? 0,
      BlockY = blockPos?.Y ?? 0,
      BlockZ = blockPos?.Z ?? 0
    };
    this.ClientChannel.SendPacket(packet);
    ((ICoreAPI) this.ClientAPI).Logger.Notification($"[PlaybackManagerClient] Sent playback request: file={file}, channel={channel}, instrument={instrumentType?.Name ?? "none"}, band={bandName ?? ""}, block={packet.IsBlockSource}");
  }

  public void RequestStopPlayback()
  {
    this.ClientChannel.SendPacket(new StopPlaybackRequest());
  }

  // resolves the playback state for a given slot id, lazy-creating a block-slot if needed.
  protected PlaybackStateClient GetOrCreateBlockState(int slotId, int bx, int by, int bz)
  {
    if (this.GetPlaybackState(slotId) is PlaybackStateClient s)
      return s;
    var blockState = new PlaybackStateClient(this.ClientAPI, slotId, new Vec3f(bx + 0.5f, by + 0.5f, bz + 0.5f));
    this.AddPlaybackState<PlaybackStateClient>(blockState);
    return blockState;
  }

  protected void OnStartPlaybackBroadcast(StartPlaybackBroadcast packet)
  {
    long elapsedMilliseconds = ((IWorldAccessor) this.ClientAPI.World).ElapsedMilliseconds;

    PlaybackStateClient state;
    if (packet.IsBlockSource)
      state = this.GetOrCreateBlockState(packet.ClientId, packet.BlockX, packet.BlockY, packet.BlockZ);
    else
      state = this.GetPlaybackState(packet.ClientId) as PlaybackStateClient;

    if (state == null)
    {
      Log.Error((ICoreAPI) this.ClientAPI, "PlaybackManagerClient", $"no playback state for slot {packet.ClientId}");
      return;
    }

    // block-source slots have no Player; fall back to the local client player for the file lookup.
    IPlayer fileRequestPlayer = state.Player ?? (IPlayer) this.ClientAPI.World.Player;
    this.ClientFileManager.RequestFile(fileRequestPlayer, packet.File, (FileManager.RequestFileCallback) ((node, context) =>
    {
      double startTimeSec = (double) (((IWorldAccessor) this.ClientAPI.World).ElapsedMilliseconds - (long) context) / 1000.0 + packet.StartTimeOffsetSec;
      this.StartPlayback(state.ClientId, node, packet.Channel, packet.Instrument, startTimeSec);
    }), (object) elapsedMilliseconds);
  }

  protected void OnStopPlaybackBroadcast(StopPlaybackBroadcast packet)
  {
    this.StopPlayback(packet.ClientId, packet.Reason);
  }

  protected void OnStartPlaybackOwner(StartPlaybackOwner packet)
  {
    var node = this.ClientFileManager.UserTree.Find(packet.File);
    if (node == null)
    {
      Log.Error((ICoreAPI)this.ClientAPI, "PlaybackManagerClient", $"file not found in tree: '{packet.File}'");
      this.ShowPlaybackErrorMessage($"Could not find file: {Path.GetFileName(packet.File)}");
      return;
    }
    this.StartPlayback(((IPlayer)this.ClientAPI.World.Player).ClientId, node, packet.Channel, packet.Instrument, packet.StartTimeOffsetSec);
    this.ShowPlaybackNotification($"Playing track #{packet.Channel:00} of {Path.GetFileNameWithoutExtension(packet.File)}.");
  }

  protected void OnStartPlaybackDenyOwner(StartPlaybackDenyOwner packet)
  {
    this.ShowPlaybackErrorMessage(packet.Reason.GetText());
  }

  protected void StartPlayback(
    int slotId,
    FileTree.Node node,
    int channel,
    int instrumentTypeId,
    double startTimeSec = 0.0)
  {
    if (node == null)
    {
      Log.Error((ICoreAPI)this.ClientAPI, "PlaybackManagerClient", $"StartPlayback called with null node for slot={slotId}");
      this.ShowPlaybackErrorMessage("Failed to locate MIDI file.");
      return;
    }

    try
    {
      if (!(this.GetPlaybackState(slotId) is PlaybackStateClient state))
      {
        Log.Error((ICoreAPI)this.ClientAPI, "PlaybackManagerClient", $"PlaybackState is null for slot={slotId}");
        this.ShowPlaybackErrorMessage("Player state not found.");
        return;
      }

      MidiFile midi = MidiFile.Read(node.FullPath, (ReadingSettings)null);
      InstrumentType instrumentType = InstrumentType.Find(instrumentTypeId);
      Log.Notification((ICoreAPI)this.ClientAPI, "PlaybackManagerClient", $"Starting playback: slot={slotId}, file={node.Name}, instrument={instrumentType?.Name ?? "unknown"}");
      state.StartPlayback(midi, instrumentType, channel, startTimeSec);
    }
    catch (Exception ex)
    {
      Log.Error((ICoreAPI)this.ClientAPI, "PlaybackManagerClient", "StartPlayback failed", ex);
      this.ShowPlaybackErrorMessage($"Playback error: {ex.Message}");
    }
  }

  protected void StopPlayback(int slotId, StopPlaybackReason reason)
  {
    if (!(this.GetPlaybackState(slotId) is PlaybackStateClient state))
      return;
    state.StopPlayback();
    if (slotId != ((IPlayer) this.ClientAPI.World.Player).ClientId)
      return;
    this.ShowPlaybackNotification("Playback stopped: " + reason.GetText());
  }

  public override void Update(float deltaTime)
  {
    foreach (var s in this.PlaybackStates.Values)
    {
      if (s.IsPlaying)
      {
        s.Update(deltaTime);
        if (s.IsFinished)
          this.StopPlayback(s.ClientId, StopPlaybackReason.Finished);
      }
    }
  }

  protected void ShowPlaybackErrorMessage(string reason)
  {
    this.ClientAPI.ShowChatMessage("Instruments playback failed: " + reason);
  }

  protected void ShowPlaybackNotification(string message)
  {
    this.ClientAPI.ShowChatMessage("Instruments: " + message);
  }

  protected class PlaybackStateClient : PlaybackStateBase
  {
    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected ICoreClientAPI ClientAPI { get; private set; }

    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected MidiPlayerBase MidiPlayer { get; private set; }

    // when set, this state belongs to a music block and sound is emitted from this position.
    private readonly Vec3f _blockSourcePos;

    public PlaybackStateClient(ICoreClientAPI api, IClientPlayer player) : base((IPlayer) player)
    {
      this.ClientAPI = api;
      this._blockSourcePos = null;
    }

    public PlaybackStateClient(ICoreClientAPI api, int slotId, Vec3f blockSourcePos) : base(slotId)
    {
      this.ClientAPI = api;
      this._blockSourcePos = blockSourcePos;
    }

    public void StartPlayback(MidiFile midi, InstrumentType instrumentType, int channel, double startTime)
    {
      this.MidiPlayer = new MidiPlayer((ICoreAPI) this.ClientAPI, this.Player, instrumentType, this._blockSourcePos);
      this.MidiPlayer.Play(midi, channel);
      this.MidiPlayer.TrySeek(startTime);
      if (this.Player != null)
        this.Player.Entity.Attributes.SetBool("isPlayingInstrument", true);
    }

    public void StopPlayback()
    {
      if (this.MidiPlayer == null)
        return;
      if (this.Player != null)
        this.Player.Entity.Attributes.SetBool("isPlayingInstrument", false);
      this.MidiPlayer.TryStop();
      this.MidiPlayer.Dispose();
      this.MidiPlayer = null;
    }

    public override bool IsPlaying => this.MidiPlayer != null && this.MidiPlayer.IsPlaying;

    public override bool IsFinished => this.MidiPlayer != null && this.MidiPlayer.IsFinished;

    public override void Update(float deltaTime)
    {
      if (this.MidiPlayer.IsFinished)
        return;
      this.MidiPlayer.Update(deltaTime);
    }
  }
}
