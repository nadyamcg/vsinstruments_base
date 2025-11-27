using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Midi;
using Instruments.Mapping;

namespace Instruments.Types
{
	//
	// Summary:
	//     This object decouples and implements sharing of static data and logic of unique item types.
	//     This allows relevant data and logic to be accessed regardless of the actual item's lifetime.
	//     See InstrumentTypeExtensions for convenience API extensions.
	public abstract class InstrumentType
	{
		//
		// Summary:
		//     The interface to the game.
		private ICoreAPI _api;
		//
		// Summary:
		//     Unique identifier of this instrument type.
		private int _id;
		// Summary:
		//     Name of the instrument as specified in the data, for example 'grandpiano'.
		private string _name;
		//
		// Summary:
		//     Name of the animation as specified in the data, for example 'holdbothhandslarge'.
		private string _animation;
		//
		// Summary:
		//     Default shared item type, generally used if no other item type is provided.
		private NoteMapping<string> _noteMap;
		//
		// Summary:
		//     Tool modes shared across all instances of this instrument type.
		private SkillItem[] _toolModes;
		//
		// Summary:
		//     Map of all instrument types by their unique identifier.
		private static Dictionary<int, InstrumentType> _instrumentTypes;
		//
		// Summary:
		//     Queue of types that are pending initialization.
		private static Queue<InstrumentType> _initializationQueue;
		//
		// Summary:
		//     Intializes static type properties.
		static InstrumentType()
		{
			_instrumentTypes = new Dictionary<int, InstrumentType>();
			_initializationQueue = new Queue<InstrumentType>();
		}
		//
		// Summary:
		//     Creates new type.
		public InstrumentType(string name, string animation)
		{
			_name = name;
			_animation = animation;
		}
		//
		// Summary:
		//     Registers and associates the provided class type with given instance type.
		public static void Register(ICoreAPI api, Type instanceType, InstrumentType instrumentType)
		{
			int id = ComputeID(instanceType);
			if (!_instrumentTypes.TryAdd(id, instrumentType))
			{
				// This instrument type is already registered. This may be legal when the application is
				// running as both the server and the client.
				return;
			}

			instrumentType._api = api;
			instrumentType._id = id;

			// Insert the newly registered type into the queue for pending initialization,
			// the initialization will need to happen only after assets are loaded.
			_initializationQueue.Enqueue(instrumentType);
		}
		//
		// Summary:
		//     Initialize all types that are pending initialization.
		public static void InitializeTypes()
		{
			while (_initializationQueue.Count > 0)
			{
				InstrumentType type = _initializationQueue.Dequeue();
				type.Initialize();
			}
		}
		//
		// Summary:
		//     Cleans up all registered types.
		public static void UnregisterAll()
		{
			InstrumentType[] allTypes = new InstrumentType[_instrumentTypes.Count];
			for (int i = 0; i < allTypes.Length; ++i)
			{
				InstrumentType type = allTypes[i];
				if (type != null && _instrumentTypes.Remove(type._id))
					type.Cleanup();
			}
			_instrumentTypes.Clear();
		}
		//
		// Summary:
		//     Initialize the instrument type instance.
		//     This occurs only after the item type and its associated instrument type are both registered.
		//     Additionally initialization will only happen after assets are loaded.
		protected virtual void Initialize()
		{
			_toolModes = new SkillItem[4];
			_toolModes[(int)PlayMode.abc] = new SkillItem() { Code = new AssetLocation(PlayMode.abc.ToString()), Name = Lang.Get("ABC Mode") };
			_toolModes[(int)PlayMode.fluid] = new SkillItem() { Code = new AssetLocation(PlayMode.fluid.ToString()), Name = Lang.Get("Fluid Play") };
			_toolModes[(int)PlayMode.lockedSemiTone] = new SkillItem() { Code = new AssetLocation(PlayMode.lockedSemiTone.ToString()), Name = Lang.Get("Locked Play: Semi Tone") };
			_toolModes[(int)PlayMode.lockedTone] = new SkillItem() { Code = new AssetLocation(PlayMode.lockedTone.ToString()), Name = Lang.Get("Locked Play: Tone") };

			if (Api is ICoreClientAPI capi)
			{
				_toolModes[(int)PlayMode.abc].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/abc.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
				_toolModes[(int)PlayMode.abc].TexturePremultipliedAlpha = false;
				_toolModes[(int)PlayMode.fluid].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/3.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
				_toolModes[(int)PlayMode.fluid].TexturePremultipliedAlpha = false;
				_toolModes[(int)PlayMode.lockedSemiTone].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/2.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
				_toolModes[(int)PlayMode.lockedSemiTone].TexturePremultipliedAlpha = false;
				_toolModes[(int)PlayMode.lockedTone].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/1.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
				_toolModes[(int)PlayMode.lockedTone].TexturePremultipliedAlpha = false;
			}

			_noteMap = new NoteMappingLegacy(string.Concat("sounds/", Name));
		}
		//
		// Summary:
		//     Unregister this type.
		internal static void UnregisterType(Type instanceType)
		{
			int typeID = ComputeID(instanceType);
			if (_instrumentTypes.Remove(typeID, out InstrumentType type))
			{
				type.Cleanup();
			}
		}
		//
		// Summary:
		//     Releases any resources held by this type.
		protected virtual void Cleanup()
		{
			foreach (SkillItem toolMode in _toolModes)
				toolMode.Dispose();
			Array.Clear(_toolModes);
			_toolModes = null;
		}
		//
		// Summary:
		//     Returns the game api, available on the client and the server.
		public ICoreAPI Api
		{
			get
			{
				return _api;
			}
		}

