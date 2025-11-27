using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Midi;
using MidiParser;
using Instruments.Types;

namespace Instruments.Players
{
	//
	// Summary:
	//     Base class for players that allow processing and playing back parsed MIDI files.
	public abstract class MidiPlayerBase : IDisposable
	{
		//
		// Summary:
		//     The core interface to the game.
		protected ICoreAPI CoreAPI { get; private set; }
		//
		// Summary:
		//     The instrument type associated with this player.
		protected InstrumentType InstrumentType { get; private set; }
		//
		// Summary:
		//     The active and parsed track to be played by this player.
		private MidiTrack _midiTrack;
		//
		// Summary:
		//     The beats per minute of the active track.
		private int _beatsPerMinute;
		//
		// Summary:
		//     The ticks per quarter note of the active track, as specified in the MIDI.
		private int _ticksPerQuarterNote;
		//
		// Summary:
		//     The duration of the active track in ticks.
		private int _ticksDuration;
		//
		// Summary:
		//     Elapsed time since start in seconds.
		private double _elapsedTime;
		//
		// Summary:
		//     The duration of active track in seconds.
		private double _duration;
		//
		// Summary:
		//     The index of the last MIDI event polled.
		private int _eventIndex;
		//
		// Summary:
		//     The selected channel (track) index.
		private int _channel;
		//
		// Summary:
		//     Creates new MIDI player.
		// Parameters:
		//   api: The API interface to the game world.
		//   instrumentType: The instrument type this player should use.
		public MidiPlayerBase(ICoreAPI api, InstrumentType instrumentType)
		{
			CoreAPI = api;
			InstrumentType = instrumentType;
		}
		//
		// Summary:
		//     Returns the duration of the played track in seconds.
		public double Duration
		{
			get
			{
				return _duration;
			}
		}
		//
		// Summary:
		//     Returns the elapsed time of this player in seconds.
		public double ElapsedTime
		{
			get
			{
				return _elapsedTime;
			}
		}
		//
		// Summary:
		//     Returns whether this player is currently playing.
		public bool IsPlaying
		{
			get
			{
				return _midiTrack != null && _elapsedTime < _duration;
			}
		}
		//
		// Summary:
		//     Returns whether this player has finished its playback.
		public bool IsFinished
		{
			get
			{
				return _midiTrack != null && _elapsedTime >= _duration;
			}
		}
		//
		// Summary:
		//     Returns the duration of this track in ticks.
		private int GetDuration(MidiTrack track)
		{
			// Find the last event and converts it tick time
			// to real-time duration, to know when this track ends.
			int count = track.MidiEvents.Count;
			if (count > 0)
			{
				return track.MidiEvents[count - 1].Time;
			}
			return 0;
		}
		//
		// Summary:
		//     Converts elapsed time in seconds to elapsed ticks.
		//
		// Parameters:
		//   time: Time in (elapsed) seconds to convert to ticks.
		private long TimeToTicks(double seconds)
		{
			return MidiExtensions.TimeToTicks(seconds, _beatsPerMinute, _ticksPerQuarterNote);
		}
		//
		// Summary:
		//     Converts elapsed ticks to elapsed time in seconds.
		//
		// Parameters:
		//   ticks: Ticks to convert to (elapsed) time in seconds.
		private double TicksToTime(long ticks)
		{
			return MidiExtensions.TicksToTime(ticks, _beatsPerMinute, _ticksPerQuarterNote);
		}
		//
		// Summary:
		//     Plays the specified track of the provided MIDI file.
		public void Play(MidiFile midi, int channel)
		{
			if (IsPlaying)
			{
				throw new InvalidOperationException("Cannot start MIDI playback, the player is already playing!");
			}

			if (midi == null)
			{
				throw new InvalidOperationException("Cannot start MIDI playback, the provided file is invalid!");
			}

			_midiTrack = midi.Tracks[channel];

			_beatsPerMinute = midi.ReadBPM();
			_ticksPerQuarterNote = midi.TicksPerQuarterNote;

			_elapsedTime = 0;
			_ticksDuration = GetDuration(midi.Tracks[channel]);
			_duration = TicksToTime(_ticksDuration);

			_channel = channel;
			_eventIndex = 0;
		}
		//
		// Summary:
		//     Opens and plays the MIDI file at the provided file path.
		public void Play(string midiFilePath, int channel)
		{
			MidiFile midiFile = new MidiFile(midiFilePath);
			Play(midiFile, channel);
		}
		//
		// Summary:
		//     Update the playback of this player.
		//
		// Parameters:
		//   deltaTime: Elapsed time in seconds.
		public void Update(float deltaTime)
		{
			if (!IsPlaying)
			{
				throw new InvalidOperationException("Player is not playing!");
			}

			_elapsedTime += deltaTime;
			long elapsedTicks = TimeToTicks(_elapsedTime);

			// Clamp to bounds - the playback is finished.
			if (elapsedTicks > _ticksDuration)
				_elapsedTime = _duration;

			// Peeks one message in the track event list and if it
			// is time for it to be played, true is returned.
			bool tryPollEvent(out MidiEvent outEvent)
			{
				// Out of events range, no work left.
				if (_eventIndex >= _midiTrack.MidiEvents.Count)
				{
					outEvent = default;
					return false;
				}

				MidiEvent polledEvent = _midiTrack.MidiEvents[_eventIndex];
				if (polledEvent.Time <= elapsedTicks)
				{
					outEvent = polledEvent;
					++_eventIndex;
					return true;
				}

				outEvent = default;
				return false;
			}

			// Try polling for possible events to play
			while (tryPollEvent(out MidiEvent midiEvent))
			{
				int channel = (int)_channel;
				float time = (float)TicksToTime(midiEvent.Time);
				switch (midiEvent.MidiEventType)
				{
					case MidiEventType.NoteOff:
						OnNoteOff((Pitch)midiEvent.Note, Constants.Midi.NormalizeVelocity((byte)midiEvent.Velocity), channel, time);
						break;

					case MidiEventType.NoteOn:
						OnNoteOn((Pitch)midiEvent.Note, Constants.Midi.NormalizeVelocity((byte)midiEvent.Velocity), channel, time);
						break;

					case MidiEventType.KeyAfterTouch:
						break;
					case MidiEventType.ControlChange:
						break;
					case MidiEventType.ProgramChange:
						break;
					case MidiEventType.ChannelAfterTouch:
						break;
					case MidiEventType.PitchBendChange:
						break;
					case MidiEventType.MetaEvent:
						break;
				}
			}

			// Fetch new source position and update all sounds:
			Vec3f sourcePosition = GetSourcePosition();
			SetPosition(sourcePosition);
		}
		//
		// Summary:
		//     Seeks to the position within the player without playing notes.
		//
		// Parameters:
		//   time: Time in seconds to seek to.
		public void Seek(double time)
		{
			if (!IsPlaying)
			{
				throw new InvalidOperationException("Player is not playing!");
			}

			long timeInTicks = TimeToTicks(time);
			long durationInTicks = GetDuration(_midiTrack);
			if (timeInTicks > durationInTicks)
			{
				throw new ArgumentOutOfRangeException("Player cannot seek beyond its end!");
			}

			// Find the nearest event
			for (int i = 0; i < _midiTrack.MidiEvents.Count; ++i)
			{
				if (_midiTrack.MidiEvents[i].Time >= timeInTicks)
				{
					_eventIndex = i;
					break;
				}
			}


			_elapsedTime = TicksToTime(timeInTicks);
		}
		//
		// Summary:
		//     Seeks to the position, limited by the player bounds, within the player without playing
		//     any notes and only if Seek can be performed.
		//
		// Parameters:
		//   time: Time in seconds to seek to.
		// Returns:
		//     Whether the operation was successfull.
		public bool TrySeek(double time)
		{
			if (!IsPlaying)
			{
				return false;
			}

			double clampedTime = Math.Min(Math.Max(time, 0), Duration);
			Seek(clampedTime);
			return true;
		}
		//
		// Summary:
		//     Stops the playback.
		//     The player must be playing or finished in order for it to be able to stop!
		public void Stop()
		{
			if (!IsPlaying && !IsFinished)
			{
				throw new InvalidOperationException("Cannot stop MIDI playback, the player is not playing!");
			}

			_midiTrack = null;

			_beatsPerMinute = MidiExtensions.DefaultBPM;
			_ticksPerQuarterNote = 0;

			_elapsedTime = 0;
			_ticksDuration = 0;
			_duration = 0;

			_eventIndex = 0;
			_channel = 0;

			OnStop();
		}
		//
		// Summary:
		//     Stops the playback, only if the playback is active.
		// Returns:
		//     Whether the stop action was successfull.
		public bool TryStop()
		{
			if (IsPlaying)
			{
				Stop();
				return true;
			}
			return false;
		}
		//
		// Summary:
		//     Callback raised when a MIDI NoteOn event occurs.
		// Parameters:
		//   pitch: The key pitch to be played.
		//   velocity: The rate at which the key was pressed. (0-1)
		//   channel: The source channel index.
		//   time: The time in seconds this event occured at.
		protected abstract void OnNoteOn(Pitch pitch, float velocity, int channel, float time);
		//
		// Summary:
		//     Callback raised when a MIDI NoteOff event occurs.
		// Parameters:
		//   pitch: The key pitch to be stopped.
		//   velocity: The rate at which the key was released. (0-1)
		//   channel: The source channel index.
		//   time: The time in seconds this event occured at.
		protected abstract void OnNoteOff(Pitch pitch, float velocity, int channel, float time);
		//
		// Summary:
		//     Callback raised when the playback stops.
		protected virtual void OnStop() { }
		//
		// Summary:
		//     Request sound params for the provided pitch played with specified properties.
		// Returns:
		//     SoundParams instance or null if there is no sound to be played for the specified configuration.
		protected SoundParams CreateSoundParams(Pitch pitch, float velocity, int channel, float time, InstrumentType instrumentType)
		{
			if (IsSourceValid() && instrumentType.GetPitchSound(pitch, out string assetPath, out float soundPitch))
			{
				SoundParams soundParams = new SoundParams(new AssetLocation("instruments", assetPath));
				soundParams.Volume = Constants.Playback.GetVolumeFromVelocity(velocity);
				soundParams.DisposeOnFinish = true;
				soundParams.RelativePosition = false;
				soundParams.Position = GetSourcePosition();
				soundParams.Pitch = soundPitch;
				return soundParams;
			}

			return null;
		}
		//
		// Summary:
		//     Returns whether the source is valid. With invalid source the playback will not occur.
		protected abstract bool IsSourceValid();
		//
		// Summary:
		//     Returns the source position or in other words the position from which sounds should originate.
		protected abstract Vec3f GetSourcePosition();
		//
		// Summary:
		//     Callback raised when the playback is updated.
		// Parameters:
		//   sourcePosition: The sound source position in world space.
		protected abstract void SetPosition(Vec3f sourcePosition);
		//
		// Summary:
		//     Dispose of this player and all of its allocated resources and sounds.
		public abstract void Dispose();
	}
}