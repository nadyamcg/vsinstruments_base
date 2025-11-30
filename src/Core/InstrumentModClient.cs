using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Network.Packets;
using VSInstrumentsBase.src.Playback;
using VSInstrumentsBase.src.Types;
using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VSInstrumentsBase.src.Core;

public class InstrumentModClient : InstrumentModBase
{
  private ICoreClientAPI clientApi;
  private bool clientSideEnable;
  private bool clientSideReady = false;
  private long listenerIDClient = -1;
  private FileManagerClient _fileManager;
  private PlaybackManagerClient _playbackManager;

  public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

  public override FileManager FileManager => this._fileManager;

  public override PlaybackManager PlaybackManager => this._playbackManager;

  public override void StartClientSide(ICoreClientAPI api)
  {
    this.clientApi = api;
    api.Logger.Notification("[InstrumentModClient] Starting client-side initialization");
    Definitions.Reset();
    api.Logger.Debug("[InstrumentModClient] Definitions reset");
    this._fileManager = new FileManagerClient(api, InstrumentModSettings.Instance);
    api.Logger.Debug("[InstrumentModClient] FileManager created");
    this._playbackManager = new PlaybackManagerClient(api, this._fileManager);
    api.Logger.Debug("[InstrumentModClient] PlaybackManager created");
    try
    {
      this.clientApi.Network.GetChannel("instrumentsMusicBlock").RegisterMessageType<MusicBlockPlayRequest>().SetMessageHandler<MusicBlockPlayRequest>(OnMusicBlockPlayRequest);
      api.Logger.Debug("[InstrumentModClient] MusicBlock channel registered");
    }
    catch (Exception ex)
    {
      api.Logger.Warning("[InstrumentModClient] Failed to register MusicBlock channel: " + ex.Message);
    }
    try
    {
      this.clientApi.ChatCommands.Create("instruments").WithDescription("instrument playback commands").WithArgs(new ICommandArgumentParser[1]
      {
        this.clientApi.ChatCommands.Parsers.OptionalWord("enable|disable")
      }).HandleWith(ParseClientCommand);
      api.Logger.Debug("[InstrumentModClient] Chat command registered");
    }
    catch (Exception ex)
    {
      api.Logger.Warning("[InstrumentModClient] Failed to register chat command: " + ex.Message);
    }
    this.clientSideEnable = true;
    this.clientSideReady = true;
    api.Logger.Notification("[InstrumentModClient] Client-side initialization complete!");
  }

  public override void Dispose()
  {
    base.Dispose();
    if (this.listenerIDClient != -1L)
    {
      ((IEventAPI) this.clientApi.Event).UnregisterGameTickListener(this.listenerIDClient);
      this.listenerIDClient = 0L;
    }
    this.clientSideReady = false;
  }

  private void OnMusicBlockPlayRequest(MusicBlockPlayRequest packet)
  {
    ((ICoreAPI) this.clientApi).Logger.Notification("[InstrumentModClient] Received MusicBlock play request: " + packet.SongPath);
    InstrumentType instrumentType = InstrumentType.Find(packet.InstrumentId);
    if (instrumentType == null)
      ((ICoreAPI) this.clientApi).Logger.Error($"[InstrumentModClient] Invalid instrument ID: {packet.InstrumentId}");
    else
      this._playbackManager.RequestStartPlayback(packet.SongPath, packet.Channel, instrumentType);
  }

  private TextCommandResult ParseClientCommand(TextCommandCallingArgs args)
  {
    switch (args.Parsers[0].GetValue().ToString())
    {
      case "enable":
        this.clientSideEnable = true;
        return TextCommandResult.Success("MIDI playback enabled!", (object) null);
      case "disable":
        this.clientSideEnable = false;
        return TextCommandResult.Success("MIDI playback disabled!", (object) null);
      default:
        return TextCommandResult.Success("Syntax: .instruments [enable|disable]", (object) null);
    }
  }
}
