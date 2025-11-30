using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using VSInstrumentsBase.src;
using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Network.Packets;
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

    try
    {
      clientApi.Network
        .GetChannel(Constants.Channel.MusicBlock)
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

  public override void Dispose()
  {
    base.Dispose();
    if (listenerIDClient != -1)
    {
      clientApi.Event.UnregisterGameTickListener(listenerIDClient);
      listenerIDClient = 0;
    }
    clientSideReady = false;
  }

  private void OnMusicBlockPlayRequest(MusicBlockPlayRequest packet)
  {
    clientApi.Logger.Notification("[InstrumentModClient] Received MusicBlock play request: " + packet.SongPath);

    InstrumentType instrumentType = InstrumentType.Find(packet.InstrumentId);
    if (instrumentType == null)
    {
      clientApi.Logger.Error($"[InstrumentModClient] Invalid instrument ID: {packet.InstrumentId}");
      return;
    }

    _playbackManager.RequestStartPlayback(packet.SongPath, packet.Channel, instrumentType);
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
