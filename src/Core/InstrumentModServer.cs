using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Network.Packets;
using VSInstrumentsBase.src.Network.Playback;
using VSInstrumentsBase.src.Playback;
using VSInstrumentsBase.src.Types;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VSInstrumentsBase.src.Core;

public class InstrumentModServer : InstrumentModBase
{
  private ICoreServerAPI serverAPI;
  private FileManagerServer _fileManager;
  private PlaybackManagerServer _playbackManager;

  public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

  public override FileManager FileManager => this._fileManager;

  public override PlaybackManager PlaybackManager => this._playbackManager;

  public override void StartServerSide(ICoreServerAPI api)
  {
    this.serverAPI = api;
    base.StartServerSide(api);
    this._fileManager = new FileManagerServer(api, InstrumentModSettings.Instance);
    this._playbackManager = new PlaybackManagerServer(api, this._fileManager);
    this.serverAPI.Network.RegisterChannel("instrumentsMusicBlock").RegisterMessageType<MusicBlockPlayRequest>().SetMessageHandler<MusicBlockPlayRequest>(OnMusicBlockPlayRequest);
    api.Logger.Debug("[InstrumentModServer] MusicBlock channel registered");
  }

  public override void Dispose() => base.Dispose();

  private void OnMusicBlockPlayRequest(IServerPlayer fromPlayer, MusicBlockPlayRequest packet)
  {
    ((ICoreAPI) this.serverAPI).Logger.Notification($"[InstrumentModServer] MusicBlock play request from {((IPlayer) fromPlayer).PlayerName}: {packet.SongPath}, channel={packet.Channel}, instrumentId={packet.InstrumentId}");
    if (InstrumentType.Find(packet.InstrumentId) == null)
    {
      ((ICoreAPI) this.serverAPI).Logger.Error($"[InstrumentModServer] Invalid instrument ID in MusicBlock request: {packet.InstrumentId}");
    }
    else
    {
      StartPlaybackRequest startPlaybackRequest = new()
      {
        File = packet.SongPath,
        Channel = packet.Channel,
        Instrument = packet.InstrumentId
      };
      this._playbackManager.GetType().GetMethod("OnStartPlaybackRequest", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke((object) this._playbackManager, new object[2]
      {
        (object) fromPlayer,
        (object) startPlaybackRequest
      });
    }
  }
}
