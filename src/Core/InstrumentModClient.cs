using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using VSInstrumentsBase.src;
using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Network.Packets;
using VSInstrumentsBase.src.Network.Playback;
using VSInstrumentsBase.src.Playback;
using VSInstrumentsBase.src.Types;

namespace VSInstrumentsBase.src.Core;

public class InstrumentModClient : InstrumentModBase
{
  private ICoreClientAPI clientApi;
  private bool clientSideEnable;
  private bool clientSideReady;
  private long listenerIDClient = -1;
  private FileManagerClient _fileManager;
  private PlaybackManagerClient _playbackManager;
  private LoadedTrack _loadedTrack;

  // a track the player has picked but not started yet. holding it here rather
  // than on the itemstack means swapping hotbar slots drops it, which is the
  // intended way to back out of a selection.
  public sealed class LoadedTrack
  {
    public string RelativePath { get; init; }
    public string DisplayName { get; init; }
    public int TrackIndex { get; init; }
    // set once the server confirms it holds a usable copy of the file.
    public bool ServerReady { get; set; }
  }

  public LoadedTrack CurrentTrack => _loadedTrack;

  // called by the song select dialog. stores the pick and kicks off the server
  // side transfer so the file is in place before playback is ever requested.
  public void LoadTrack(string relativePath, string displayName, int trackIndex)
  {
    string name = System.IO.Path.GetFileNameWithoutExtension(displayName ?? relativePath ?? "");

    if (IsAlreadyLoaded(relativePath, trackIndex))
    {
      _playbackManager?.ShowPlaybackNotification($"Track #{trackIndex:00} of {name} is already loaded.");
      return;
    }

    // a different pick replaces the old one outright.
    _loadedTrack = new LoadedTrack
    {
      RelativePath = relativePath,
      DisplayName = displayName,
      TrackIndex = trackIndex
    };

    _playbackManager?.ShowPlaybackNotification($"Loaded track #{trackIndex:00} of {name}. Right-click to play.");
    _playbackManager?.RequestPreload(relativePath, trackIndex);
  }

  private bool IsAlreadyLoaded(string relativePath, int trackIndex)
  {
    return _loadedTrack != null
      && _loadedTrack.TrackIndex == trackIndex
      && string.Equals(_loadedTrack.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase);
  }

  public void OnTrackPreloadResult(string file, int channel, bool ready, DenyPlaybackReason reason)
  {
    // a stale reply for a track that has since been swapped out or dropped.
    if (_loadedTrack == null || _loadedTrack.RelativePath != file || _loadedTrack.TrackIndex != channel)
      return;

    if (ready)
    {
      _loadedTrack.ServerReady = true;
      return;
    }

    if (reason == DenyPlaybackReason.TooManyRequests)
    {
      _playbackManager?.ShowPlaybackNotification("Track loaded, but the server is still busy with a previous request. It will be sent when you play.");
      return;
    }

    // the file will not play, so do not leave the player holding a dud.
    _loadedTrack = null;
    _playbackManager?.ShowPlaybackErrorMessage($"Could not load track: {reason.GetText()}");
  }

  // returns true if a track was actually cleared, so callers can decide whether
  // the unload is worth telling the player about.
  public bool ClearLoadedTrack()
  {
    if (_loadedTrack == null)
      return false;
    _loadedTrack = null;
    return true;
  }

  public override bool ShouldLoad(EnumAppSide side)
  {
    return side == EnumAppSide.Client;
  }

  public override FileManager FileManager => _fileManager;

  public override PlaybackManager PlaybackManager => _playbackManager;

