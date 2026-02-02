using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.IO;
using System.Linq;


namespace VSInstrumentsBase.src.Players;

public class MidiFileInfo
{
  public readonly FileInfo FileInfo;
  private readonly MidiFile MidiFile;
  public readonly int BPM;
  public readonly double Duration;
  public readonly MidiTrackInfo[] Tracks;
  public readonly string ParseError;

  public bool Exists => FileInfo?.Exists ?? false;

  public bool IsMidi => MidiFile != null;

  public long SizeKB
  {
    get
    {
      if (!Exists)
        return 0;
      try
      {
        FileInfo.Refresh();
        return (long)(FileInfo.Length / 1000.0);
      }
      catch
      {
        return 0;
      }
    }
  }

  public int TracksCount => Tracks?.Length ?? 0;

  public string FormatText
  {
    get
    {
      if (!IsMidi)
        return ParseError != null ? "Parse Error" : "Unknown";
      return (int)MidiFile.OriginalFormat switch
      {
        0 => "Single track",
        1 => "Multi track",
        2 => "Multi song",
        _ => "Unknown"
      };
    }
  }

  public MidiFileInfo(string path)
  {
    FileInfo = new FileInfo(path);
    Tracks = Array.Empty<MidiTrackInfo>();

    if (!FileInfo.Exists)
    {
      ParseError = "File does not exist";
      return;
    }

    if (FileInfo.Length == 0)
    {
      ParseError = "File is empty (0 bytes)";
      return;
    }

    try
    {
      MidiFile = MidiFile.Read(path, (ReadingSettings)null);
    }
    catch (Exception ex)
    {
      ParseError = $"MIDI parse failed: {ex.Message}";
      return;
    }

    try
    {
      TempoMap tempoMap = TempoMapManagingUtilities.GetTempoMap(MidiFile);
      BPM = (int)Math.Round(tempoMap.GetTempoAtTime((ITimeSpan)new MetricTimeSpan(0L)).BeatsPerMinute);
      TimedEvent timedEvent = TimedEventsManagingUtilities.GetTimedEvents(MidiFile, (TimedEventDetectionSettings)null).LastOrDefault<TimedEvent>();
      Duration = timedEvent == null ? 0.0 : TimeConverter.ConvertTo<MetricTimeSpan>(timedEvent.Time, tempoMap).TotalSeconds;
      TrackChunk[] array = Melanchall.DryWetMidi.Core.TrackChunkUtilities.GetTrackChunks(MidiFile).ToArray<TrackChunk>();
      Tracks = new MidiTrackInfo[array.Length];
      for (int trackIndex = 0; trackIndex < array.Length; ++trackIndex)
        Tracks[trackIndex] = new MidiTrackInfo(MidiFile, trackIndex, tempoMap);
    }
    catch (Exception ex)
    {
      ParseError = $"MIDI metadata extraction failed: {ex.Message}";
    }
  }

  public MidiFile GetMidiFile() => MidiFile;
}
