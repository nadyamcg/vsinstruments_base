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
        this.AddPlaybackState<PlaybackManagerClient.PlaybackStateClient>(new PlaybackManagerClient.PlaybackStateClient(api, player as IClientPlayer));
      }
    };
    foreach (IPlayer allOnlinePlayer in ((IWorldAccessor) this.ClientAPI.World).AllOnlinePlayers)
    {
      if (!this.HasPlaybackState(allOnlinePlayer.ClientId))
      {
        this.AddPlaybackState<PlaybackManagerClient.PlaybackStateClient>(new PlaybackManagerClient.PlaybackStateClient(api, allOnlinePlayer as IClientPlayer));
      }
    }
    this.ClientAPI.Event.PlayerLeave += player =>
    {
      this.RemovePlaybackState<PlaybackManagerClient.PlaybackStateClient>(player.ClientId, out _);
    };
    ((IEventAPI) this.ClientAPI.Event).RegisterGameTickListener(new Action<float>(((PlaybackManager) this).Update), 33, 0);
  }

  public void RequestStartPlayback(string file, int channel, InstrumentType instrumentType)
  {
    this.ClientChannel.SendPacket<StartPlaybackRequest>(new StartPlaybackRequest()
    {
      File = file,
      Channel = channel,
      Instrument = instrumentType != null ? instrumentType.ID : -1
    });
    ((ICoreAPI) this.ClientAPI).Logger.Notification($"[PlaybackManagerClient] Sent playback request: file={file}, channel={channel}, instrument={instrumentType?.Name ?? "none"}");
  }

  public void RequestStopPlayback()
  {
    this.ClientChannel.SendPacket<StopPlaybackRequest>(new StopPlaybackRequest());
  }

  protected void OnStartPlaybackBroadcast(StartPlaybackBroadcast packet)
  {
    long elapsedMilliseconds = ((IWorldAccessor) this.ClientAPI.World).ElapsedMilliseconds;
    PlaybackManagerClient.PlaybackStateClient state = this.GetPlaybackState(packet.ClientId) as PlaybackManagerClient.PlaybackStateClient;
    this.ClientFileManager.RequestFile(state.Player, packet.File, (FileManager.RequestFileCallback) ((node, context) =>
    {
      double startTimeSec = (double) (((IWorldAccessor) this.ClientAPI.World).ElapsedMilliseconds - (long) context) / 1000.0;
      this.StartPlayback(state.Player.ClientId, node, packet.Channel, packet.Instrument, startTimeSec);
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
      Log.Error((ICoreAPI)this.ClientAPI, "PlaybackManagerClient",
        $"file not found in tree: '{packet.File}'");
      this.ShowPlaybackErrorMessage($"Could not find file: {Path.GetFileName(packet.File)}");
      return;
    }
    this.StartPlayback(((IPlayer)this.ClientAPI.World.Player).ClientId, node, packet.Channel, packet.Instrument);
    this.ShowPlaybackNotification($"Playing track #{packet.Channel:00} of {Path.GetFileNameWithoutExtension(packet.File)}.");
  }

  protected void OnStartPlaybackDenyOwner(StartPlaybackDenyOwner packet)
  {
    this.ShowPlaybackErrorMessage(packet.Reason.GetText());
  }

  protected void StartPlayback(
    int clientId,
    FileTree.Node node,
    int channel,
    int instrumentTypeId,
    double startTimeSec = 0.0)
  {
    if (node == null)
    {
      Log.Error((ICoreAPI)this.ClientAPI, "PlaybackManagerClient",
        $"StartPlayback called with null node for clientId={clientId}");
      this.ShowPlaybackErrorMessage("Failed to locate MIDI file.");
      return;
    }

    try
    {
      PlaybackManagerClient.PlaybackStateClient playbackState = this.GetPlaybackState(clientId) as PlaybackManagerClient.PlaybackStateClient;
      if (playbackState == null)
      {
        Log.Error((ICoreAPI)this.ClientAPI, "PlaybackManagerClient",
          $"PlaybackState is null for clientId={clientId}");
        this.ShowPlaybackErrorMessage("Player state not found.");
        return;
      }

      MidiFile midi = MidiFile.Read(node.FullPath, (ReadingSettings)null);
      InstrumentType instrumentType = InstrumentType.Find(instrumentTypeId);
      Log.Notification((ICoreAPI)this.ClientAPI, "PlaybackManagerClient",
        $"Starting playback: clientId={clientId}, file={node.Name}, instrument={instrumentType?.Name ?? "unknown"}");
      playbackState.StartPlayback(midi, instrumentType, channel, startTimeSec);
    }
    catch (Exception ex)
    {
      Log.Error((ICoreAPI)this.ClientAPI, "PlaybackManagerClient", "StartPlayback failed", ex);
      this.ShowPlaybackErrorMessage($"Playback error: {ex.Message}");
    }
  }

  protected void StopPlayback(int clientId, StopPlaybackReason reason)
  {
    var state = this.GetPlaybackState(clientId) as PlaybackManagerClient.PlaybackStateClient;
    if (state == null) return;
    state.StopPlayback();
    if (clientId != ((IPlayer) this.ClientAPI.World.Player).ClientId)
      return;
    this.ShowPlaybackNotification("Playback stopped: " + reason.GetText());
  }

  public override void Update(float deltaTime)
  {
    foreach (PlaybackManager.PlaybackStateBase playbackStateBase in this.PlaybackStates.Values)
    {
      if (playbackStateBase.IsPlaying)
      {
        playbackStateBase.Update(deltaTime);
        if (playbackStateBase.IsFinished)
          this.StopPlayback(playbackStateBase.ClientId, StopPlaybackReason.Finished);
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

  protected class PlaybackStateClient(ICoreClientAPI api, IClientPlayer player) :
    PlaybackManager.PlaybackStateBase((IPlayer) player)
  {
    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected ICoreClientAPI ClientAPI { get; private set; } = api;

    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected MidiPlayerBase MidiPlayer { get; private set; }

    public void StartPlayback(
      MidiFile midi,
      InstrumentType instrumentType,
      int channel,
      double startTime)
    {
      this.MidiPlayer = (MidiPlayerBase) new MidiPlayer((ICoreAPI) this.ClientAPI, this.Player, instrumentType);
      this.MidiPlayer.Play(midi, channel);
      this.MidiPlayer.TrySeek(startTime);
      Player.Entity.Attributes.SetBool("isPlayingInstrument", true);
    }

    public void StopPlayback()
    {
      if (this.MidiPlayer == null)
        return;
      Player.Entity.Attributes.SetBool("isPlayingInstrument", false);
      this.MidiPlayer.TryStop();
      this.MidiPlayer.Dispose();
      this.MidiPlayer = (MidiPlayerBase) null;
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
