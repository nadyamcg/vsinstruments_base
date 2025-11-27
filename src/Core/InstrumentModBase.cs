using System;
using Vintagestory.API.Common;

using Instruments.Blocks;
using Instruments.Types;
using Instruments.Files;
using Instruments.Playback;

namespace Instruments.Core
{
	public abstract class InstrumentModBase : ModSystem
	{
		protected bool otherPlayerSync = true;
		protected bool serversideAnimSync = false;

		//
		// Summary:
		//     Returns the object responsible for management of files.
		public abstract FileManager FileManager { get; }
		//
		// Summary:
		//     Returns the object responsible for music playback.
		public abstract PlaybackManager PlaybackManager { get; }

		public override void Start(ICoreAPI api)
		{
			base.Start(api);

			InstrumentModSettings.Load(api);

			// TODO@exocs: Add InstrumentType support
			api.RegisterBlockClass("musicblock", typeof(MusicBlock));
			api.RegisterBlockEntityClass("musicblockentity", typeof(BEMusicBlock));
		}

		public override void AssetsLoaded(ICoreAPI api)
		{
			base.AssetsLoaded(api);
			InstrumentType.InitializeTypes();
		}
	}

	//
	// Summary:
	//     This class provides convenience extension wrappers for retrieving the mod system instance.
	public static partial class InstrumentModExtensions
	{
		//
		// Summary:
		//     Convenience wrapper for retrieving the instruments mod instances.
		private static T GetInstrumentMod<T>(this ICoreAPI coreAPI) where T : InstrumentModBase
		{
			return coreAPI.ModLoader.GetModSystem<T>();
		}
		//
		// Summary:
		//     Convenience wrapper for retrieving the instruments mod instance.
		public static InstrumentModBase GetInstrumentMod(this ICoreAPI coreAPI)
		{
			return GetInstrumentMod<InstrumentModBase>(coreAPI);
		}
	}
}