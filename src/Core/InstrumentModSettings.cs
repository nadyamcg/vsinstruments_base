using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Instruments.Core
{
	public class InstrumentModSettings
	{
		//
		// Summary:
		//     The settings instance. Only exists after loaded!
		private static InstrumentModSettings _instance;


		public bool enabled { get; set; } = true;
		public float playerVolume { get; set; } = 0.7f;
		public float blockVolume { get; set; } = 1.0f;
		public int abcBufferSize { get; set; } = 32;

		[Obsolete("Abc is no longer supported!")]
		public string abcLocalLocation { get; set; } = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "abc";

		[Obsolete("Abc is no longer supported!")]
		public string abcServerLocation { get; set; } = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "abc_server";

		//
		// Summary:
		//     Local fully qualified path to the directory which local (user) songs are stored in.
		//     For the client this represents the directory with songs that are only known to that user, i.e.
		//     all the local songs. In previous versions of the mod this would typically point to the game
		//     directory to the 'abc' subfolder.
		public string LocalSongsDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Songs");
		//
		// Summary:
		//     Local fully qualified path to the directory in which shared, per-server or per-client songs are stored in.
		//     This is an addition in later versions of the mod that allows caching user content instead of re-sending it
		//     on each playback request.
		public string DataSongsDirectory { get; set; } = Path.Combine(GamePaths.DataPath, "Songs");
		//
		// Summary:
		//     Ensures that the provided directory exists or creates one if there is none.
		protected static void EnsureDirectoryExists(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException();

			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}
		//
		// Summary:
		//     Loads the mod configuration, or creates default configuration if no mod settings are present.
		public static void Load(ICoreAPI api)
		{
			// Load settings file
			try
			{
				InstrumentModSettings instance = api.LoadModConfig<InstrumentModSettings>("instruments.json");
				if (instance == null)
				{
					instance = new InstrumentModSettings();
					api.StoreModConfig(instance, "instruments.json");
				}

				EnsureDirectoryExists(instance.LocalSongsDirectory);
				EnsureDirectoryExists(instance.DataSongsDirectory);

				_instance = instance;
			}
			catch (Exception)
			{
				api.Logger.Error("Could not load instruments config, using default values...");
				_instance = new InstrumentModSettings();
			}
		}

		//
		// Summary:
		//     Returns the loaded instrument mod settings instance.
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
}