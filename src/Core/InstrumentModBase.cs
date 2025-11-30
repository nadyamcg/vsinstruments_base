using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Items;
using VSInstrumentsBase.src.Playback;
using VSInstrumentsBase.src.Types;
using VSInstrumentsBase.src.Utils;
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
    Log.Notification(api, "InstrumentModBase", "Mod starting - base initialization");
    InstrumentModSettings.Load(api);
    Log.Debug(api, "InstrumentModBase", "Settings loaded");
    InstrumentTypes.RegisterAll(api);
    this.RegisterInstrumentItems(api);
    ((ICoreAPICommon) api).RegisterBlockClass("musicblock", typeof (MusicBlock));
    ((ICoreAPICommon) api).RegisterBlockEntityClass("musicblockentity", typeof (BEMusicBlock));
    Log.Debug(api, "InstrumentModBase", "MusicBlock registered");
  }

  private void RegisterInstrumentItems(ICoreAPI api)
  {
    ((ICoreAPICommon) api).RegisterItemClass("InstrumentItem", typeof (InstrumentItem));
    Log.Notification(api, "InstrumentModBase", "Registered InstrumentItem class");
  }

  public override void AssetsLoaded(ICoreAPI api)
  {
    base.AssetsLoaded(api);
    Log.Debug(api, "InstrumentModBase", "AssetsLoaded - initializing InstrumentTypes");
    InstrumentType.InitializeTypes();
    Log.Notification(api, "InstrumentModBase", "All instrument types initialized");
  }
}
