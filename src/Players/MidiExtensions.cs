using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Linq;


namespace VSInstrumentsBase.src.Players;

public static class MidiExtensions
{
  public const int DefaultBPM = 120;

  public static int ReadBPM(this MidiFile midi)
  {
    return (int) Math.Round(TempoMapManagingUtilities.GetTempoMap(midi).GetTempoAtTime((ITimeSpan) new MetricTimeSpan(0L)).BeatsPerMinute);
  }

  public static double ReadTrackDuration(this MidiFile midi, int trackIndex)
  {
    TrackChunk[] array = Melanchall.DryWetMidi.Core.TrackChunkUtilities.GetTrackChunks(midi).ToArray<TrackChunk>();
    if (trackIndex >= array.Length)
      return 0.0;
    TrackChunk trackChunk = array[trackIndex];
    TempoMap tempoMap = TempoMapManagingUtilities.GetTempoMap(midi);
    TimedEvent timedEvent = TimedEventsManagingUtilities.GetTimedEvents(trackChunk, (TimedEventDetectionSettings) null).LastOrDefault<TimedEvent>();
    return timedEvent != null ? TimeConverter.ConvertTo<MetricTimeSpan>(timedEvent.Time, tempoMap).TotalSeconds : 0.0;
  }

  public static double ReadFirstNoteInSeconds(this MidiFile midi, int trackIndex)
  {
    TrackChunk[] array = Melanchall.DryWetMidi.Core.TrackChunkUtilities.GetTrackChunks(midi).ToArray<TrackChunk>();
    if (trackIndex >= array.Length)
      return -1.0;
    Note note = NotesManagingUtilities.GetNotes(array[trackIndex], (NoteDetectionSettings) null, (TimedEventDetectionSettings) null).FirstOrDefault<Note>();
    if (note == null)
      return -1.0;
    TempoMap tempoMap = TempoMapManagingUtilities.GetTempoMap(midi);
    return TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap).TotalSeconds;
  }

  public static int ReadNoteCount(this MidiFile midi, int trackIndex)
  {
    TrackChunk[] array = Melanchall.DryWetMidi.Core.TrackChunkUtilities.GetTrackChunks(midi).ToArray<TrackChunk>();
    return trackIndex >= array.Length ? 0 : NotesManagingUtilities.GetNotes(array[trackIndex], (NoteDetectionSettings) null, (TimedEventDetectionSettings) null).Count<Note>();
  }

  public static double ReadMaxTrackDuration(this MidiFile midi)
  {
    TempoMap tempoMap = TempoMapManagingUtilities.GetTempoMap(midi);
    TimedEvent timedEvent = TimedEventsManagingUtilities.GetTimedEvents(midi, (TimedEventDetectionSettings) null).LastOrDefault<TimedEvent>();
    return timedEvent != null ? TimeConverter.ConvertTo<MetricTimeSpan>(timedEvent.Time, tempoMap).TotalSeconds : 0.0;
  }

  public static long TimeToTicks(double seconds, int bpm, int ticksPerQuarterNote)
  {
    double num = 60.0 / (double) bpm;
    return (long) (seconds * ((double) ticksPerQuarterNote / num));
  }

  public static double TicksToTime(long ticks, int bpm, int ticksPerQuarterNote)
  {
    double num = 60.0 / (double) bpm;
    return (double) ticks * (num / (double) ticksPerQuarterNote);
  }

  public static string GetInstrumentName(byte programNumber)
  {
    string[] strArray = new string[128 ]
    {
      "Acoustic Grand Piano",
      "Bright Acoustic Piano",
      "Electric Grand Piano",
      "Honky-tonk Piano",
      "Electric Piano 1",
      "Electric Piano 2",
      "Harpsichord",
      "Clavinet",
      "Celesta",
      "Glockenspiel",
      "Music Box",
      "Vibraphone",
      "Marimba",
      "Xylophone",
      "Tubular Bells",
      "Dulcimer",
      "Drawbar Organ",
      "Percussive Organ",
      "Rock Organ",
      "Church Organ",
      "Reed Organ",
      "Accordion",
      "Harmonica",
      "Tango Accordion",
      "Acoustic Guitar (nylon)",
      "Acoustic Guitar (steel)",
      "Electric Guitar (jazz)",
      "Electric Guitar (clean)",
      "Electric Guitar (muted)",
      "Overdriven Guitar",
      "Distortion Guitar",
      "Guitar harmonics",
      "Acoustic Bass",
      "Electric Bass (finger)",
      "Electric Bass (pick)",
      "Fretless Bass",
      "Slap Bass 1",
      "Slap Bass 2",
      "Synth Bass 1",
      "Synth Bass 2",
      "Violin",
      "Viola",
      "Cello",
      "Contrabass",
      "Tremolo Strings",
      "Pizzicato Strings",
      "Orchestral Harp",
      "Timpani",
      "String Ensemble 1",
      "String Ensemble 2",
      "SynthStrings 1",
      "SynthStrings 2",
      "Choir Aahs",
      "Voice Oohs",
      "Synth Voice",
      "Orchestra Hit",
      "Trumpet",
      "Trombone",
      "Tuba",
      "Muted Trumpet",
      "French Horn",
      "Brass Section",
      "SynthBrass 1",
      "SynthBrass 2",
      "Soprano Sax",
      "Alto Sax",
      "Tenor Sax",
      "Baritone Sax",
      "Oboe",
      "English Horn",
      "Bassoon",
      "Clarinet",
      "Piccolo",
      "Flute",
      "Recorder",
      "Pan Flute",
      "Blown Bottle",
      "Shakuhachi",
      "Whistle",
      "Ocarina",
      "Lead 1 (square)",
      "Lead 2 (sawtooth)",
      "Lead 3 (calliope)",
      "Lead 4 (chiff)",
      "Lead 5 (charang)",
      "Lead 6 (voice)",
      "Lead 7 (fifths)",
      "Lead 8 (bass+lead)",
      "Pad 1 (new age)",
      "Pad 2 (warm)",
      "Pad 3 (polysynth)",
      "Pad 4 (choir)",
      "Pad 5 (bowed)",
      "Pad 6 (metallic)",
      "Pad 7 (halo)",
      "Pad 8 (sweep)",
      "FX 1 (rain)",
      "FX 2 (soundtrack)",
      "FX 3 (crystal)",
      "FX 4 (atmosphere)",
      "FX 5 (brightness)",
      "FX 6 (goblins)",
      "FX 7 (echoes)",
      "FX 8 (sci-fi)",
      "Sitar",
      "Banjo",
      "Shamisen",
      "Koto",
      "Kalimba",
      "Bag pipe",
      "Fiddle",
      "Shanai",
      "Tinkle Bell",
      "Agogo",
      "Steel Drums",
      "Woodblock",
      "Taiko Drum",
      "Melodic Tom",
      "Synth Drum",
      "Reverse Cymbal",
      "Guitar Fret Noise",
      "Breath Noise",
      "Seashore",
      "Bird Tweet",
      "Telephone Ring",
      "Helicopter",
      "Applause",
      "Gunshot"
    };
    return programNumber >= (byte) 0 && (int) programNumber < strArray.Length ? strArray[(int) programNumber] : "Unknown";
  }
}
