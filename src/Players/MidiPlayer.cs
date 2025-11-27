using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Midi;
using Instruments.Types;

namespace Instruments.Players
{
	//
	// Summary:
	//     Basic implementation of a MidiPlayer that can be further specialized to playback MIDI tracks on the client.
	public class MidiPlayer : MidiPlayerBase, IDisposable
	{
		//
		// Summary:
		//     List of all active sounds with each slot representing a single pitch.
		//     Contents may be null if no sound is playing for provided slot.
		private ILoadedSound[] _sounds;
		//
		// Summary:
		//     The source of playback.
		private IPlayer _source;
		//
		// Summary:
		//     Creates new music player.
		public MidiPlayer(ICoreAPI api, IPlayer source, InstrumentType instrumentType)
			: base(api, instrumentType)
		{
			_sounds = new ILoadedSound[Constants.Note.NoteCount];
			_source = source;
		}
		//
		// Summary:
		//     Starts playing a note.
		protected override void OnNoteOn(Pitch pitch, float velocity, int channel, float time)
		{
			// Do not perform any playback on the server:
			if (CoreAPI is not ICoreClientAPI clientAPI)
			{
				return;
			}

			// If, by any chance, the events are mismatched or something else has happened
			// and there is a sound already playing in the slot, simply stop it and replace
			// it immediately with the new sound:
			int index = (int)pitch;
			TryRemoveSound(index, Constants.Playback.MinFadeOutDuration);

			// Try to prepare sound params for the new sound to be played in the targeted slot,
			// and load and play the sound if possible.
			SoundParams soundParams = CreateSoundParams(pitch, velocity, channel, time, InstrumentType);
			if (soundParams != null)
			{
				ILoadedSound sound = clientAPI.World.LoadSound(soundParams);
				if (sound != null)
				{
					_sounds[index] = sound;
					sound.Start();
				}
			}
		}
		//
		// Summary:
		//     Stops playing a note.
		protected override void OnNoteOff(Pitch pitch, float velocity, int channel, float time)
		{
			// Do not perform any playback on the server:
			if (CoreAPI is not ICoreClientAPI)
			{
				return;
			}

			int index = (int)pitch;
			float fadeDuration = Constants.Playback.GetFadeDurationFromVelocity(velocity);
			TryRemoveSound(index, fadeDuration);
		}
		//
		// Summary:
		//     Returns the source from which sound should originate is valid.
		protected override bool IsSourceValid()
		{
			return _source != null && _source.Entity != null;
		}
		//
		// Summary:
		//     Returns the position from which sounds should originate.
		protected override Vec3f GetSourcePosition()
		{
			return _source.Entity.Pos.XYZFloat;
		}
		//
		// Summary:
		//     Sets the position of all active sounds.
		protected override void SetPosition(Vec3f sourcePosition)
		{
			for (int i = 0; i < _sounds.Length; ++i)
			{
				ILoadedSound sound = _sounds[i];
				if (sound == null)
				{
					continue;
				}

				sound.SetPosition(sourcePosition);
			}
		}
		//
		// Summary:
		//     Removes a sound from the active sounds.
		// Parameters:
		//   fadeDuration: Duration to fade out the sound in (in seconds) or 0 for immediate.
		private void TryRemoveSound(int index, float fadeDuration)
		{
			ILoadedSound sound = _sounds[index];
			if (sound == null)
				return;

			if (fadeDuration <= 0)
			{
				sound.Dispose();
				_sounds[index] = null;
				return;
			}
			else
			{
				sound.FadeOutAndStop(fadeDuration);
				_sounds[index] = null;
				return;
			}
		}
		//
		// Summary:
		//     Callback raised when the playback stops.
		protected override void OnStop()
		{
			StopAllSounds(Constants.Playback.MinFadeOutDuration);
			base.OnStop();
		}
		//
		// Summary:
		//     Stops all outgoing sounds in the provided duration.
		protected void StopAllSounds(float fadeDuration)
		{
			for (int i = 0; i < _sounds.Length; ++i)
				TryRemoveSound(i, fadeDuration);

			Array.Clear(_sounds);
		}
		//
		// Summary:
		//     Dispose of this player and all of its allocated resources and sounds.
		public override void Dispose()
		{
			StopAllSounds(0);
		}
	}
}