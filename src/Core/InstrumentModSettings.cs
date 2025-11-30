using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace VSInstrumentsBase.src.Core;

public class InstrumentModSettings
{
  private static InstrumentModSettings _instance;

  public bool Enabled { get; set; } = true;
  public float PlayerVolume { get; set; } = 0.7f;
  public float BlockVolume { get; set; } = 1f;
  public string LocalSongsDirectory { get; set; } = Path.Combine(GamePaths.DataPath, "Songs");
  public string DataSongsDirectory { get; set; } = Path.Combine(GamePaths.DataPath, "Songs");

  private static void EnsureDirectoryExists(string path)
  {
    if (string.IsNullOrEmpty(path))
      throw new ArgumentNullException(nameof(path));

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
      InstrumentModSettings settings = api.LoadModConfig<InstrumentModSettings>("instruments.json");
      if (settings == null)
      {
        settings = new InstrumentModSettings();
        api.StoreModConfig(settings, "instruments.json");
      }

      EnsureDirectoryExists(settings.LocalSongsDirectory);
      EnsureDirectoryExists(settings.DataSongsDirectory);
      _instance = settings;
    }
    catch (Exception)
    {
      api.Logger.Error("Could not load instruments config, using default values...");
      _instance = new InstrumentModSettings();
    }
  }

  public static InstrumentModSettings Instance
  {
    get
    {
      if (_instance == null)
        throw new Exception("Mod settings instance must be loaded before it may be used!");

      return _instance;
    }
  }
}
