using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Network.Playback;
using VSInstrumentsBase.src.Players;
using Melanchall.DryWetMidi.Core;
using System;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using VSInstrumentsBase.src.Files;


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
        this.AddPlaybackState<PlaybackManagerServer.PlaybackStateServer>(new PlaybackManagerServer.PlaybackStateServer(api, player));
      }
    };
    this.ServerAPI.Event.PlayerLeave += player =>
    {
      this.RemovePlaybackState<PlaybackManagerServer.PlaybackStateServer>(player.ClientId, out _);
    };
    this.ServerAPI.Event.PlayerDeath += (player, damageSource) =>
    {
      if (this.HasPlaybackState(player.ClientId) && this.GetPlaybackState(player.ClientId).IsPlaying)
      {
        this.StopPlayback(player.ClientId, StopPlaybackReason.Died);
      }
    };
    this.ServerAPI.Event.AfterActiveSlotChanged += (player, args) =>
    {
      if (this.HasPlaybackState(player.ClientId) && this.GetPlaybackState(player.ClientId).IsPlaying)
      {
        this.StopPlayback(player.ClientId, StopPlaybackReason.ChangedSlot);
      }
    };
    ((IEventAPI) this.ServerAPI.Event).RegisterGameTickListener(new Action<float>(((PlaybackManager) this).Update), 33, 0);
  }

  protected void OnStartPlaybackRequest(IServerPlayer source, StartPlaybackRequest packet)
  {
    ((ICoreAPI) this.ServerAPI).Logger.Notification($"[PlaybackManagerServer] Received playback request from {((IPlayer) source).PlayerName}: file={packet.File}, channel={packet.Channel}, instrument={packet.Instrument}");
    if (!PlaybackManagerServer.ValidatePlaybackRequest(source, packet))
      return;
    this.ServerFileManager.RequestFile((IPlayer) source, packet.File, (FileManager.RequestFileCallback) ((node, context) => this.StartPlayback(source, packet.File, node, packet.Channel, packet.Instrument)));
  }

  protected void StartPlayback(
    IServerPlayer source,
    string sourceFile,
    FileTree.Node serverFile,
    int channel,
    int instrumentType)
  {
    PlaybackManagerServer.PlaybackStateServer playbackState = this.GetPlaybackState(((IPlayer) source).ClientId) as PlaybackManagerServer.PlaybackStateServer;
    if (((IWorldAccessor) this.ServerAPI.World).ElapsedMilliseconds - playbackState.LastRequestTime < 1000L)
    {
      this.ServerChannel.SendPacket<StartPlaybackDenyOwner>(new StartPlaybackDenyOwner()
      {
        Reason = DenyPlaybackReason.TooManyRequests
      }, new IServerPlayer[1]{ source });
    }
    else
    {
      playbackState.BumpLastRequestTime();
      if (playbackState.IsPlaying)
      {
        this.ServerChannel.SendPacket<StartPlaybackDenyOwner>(new StartPlaybackDenyOwner()
        {
          Reason = DenyPlaybackReason.OperationInProgress
        }, new IServerPlayer[1]{ source });
      }
      else
      {
        double durationSeconds = 0.0;
        bool flag;
        try
        {
          durationSeconds = MidiFile.Read(serverFile.FullPath, (ReadingSettings) null).ReadTrackDuration(channel);
          flag = true;
        }
        catch
        {
          flag = false;
        }
        if (!flag)
        {
          this.ServerChannel.SendPacket<StartPlaybackDenyOwner>(new StartPlaybackDenyOwner()
          {
            Reason = DenyPlaybackReason.InvalidFile
          }, new IServerPlayer[1]{ source });
        }
        else
        {
          this.ServerChannel.BroadcastPacket<StartPlaybackBroadcast>(new StartPlaybackBroadcast()
          {
            ClientId = ((IPlayer) source).ClientId,
            Channel = channel,
            File = serverFile.RelativePath,
            Instrument = instrumentType
          }, new IServerPlayer[1]{ source });
          ((ICoreAPI) this.ServerAPI).Logger.Notification("[PlaybackManagerServer] Broadcasting playback to other players");
          this.ServerChannel.SendPacket<StartPlaybackOwner>(new StartPlaybackOwner()
          {
            Channel = channel,
            File = sourceFile,
            Instrument = instrumentType
          }, new IServerPlayer[1]{ source });
          playbackState.StartPlayback(durationSeconds);
        }
      }
    }
  }

  protected static bool ValidatePlaybackRequest(IServerPlayer source, StartPlaybackRequest packet)
  {
    return true;
  }

  protected void OnStopPlaybackRequest(IServerPlayer source, StopPlaybackRequest packet)
  {
    if (!(this.GetPlaybackState(((IPlayer) source).ClientId) as PlaybackManagerServer.PlaybackStateServer).IsPlaying)
      return;
    this.StopPlayback(((IPlayer) source).ClientId, StopPlaybackReason.Cancelled);
  }

  protected void StopPlayback(int clientId, StopPlaybackReason reason)
  {
    PlaybackManagerServer.PlaybackStateServer playbackState = this.GetPlaybackState(clientId) as PlaybackManagerServer.PlaybackStateServer;
    Debug.Assert(playbackState.IsPlaying);
    this.ServerChannel.BroadcastPacket<StopPlaybackBroadcast>(new StopPlaybackBroadcast()
    {
      ClientId = clientId,
      Reason = reason
    }, Array.Empty<IServerPlayer>());
    playbackState.StopPlayback();
  }

  public override void Update(float deltaTime)
  {
    foreach (PlaybackManager.PlaybackStateBase playbackStateBase in this.PlaybackStates.Values)
    {
      if (playbackStateBase.IsPlaying)
      {
        playbackStateBase.Update(deltaTime);
        if (playbackStateBase.IsFinished)
          ((PlaybackManagerServer.PlaybackStateServer) playbackStateBase).StopPlayback();
      }
    }
  }

  protected class PlaybackStateServer(ICoreServerAPI api, IServerPlayer player) : 
    PlaybackManager.PlaybackStateBase((IPlayer) player)
  {
    private long _lastRequestTime = 0;
    private bool _isPlaying = false;
    private long _finishTime;

    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected ICoreServerAPI ServerAPI { get; private set; } = api;

    public void StartPlayback(double durationSeconds)
    {
      this._isPlaying = true;
      this._finishTime = ((IWorldAccessor) this.ServerAPI.World).ElapsedMilliseconds + (long) (durationSeconds * 1000.0);
    }

    public void StopPlayback()
    {
      this._isPlaying = false;
      this._finishTime = 0L;
    }

    public override bool IsPlaying => this._isPlaying;

    public override bool IsFinished
    {
      get
      {
        return this._isPlaying && ((IWorldAccessor) this.ServerAPI.World).ElapsedMilliseconds >= this._finishTime;
      }
    }

    public long LastRequestTime => this._lastRequestTime;

    public void BumpLastRequestTime()
    {
      this._lastRequestTime = ((IWorldAccessor) this.ServerAPI.World).ElapsedMilliseconds;
    }
  }
}