  public override void StartClientSide(ICoreClientAPI api)
  {
    clientApi = api;
    api.Logger.Notification("[InstrumentModClient] Starting client-side initialization");

    Definitions.Reset();
    api.Logger.Debug("[InstrumentModClient] Definitions reset");

    _fileManager = new FileManagerClient(api, InstrumentModSettings.Instance);
    api.Logger.Debug("[InstrumentModClient] FileManager created");

    _playbackManager = new PlaybackManagerClient(api, _fileManager);
    api.Logger.Debug("[InstrumentModClient] PlaybackManager created");

    api.Event.AfterActiveSlotChanged += OnAfterActiveSlotChanged;
    api.Logger.Debug("[InstrumentModClient] Active slot listener registered");

    try
    {
      clientApi.Network
        .RegisterChannel(Constants.Channel.MusicBlock)
        .RegisterMessageType<MusicBlockPlayRequest>()
        .SetMessageHandler<MusicBlockPlayRequest>(OnMusicBlockPlayRequest);

      api.Logger.Debug("[InstrumentModClient] MusicBlock channel registered");
    }
    catch (Exception ex)
    {
      api.Logger.Warning("[InstrumentModClient] Failed to register MusicBlock channel: " + ex.Message);
    }

    try
    {
      clientApi.ChatCommands
        .Create("instruments")
        .WithDescription("instrument playback commands")
        .WithArgs(clientApi.ChatCommands.Parsers.OptionalWord("enable|disable"))
        .HandleWith(ParseClientCommand);

      api.Logger.Debug("[InstrumentModClient] Chat command registered");
    }
    catch (Exception ex)
    {
      api.Logger.Warning("[InstrumentModClient] Failed to register chat command: " + ex.Message);
    }

    clientSideEnable = true;
    clientSideReady = true;
    api.Logger.Notification("[InstrumentModClient] Client-side initialization complete!");
  }

  // stops the local player's performance when they select a different hotbar slot.
  private void OnAfterActiveSlotChanged(ActiveSlotChangeEventArgs args)
  {
    if (args == null || args.FromSlot == args.ToSlot)
      return;

    IClientPlayer player = clientApi?.World?.Player;
    if (player == null || _playbackManager == null)
      return;

    // leaving the slot also drops any track waiting to be played
    if (ClearLoadedTrack())
      _playbackManager.ShowPlaybackNotification("Unloaded track.");

    if (!_playbackManager.IsPlaying(player.ClientId))
      return;

    _playbackManager.RequestStopPlayback();
  }

  public override void Dispose()
  {
    base.Dispose();
    if (clientApi != null)
      clientApi.Event.AfterActiveSlotChanged -= OnAfterActiveSlotChanged;
    _playbackManager?.UnregisterRenderer();
    if (listenerIDClient != -1)
    {
      clientApi.Event.UnregisterGameTickListener(listenerIDClient);
      listenerIDClient = 0;
    }
    clientSideReady = false;
  }

  private void OnMusicBlockPlayRequest(MusicBlockPlayRequest packet)
  {
    if (!clientSideEnable || !clientSideReady)
    {
      clientApi.Logger.Notification("[InstrumentModClient] Ignoring MusicBlock request: client not enabled/ready.");
      return;
    }

    clientApi.Logger.Notification("[InstrumentModClient] Received MusicBlock play request: " + packet.SongPath);

    InstrumentType instrumentType = InstrumentType.Find(packet.InstrumentId);
    if (instrumentType == null)
    {
      clientApi.Logger.Error($"[InstrumentModClient] Invalid instrument ID: {packet.InstrumentId}");
      return;
    }

    var blockPos = new Vintagestory.API.MathTools.BlockPos(packet.BlockX, packet.BlockY, packet.BlockZ);
    _playbackManager.RequestStartPlayback(packet.SongPath, packet.Channel, instrumentType, packet.BandName ?? "", blockPos);
  }

  private TextCommandResult ParseClientCommand(TextCommandCallingArgs args)
  {
    string command = args.Parsers[0].GetValue()?.ToString() ?? "";

    switch (command)
    {
      case "enable":
        clientSideEnable = true;
        return TextCommandResult.Success("MIDI playback enabled!");

      case "disable":
        clientSideEnable = false;
        return TextCommandResult.Success("MIDI playback disabled!");

      default:
        return TextCommandResult.Success("Syntax: .instruments [enable|disable]");
    }
  }
}
