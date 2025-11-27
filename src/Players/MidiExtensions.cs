using System.IO;
using Midi;
using MidiParser;

namespace Instruments.Players
{
	//
	// Summary:
	//     Class that stores and describes a single track within a MIDI file.
	public class MidiTrackInfo
	{
		//
		// Summary:
		//     The index of this track.
		public readonly int Index;
		//
		// Summary:
		//     The length of this track in seconds.
		public readonly double Duration;
		//
		// Summary:
		//     Number of notes in the track.
		public readonly int NoteCount;
		//
		// Summary:
		//     Time in seconds at which the first note exists.
		//     May be null if no notes are present.
		public readonly double? FirstNoteTime;
		//
		// Summary:
		//     Instrument defined by a program change in this track.
		//     May be null if no such events are present.
		public readonly Instrument? Instrument;
		//
		// Summary:
		//     Returns the MIDI instrument in human readable format.
		public string InstrumentText
		{
			get
			{
				if (!Instrument.HasValue)
					return "Unknown";

				return Instrument.Value.Name();
			}
		}
		//
		// Summary:
		//     Creates new midi track info for the provided track.
		public MidiTrackInfo(MidiFile midi, int track)
		{
			Index = track;
			Duration = midi.ReadTrackDuration(track);
			NoteCount = midi.ReadNoteCount(track);
			FirstNoteTime = (NoteCount > 0) ? midi.ReadFirstNoteInSeconds(track) : null;
			Instrument = midi.FindInstrument(track);
		}
	}
	//
	// Summary:
	//     Class that stores and describes a single MIDI file.
	public class MidiFileInfo
	{
		//
		// Summary:
		//     Stores the generic information about this file.
		public readonly FileInfo FileInfo;
		//
		// Summary:
		//     Returns whether this file exists.
		public bool Exists => FileInfo.Exists;
		//
		// Summary:
		//     Actual and parsed MIDI file, if successfull.
		private readonly MidiFile MidiFile;
		//
		// Summary:
		//     Returns whether this object is a valid MIDI (file) info.
		public bool IsMidi => MidiFile != null;
		//
		// Summary:
		//     Returns the size of this file in KB.
		public long SizeKB
		{
			get
			{
				return (long)(FileInfo.Length / 1000.0f);
			}
		}
		//
		// Summary:
		//     Returns the BPM of the MIDI.
		public readonly int BPM;
		//
		// Summary:
		//     Returns the duration of the longest channel in the MIDI.
		public readonly double Duration;
		//
		// Summary:
		//     Stores information about individual tracks in this file.
		public readonly MidiTrackInfo[] Tracks;
		//
		// Summary:
		//     Returns the number of tracks in this file.
		public int TracksCount
		{
			get
			{
				return Tracks.Length;
			}
		}
		//
		// Summary:
		//     Returns the MIDI format in human readable format.
		public string FormatText
		{
			get
			{
				int formatValue = IsMidi ? MidiFile.Format : -1;
				switch (formatValue)
				{
					case 0: return "Single track";
					case 1: return "Multi track";
					case 2: return "Multi song";
					default: return "Unknown";
				}
			}
		}
		//
		// Summary:
		//     Creates new MIDI file info from the provided absolute path.
		public MidiFileInfo(string path)
		{
			// The file must exist to even be considered a valid MIDI.
			this.FileInfo = new FileInfo(path);
			if (!FileInfo.Exists)
			{
				return;
			}

			// If the file cannot be parsed, anything else is no longer
			// relevant in this context.
			try
			{
				this.MidiFile = new MidiFile(path);
			}
			catch
			{
				return;
			}

			// Start by retrieving information about the midi itself:
			BPM = MidiFile.ReadBPM();
			Duration = MidiFile.ReadMaxTrackDuration();
			Tracks = new MidiTrackInfo[MidiFile.TracksCount];
			for (int i = 0; i < MidiFile.TracksCount; ++i)
			{
				Tracks[i] = new MidiTrackInfo(MidiFile, i);
			}
		}
		//
		// Summary:
		//     Returns the actual MIDI file.
		public MidiFile GetMidiFile()
		{
			// Even though this property could be made public, explicit method call is
			// used instead to prevent accessing the file properties instead of the actual
			// cached properties by accident.
			return MidiFile;
		}
	}

	//
	// Summary:
	//     Convenience class for MIDI file and player extensions.
	public static class MidiExtensions
	{
		//
		// Summary:
		//     Default, fallback beats per minute as specified by the standard.
		public const int DefaultBPM = 120;

