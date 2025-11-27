using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Midi;
using Instruments.GUI;
using Instruments.Network.Packets;
using Instruments.Types;
using Instruments.Core;

namespace Instruments.Items
{
	public class InstrumentItem : Item
	{
		private NoteFrequency currentNote;

		private ICoreClientAPI capi;
		bool holding = false;

		//
		// Summary:
		//     The associated instrument type of this instrument.
		private InstrumentType _instrumentType;

		[Obsolete("Use InstrumentItemType API instead!")]
		public string instrument
		{
			get
			{
				return InstrumentType.Name;
			}
		}
		[Obsolete("Use InstrumentItemType API instead!")]
		protected string animation
		{
			get
			{
				return InstrumentType.Animation;
			}
		}

		public override void OnLoaded(ICoreAPI api)
		{
			if (api.Side != EnumAppSide.Client)
				return;

			Startup();
		}
		public override void OnUnloaded(ICoreAPI api)
		{
			/*for (int i = 0; toolModes != null && i < toolModes.Length; i++)
			{
				toolModes[i]?.Dispose();
			}*/
		}

		public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blocksel)
		{
			return InstrumentType.ToolModes;
		}
		public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
		{
			SetPlayMode(slot, (PlayMode)toolMode);
		}
		public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
		{
			PlayMode playMode = GetPlayMode(slot);
			return Math.Min(InstrumentType.ToolModes.Length - 1, (int)playMode);
		}
		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
		{
			if (!firstEvent)
				return;

			handling = EnumHandHandling.PreventDefault;
			bool isClient;
			var client = GetClient(byEntity, out isClient);
			if (isClient)
			{
				if (byEntity.Controls.Sneak)
				{
					// Do the default thing instead! In this case, usually, place the item.
					base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
					return;
				}

				bool isPlaying = capi.GetInstrumentMod().PlaybackManager.IsPlaying(client.Player.ClientId);
				if (isPlaying)
				{
					capi.GetInstrumentMod().PlaybackManager.RequestStopPlayback();
				}

				if (GetPlayMode(slot) != PlayMode.abc)
				{
					Vec3d pos = new Vec3d(byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
					NoteStart newNote = new NoteStart();
					newNote.pitch = currentNote.pitch;
					newNote.positon = pos;
					newNote.instrument = instrument;
					IClientNetworkChannel ch = capi.Network.GetChannel(Constants.Channel.Note);
					ch.SendPacket(newNote);
				}
				else
				{
					if (isPlaying)
					{
						//ABCSendStop();
					}
					else
					{
						ABCSongSelect();
					}
				}
			}
			else
			{
				// Called on the server - treat like any old item. Allows the instrument to be placed!
				base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			}
		}
		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
			bool isClient;
			var client = GetClient(byEntity, out isClient);

			if (isClient)
			{
				Update(slot, byEntity);
				// Additionally, update the sound packet
				if (GetPlayMode(slot) != PlayMode.abc)
				{
					Vec3d pos = new Vec3d(byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
					NoteUpdate newNote = new NoteUpdate();
					newNote.pitch = currentNote.pitch;
					newNote.positon = pos;
					IClientNetworkChannel ch = capi.Network.GetChannel(Constants.Channel.Note);
					ch.SendPacket(newNote);
				}
			}

			return true;
		}
		public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
		{
			bool isClient;
			var client = GetClient(byEntity, out isClient);

			if (isClient)
			{
				if (GetPlayMode(slot) != PlayMode.abc)
				{
					NoteStop newNote = new NoteStop();
					IClientNetworkChannel ch = capi.Network.GetChannel(Constants.Channel.Note);
					ch.SendPacket(newNote);
				}
			}
			return true;
		}

		public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
		{
			// While doing nothing, and while playing, get the angle and show the note to play on HUD.
			if (byEntity.World is IClientWorldAccessor)
			{
				Update(slot, byEntity);
			}
		}

		
		public void SetBand(string bn)
		{
			Definitions.Instance.SetBandName(bn);
		}

		public int PlaySong(string filePath)
		{
			//if(index < abcFiles.Count)
			{
				string abcData = "";
				bool abcOK = RecursiveFileProcessor.ReadFile(Definitions.Instance.ABCBasePath() + Path.DirectorySeparatorChar + filePath, ref abcData); // Todo don't send the whole thing
				if (abcOK)
				{
					ABCSendStart(abcData, false);
				}
				else
				{
					// Either the file was deleted since opening the GUI, something weird happened, or the file exists on the server.
					// Whatever happened, let the server worry about it
					ABCSendStart(filePath, true);
				}
			}
			return 1;
		}

		private void Update(ItemSlot slot, EntityAgent byEntity)
		{

			if (!holding)
			{
				holding = true;
				capi.Event.AfterActiveSlotChanged += ChangeFromInstrument;
			}

			switch (GetPlayMode(slot))
			{
				case PlayMode.lockedTone:
					AngleToPitchLockedTone(byEntity.Pos.Pitch);
					break;
				case PlayMode.lockedSemiTone:
					AngleToPitchLockedSemiTone(byEntity.Pos.Pitch);
					break;
				case PlayMode.fluid:
					AngleToPitch(byEntity.Pos.Pitch);
					break;
				case PlayMode.abc:
					// No logic for this mode; all handled by the server.
					break;
				default:
					break;
			}
		}
		private void AngleToPitch(float angle)
		{
			// entity.Pos.Pitch goes from 90 to 270
			// yMin = 4.697, yMax = 1.5858
			// In bottom half of screen, pitch needs to go up from 1 to 2
			// In top half, it needs to go from 2 to 4
			// To get a y between 1 and 3, use y = 4 - 0.6364x
			// In English, 4 - 2/PI

			const float halfwayPoint = 3.1112f;
			// Unfortunately, pitch seems to only work up to * 3, so we can't go up to 4 :'(
			// Instead, shift down 1 octave in the lower half, and up 1 in the upper
			float pitch;
			if (angle > halfwayPoint) // bottom half, remember it's inverted!
				pitch = 2 - angle * (1 / Constants.Math.PI);
			else
				pitch = 3 - angle * (2 / Constants.Math.PI);

			currentNote.pitch = pitch;
			currentNote.ID = "Fluid";
		}
		private void AngleToPitchLockedSemiTone(float angle)
		{
			// Instead of translating an angle straight into a pitch, go up in steps per semi-tone.
			// Middle note: A3, 220Hz
			// Bottom note: A2, 110Hz
			// Top note:    A4, 440Hz
			// TODO: For each semi-tone step is non-linear. Probably best to hard-code it instead of trying to be smart.
			// Each angle increment IS linear - for each angle (floored), return a unique pitch
			// Ok, we don't return the actual pitch, but the pitch multiplier offset, as before

			const float step = 0.13215f;  // 1.5858 / 12
			float currentStep = 1.55858f;
			for (int i = 24; i >= 0; i--)
			{
				// For each step, check if the angle is in the step's range
				// Yeah I know it's really shit, but idgaf
				// TODO if I can be bothered, floor the pitch and map directly to dict instead of using i
				if (angle < currentStep + step)
				{
					currentNote = Definitions.Instance.GetFrequency(i);
					break;
				}
				currentStep += step;
			}
		}
		private void AngleToPitchLockedTone(float angle)
		{
			// Like the above function, but if the current i of the noteMap is a sharp, skip it
			// Also, the step is /8 instead of /12, as steps are shorter innit
			// TODO add keys. Set the key somehow, maybe have a map per key?

			const float step = 0.198225f;  // 1.5858 / 8
			float currentStep = 1.55858f;
			for (int i = 24; i >= 0; i--)
			{
				NoteFrequency nf = currentNote = Definitions.Instance.GetFrequency(i);
				int position = 0;
				Midi.Note note = Midi.Note.ParseNote(nf.ID, ref position);
				// if (nf.ID.IndexOf("^") > 0)
				if (note.Accidental == Midi.Note.Sharp)
				{
					continue;
				}
				if (angle < currentStep + step)
				{
					currentNote = nf;
					break;
				}
				currentStep += step;
			}
		}

		private void Startup()
		{
			// Initialise gui stuff. Has to be done here as OnLoaded has a different API
			capi = api as ICoreClientAPI;
			//guiDialog = new NoteGUI(capi);
		}
#if false
        private bool ToggleGui()
        {
            Action<string> sb = SetBand;
            ModeSelectGUI modeDialog = new ModeSelectGUI(capi, SetMode, sb, Definitions.Instance.GetBandName());
            modeDialog.TryOpen();

            return true;
        }
#endif

		private IClientWorldAccessor GetClient(EntityAgent entity, out bool isClient)
		{
			isClient = entity.World.Side == EnumAppSide.Client;
			return isClient ? entity.World as IClientWorldAccessor : null;
		}
		private void ChangeFromInstrument(ActiveSlotChangeEventArgs args)
		{
			capi.Event.AfterActiveSlotChanged -= ChangeFromInstrument;
			holding = false;
			//guiDialog?.TryClose();
			if (Definitions.Instance.IsPlaying())
			{
				Definitions.Instance.SetIsPlaying(false);
				ABCSendStop();
			}
		}
		private void ABCSendStart(string fileData, bool isServerOwned)
		{
			ABCStartFromClient newABC = new ABCStartFromClient();
			newABC.abcData = fileData;
			newABC.instrument = instrument;
			newABC.bandName = Definitions.Instance.GetBandName();
			newABC.isServerFile = isServerOwned;
			IClientNetworkChannel ch = capi.Network.GetChannel(Constants.Channel.Abc);
			ch.SendPacket(newABC);
			Definitions.Instance.SetIsPlaying(true);
		}
		private void ABCSendStop()
		{
			ABCStopFromClient newABC = new ABCStopFromClient();
			IClientNetworkChannel ch = capi.Network.GetChannel(Constants.Channel.Abc);
			ch.SendPacket(newABC);
		}

		private void ABCSongSelect()
		{
			// Load abc folder
			if (Definitions.Instance.UpdateSongList(capi))
			{
				Action<string> sb = SetBand;
				//SongSelectGUI songGui = new SongSelectGUI(capi, PlaySong, Definitions.Instance.GetSongList(), sb, Definitions.Instance.GetBandName());
				SongSelectGUI songGUI = new SongSelectGUI(capi, InstrumentType);
				songGUI.TryOpen();
			}
		}

		private void SetPlayMode(ItemSlot slot, PlayMode playMode)
		{
			slot.Itemstack.Attributes.SetInt(Constants.Attributes.ToolMode, (int)playMode);
		}


		private PlayMode GetPlayMode(ItemSlot slot)
		{
			return (PlayMode)slot.Itemstack.Attributes.GetInt(Constants.Attributes.ToolMode, (int)PlayMode.abc);
		}

		//
		// Summary:
		//     Returns the associated instrument type for this instance.
		protected InstrumentType InstrumentType
		{
			get
			{
				return _instrumentType != null ? _instrumentType : _instrumentType = InstrumentType.Find(GetType());
			}
		}

		//
		// Summary:
		//     Returns the unique identifier representing the type of this instrument.
		public int InstrumentTypeID
		{
			get
			{
				return InstrumentType.ID;
			}
		}

		[Obsolete("Use InstrumentType and PlaybackManager instead!")]
		public Sound Play(Pitch pitch, Entity player)
		{
			if (!InstrumentType.GetPitchSound(pitch, out string assetPath, out float modPitch))
			{
				return null;
			}

			Sound sound = new Sound(
				(IClientWorldAccessor)api.World,
				player.Pos.XYZ,
				modPitch,
				assetPath,
				-1,
				1.0f,
				true);
			return sound;
		}
	}

#if false  // I'm keeping this for debugging
    public class AcousticGuitarItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            instrument = "acousticguitar";
            animation = "holdbothhandslarge";
            Definitions.Instance.AddInstrumentType(instrument, animation);
            base.OnLoaded(api);
        }
    }
#endif
}
