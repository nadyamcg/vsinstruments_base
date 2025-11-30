using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace VSInstrumentsBase.src.Players;

public class MidiTrackInfo
{
  public readonly int Index;
  public readonly double Duration;
  public readonly int NoteCount;
  public readonly double? FirstNoteTime;
  public readonly byte? Instrument;

  public string InstrumentText
  {
    get
    {
      return !this.Instrument.HasValue ? "Unknown" : MidiExtensions.GetInstrumentName(this.Instrument.Value);
    }
  }

  public MidiTrackInfo(MidiFile midi, int trackIndex, TempoMap tempoMap)
  {
    this.Index = trackIndex;
    TrackChunk trackChunk = Melanchall.DryWetMidi.Core.TrackChunkUtilities.GetTrackChunks(midi).ElementAt<TrackChunk>(trackIndex);
    ICollection<Note> notes = NotesManagingUtilities.GetNotes(trackChunk, (NoteDetectionSettings) null, (TimedEventDetectionSettings) null);
    this.NoteCount = notes.Count<Note>();
    TimedEvent timedEvent = TimedEventsManagingUtilities.GetTimedEvents(trackChunk, (TimedEventDetectionSettings) null).LastOrDefault<TimedEvent>();
    this.Duration = timedEvent == null ? 0.0 : TimeConverter.ConvertTo<MetricTimeSpan>(timedEvent.Time, tempoMap).TotalSeconds;
    Note note = notes.FirstOrDefault<Note>();
    this.FirstNoteTime = note == null ? new double?() : new double?(TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap).TotalSeconds);
    SevenBitNumber? programNumber = ((IEnumerable) trackChunk.Events).OfType<ProgramChangeEvent>().FirstOrDefault<ProgramChangeEvent>()?.ProgramNumber;
    this.Instrument = programNumber.HasValue ? new byte?((byte)programNumber.GetValueOrDefault()) : new byte?();
  }
}