		//
		// Summary:
		//     Returns the unique identifier of this instrument that can be used as a substitute for its name.
		public int ID
		{
			get
			{
				return _id;
			}
		}
		//
		// Summary:
		//     Returns the name of this instrument, as defined in data.
		public string Name
		{
			get
			{
				return _name;
			}
		}
		//
		// Summary:
		//     Returns the name of the animation used by this instrument, as defined in data.
		public string Animation
		{
			get
			{
				return _animation;
			}
		}
		//
		// Summary:
		//     Returns the note mapping for this instrument type.
		public NoteMapping<string> NoteMap
		{
			get
			{
				return _noteMap;
			}
		}
		//
		// Summary:
		//     Returns the tool modes for this instrument type.
		public SkillItem[] ToolModes
		{
			get
			{
				return _toolModes;
			}
		}
		//
		// Summary:
		//     Returns sound data of this instrument for the provided pitch.
		//
		// Parameters:
		//   pitch: Input pitch the sound should represent.
		//   assetPath: Outputs the path to the desired sound sample.
		//   modPitch: Outputs the pitch the sound sample should play at.
		public virtual bool GetPitchSound(Pitch pitch, out string assetPath, out float modPitch)
		{
			assetPath = NoteMap.GetValue(pitch);
			if (string.IsNullOrEmpty(assetPath))
			{
				modPitch = 1;
				return false;
			}

			modPitch = NoteMap.GetRelativePitch(pitch);
			return true;
		}
		//
		// Summary:
		//     Finds the instrument item type by its unique identifier.
		internal static InstrumentType Find(int id)
		{
			if (_instrumentTypes.TryGetValue(id, out InstrumentType type))
				return type;

			return null;
		}
		//
		// Summary:
		//     Finds the instrument item type by its instance type.
		// Parameters:
		//   instanceType: The type of the instrument instance.
		internal static InstrumentType Find(Type instanceType)
		{
			int typeID = ComputeID(instanceType);
			return Find(typeID);
		}
		//
		// Summary:
		//     Returns unique identifier for provided type.
		private static int ComputeID(Type type)
		{
			return type.FullName.GetHashCode();
		}
		//
		// Summary:
		//     Finds all instruments with the provided name.
		//     This method is considered slow and should only be used in rare scenarios.
		internal static void Find(string name, List<InstrumentType> destination, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
		{
			foreach (var kvp in _instrumentTypes)
			{
				InstrumentType instrumentType = kvp.Value;
				if (string.Compare(name, instrumentType.Name, comparison) == 0)
					destination.Add(instrumentType);
			}
		}
	}
	//
	// Summary:
	//     This class provides convenience and utility extensions for instrument types.
	public static class InstrumentTypeExtensions
	{
		//
		// Summary:
		//     Registers the provided instrument item into the game API and registers the
		//     provided instrument type as its associated instrument type.
		//
		// Parameters:
		//   api: Game API
		//   itemType: Instrument instance type.
		//   instrumentType: Instrument type instance.
		public static void RegisterInstrumentItem(this ICoreAPI api, Type itemType, InstrumentType instrumentType)
		{
			api.RegisterItemClass(instrumentType.Name, itemType);
			InstrumentType.Register(api, itemType, instrumentType);
		}
	}
}
