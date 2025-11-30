using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Items;
using VSInstrumentsBase.src.Playback;
using VSInstrumentsBase.src.Types;
using Vintagestory.API.Common;
using VSInstrumentsBase.src.Blocks;


namespace VSInstrumentsBase.src.Core;

public abstract class InstrumentModBase : ModSystem
{
  protected bool otherPlayerSync = true;
  protected bool serversideAnimSync = false;

  public abstract FileManager FileManager { get; }

  public abstract PlaybackManager PlaybackManager { get; }

  public override void Start(ICoreAPI api)
  {
    base.Start(api);
    api.Logger.Notification("[InstrumentModBase] Mod starting - base initialization");
    InstrumentModSettings.Load(api);
    api.Logger.Debug("[InstrumentModBase] Settings loaded");
    InstrumentTypes.RegisterAll(api);
    this.RegisterInstrumentItems(api);
    ((ICoreAPICommon) api).RegisterBlockClass("musicblock", typeof (MusicBlock));
    ((ICoreAPICommon) api).RegisterBlockEntityClass("musicblockentity", typeof (BEMusicBlock));
    api.Logger.Debug("[InstrumentModBase] MusicBlock registered");
  }

  private void RegisterInstrumentItems(ICoreAPI api)
  {
    ((ICoreAPICommon) api).RegisterItemClass("InstrumentItem", typeof (InstrumentItem));
    api.Logger.Notification("[InstrumentModBase] Registered InstrumentItem class");
  }

  public override void AssetsLoaded(ICoreAPI api)
  {
    base.AssetsLoaded(api);
    api.Logger.Debug("[InstrumentModBase] AssetsLoaded - initializing InstrumentTypes");
    InstrumentType.InitializeTypes();
    api.Logger.Notification("[InstrumentModBase] All instrument types initialized");
  }
}
