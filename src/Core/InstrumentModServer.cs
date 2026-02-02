using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Network.Packets;
using VSInstrumentsBase.src.Network.Playback;
using VSInstrumentsBase.src.Playback;
using VSInstrumentsBase.src.Types;

namespace VSInstrumentsBase.src.Core;

public class InstrumentModServer : InstrumentModBase
{
  private ICoreServerAPI serverAPI;
  private FileManagerServer _fileManager;
  private PlaybackManagerServer _playbackManager;

  public override bool ShouldLoad(EnumAppSide side)
  {
    return side == EnumAppSide.Server;
  }

  public override FileManager FileManager => _fileManager;

  public override PlaybackManager PlaybackManager => _playbackManager;

  public override void StartServerSide(ICoreServerAPI api)
  {
    serverAPI = api;
    base.StartServerSide(api);

    _fileManager = new FileManagerServer(api, InstrumentModSettings.Instance);
    _playbackManager = new PlaybackManagerServer(api, _fileManager);

    serverAPI.Network
      .RegisterChannel(Constants.Channel.MusicBlock)
      .RegisterMessageType<MusicBlockPlayRequest>()
      .SetMessageHandler<MusicBlockPlayRequest>(OnMusicBlockPlayRequest);

    api.Logger.Debug("[InstrumentModServer] MusicBlock channel registered");
  }

  public override void Dispose()
  {
    base.Dispose();
  }

  private void OnMusicBlockPlayRequest(IServerPlayer fromPlayer, MusicBlockPlayRequest packet)
  {
    serverAPI.Logger.Notification($"[InstrumentModServer] MusicBlock play request from {fromPlayer.PlayerName}: {packet.SongPath}, channel={packet.Channel}, instrumentId={packet.InstrumentId}");

    if (InstrumentType.Find(packet.InstrumentId) == null)
    {
      serverAPI.Logger.Error($"[InstrumentModServer] Invalid instrument ID in MusicBlock request: {packet.InstrumentId}");
      return;
    }

    StartPlaybackRequest startPlaybackRequest = new()
    {
      File = packet.SongPath,
      Channel = packet.Channel,
      Instrument = packet.InstrumentId
    };

    MethodInfo method = _playbackManager.GetType().GetMethod("OnStartPlaybackRequest", BindingFlags.Instance | BindingFlags.NonPublic);
    method?.Invoke(_playbackManager, [fromPlayer, startPlaybackRequest]);
  }
}
