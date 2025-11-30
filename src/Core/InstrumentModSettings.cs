using System;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;


namespace VSInstrumentsBase.src.Core;

public class InstrumentModSettings
{
  private static InstrumentModSettings _instance;

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public bool Enabled { get; set; } = true;

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public float PlayerVolume { get; set; } = 0.7f;

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public float BlockVolume { get; set; } = 1f;

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public string LocalSongsDirectory { get; set; } = Path.Combine(GamePaths.DataPath, "Songs");

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public string DataSongsDirectory { get; set; } = Path.Combine(GamePaths.DataPath, "Songs");

  protected static void EnsureDirectoryExists(string path)
  {
    if (string.IsNullOrEmpty(path))
      throw new ArgumentNullException();
    if (Directory.Exists(path))
      return;
    try
    {
      Directory.CreateDirectory(path);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException($"Failed to create directory '{path}': {ex.Message}");
    }
  }

  public static void Load(ICoreAPI api)
  {
    try
    {
      InstrumentModSettings instrumentModSettings =  api.LoadModConfig<InstrumentModSettings>("instruments.json");
      if (instrumentModSettings == null)
      {
        instrumentModSettings = new InstrumentModSettings();
         api.StoreModConfig(instrumentModSettings, "instruments.json");
      }
            EnsureDirectoryExists(instrumentModSettings.LocalSongsDirectory);
            EnsureDirectoryExists(instrumentModSettings.DataSongsDirectory);
            _instance = instrumentModSettings;
    }
    catch (Exception ex)
    {
      api.Logger.Error("Could not load instruments config, using default values...");
            _instance = new InstrumentModSettings();
    }
  }

  public static InstrumentModSettings Instance
  {
    get
    {
      return _instance != null ? _instance : throw new Exception("Mod settings instance must be loaded before it may be used!");
    }
  }
}
