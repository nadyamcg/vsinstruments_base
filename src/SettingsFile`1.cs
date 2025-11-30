using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Config;


namespace vsinstruments_base.src;

public class SettingsFile<TSettings> where TSettings : new()
{
  private readonly FileInfo _file;

  public SettingsFile(string filePath)
  {
    this._file = new FileInfo(filePath);
    if (!Directory.Exists(GamePaths.ModConfig))
    {
      Directory.CreateDirectory(GamePaths.ModConfig);
      this.Save();
    }
    else if (!((FileSystemInfo) this._file).Exists)
      this.Save();
    else
      this.Settings = JsonConvert.DeserializeObject<TSettings>(File.ReadAllText(((FileSystemInfo) this._file).FullName));
  }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public TSettings Settings { get; private set; } = new TSettings();

  public void Save()
  {
    File.WriteAllText(((FileSystemInfo) this._file).FullName, JsonConvert.SerializeObject((object) this.Settings, (Formatting) 1));
  }
}