		//
		// Summary:
		//     Finds the BPM meta events in any of the provided tracks.
		//     Fallbacks to default BPM of 120 as per the Midi standard.
		public static int ReadBPM(MidiTrack[] tracks, int defaultValue = DefaultBPM)
		{
			// The MIDI file should contain a track with the timing meta event,
			// that will contain the quarter notes per microseconds value, which
			// is converted into BPM in the MidiParser and used for playback timing.
			foreach (MidiTrack track in tracks)
			{
				foreach (MidiEvent midiEvent in track.MidiEvents)
				{
					if (midiEvent.Time > 0)
						break;

					if (midiEvent.MidiEventType == MidiEventType.MetaEvent &&
						midiEvent.MetaEventType == MetaEventType.Tempo)
					{
						return midiEvent.Arg2;
					}
				}
			}

			return defaultValue;
		}
		//
		// Summary:
		//     Finds the BPM meta events in any of the tracks of
		//     the provided MIDI file.
		public static int ReadBPM(this MidiFile file, int defaultValue = DefaultBPM)
		{
			if (file.TracksCount == 0)
				return defaultValue;

			return ReadBPM(file.Tracks, defaultValue);
		}
		//
		// Summary:
		//     Returns the duration of this track in ticks.
		public static int ReadTrackDurationInTicks(MidiTrack track)
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
		//     Returns the time in ticks at which the first event occurs or -1 if none.
		public static int ReadFirstNoteInTicks(MidiTrack track)
		{
			int count = track.MidiEvents.Count;
			if (count > 0)
			{
				for (int e = 0; e < count; ++e)
				{
					if (track.MidiEvents[e].MidiEventType == MidiEventType.NoteOn)
						return track.MidiEvents[e].Time;
				}
			}
			return -1;
		}
		//
		// Summary:
		//     Returns the number of notes in the provided track.
		public static int ReadNoteCount(MidiTrack track)
		{
			if (track.MidiEvents.Count == 0)
				return 0;

			int count = 0;
			for (int i = 0; i < track.MidiEvents.Count; ++i)
			{
				if (track.MidiEvents[i].MidiEventType == MidiEventType.NoteOn)
					++count;
			}
			return count;
		}
		//
		// Summary:
		//     Try to find the instrument in existing meta events in the provided track.
		public static bool FindInstrument(this MidiTrack track, out Instrument instrument)
		{
			foreach (MidiEvent midiEvent in track.MidiEvents)
			{
				if (midiEvent.Time > 0)
					break;

				if (midiEvent.MidiEventType == MidiEventType.ProgramChange)
				{
					instrument = (Midi.Instrument)midiEvent.Arg2;
					return true;
				}
			}
			instrument = default;
			return false;
		}
		//
		// Summary:
		//     Try to find the instrument in existing meta events in the provided track.
		//     Returns specified default value if no program change meta events are found.
		public static string FindInstrumentName(this MidiTrack track, string defaultValue = "Unknown")
		{
			return FindInstrument(track, out Instrument instrument) ?
				instrument.Name() :
				defaultValue;
		}
		//
		// Summary:
		//     Returns the duration of the specified track in seconds.
		public static double ReadTrackDuration(this MidiFile midi, int track)
		{
			MidiTrack midiTrack = midi.Tracks[track];
			int ticksDuration = ReadTrackDurationInTicks(midiTrack);
			if (ticksDuration == 0)
				return 0;

			int bpm = midi.ReadBPM();
			double durationSeconds = TicksToTime(ticksDuration, bpm, midi.TicksPerQuarterNote);
			return durationSeconds;
		}
		//
		// Summary:
		//     Returns the time in seconds at which the first event occurs or -1 if none.
		public static double ReadFirstNoteInSeconds(this MidiFile midi, int track)
		{
			int firstNoteInTicks = ReadFirstNoteInTicks(midi.Tracks[track]);
			if (firstNoteInTicks == -1)
				return -1;

			int bpm = midi.ReadBPM();
			double startInSeconds = TicksToTime(firstNoteInTicks, bpm, midi.TicksPerQuarterNote);
			return startInSeconds;
		}
		//
		// Summary:
		//     Returns the time in seconds at which the first event occurs or -1 if none.
		public static Instrument? FindInstrument(this MidiFile midi, int track)
		{
			return midi.Tracks[track].FindInstrument(out Instrument instrument) ?
				instrument :
				null;
		}
		//
		// Summary:
		//     Returns the number of notes in the specified track.
		public static int ReadNoteCount(this MidiFile midi, int track)
		{
			return ReadNoteCount(midi.Tracks[track]);
		}
		//
		// Summary:
		//     Returns the longest duration of this file in seconds.
		public static double ReadMaxTrackDuration(this MidiFile midi)
		{
			int maxTicks = 0;
			for (int i = 0; i < midi.TracksCount; ++i)
			{
				MidiTrack midiTrack = midi.Tracks[i];
				int ticksDuration = ReadTrackDurationInTicks(midiTrack);
				if (ticksDuration > maxTicks)
					maxTicks = ticksDuration;
			}

			if (maxTicks == 0)
				return 0;
			int bpm = midi.ReadBPM();
			double durationSeconds = TicksToTime(maxTicks, bpm, midi.TicksPerQuarterNote);
			return durationSeconds;
		}
		//
		// Summary:
		//     Converts elapsed time in seconds to elapsed ticks.
		//
		// Parameters:
		//   seconds: Time in (elapsed) seconds to convert to ticks.
		//   bpm: Track beats per minute.
		//   ticksPerQuaterNote: Track ticks per quarter note. (Defined in midi file)
		public static long TimeToTicks(double seconds, int bpm, int ticksPerQuarterNote)
		{
			double secondsPerQuarterNote = 60.0 / (double)bpm;
			return (long)(seconds * (ticksPerQuarterNote / secondsPerQuarterNote));
		}
		//
		// Summary:
		//     Converts elapsed ticks to elapsed time in seconds.
		//
		// Parameters:
		//   ticks: Time in (elapsed) ticks to convert to seconds.
		//   bpm: Track beats per minute.
		//   ticksPerQuaterNote: Track ticks per quarter note. (Defined in midi file)
		public static double TicksToTime(long ticks, int bpm, int ticksPerQuaterNote)
		{
			double secondsPerQuarterNote = 60.0 / (double)bpm;
			return ticks * (secondsPerQuarterNote / ticksPerQuaterNote);
		}
	}
}
