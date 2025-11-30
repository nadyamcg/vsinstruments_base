// Decompiled with JetBrains decompiler
// Type: Instruments.Players.MidiFileInfo
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.IO;
using System.Linq;

#nullable disable
namespace VSInstrumentsBase.src.Players;

public class MidiFileInfo
{
  public readonly FileInfo FileInfo;
  private readonly MidiFile MidiFile;
  public readonly int BPM;
  public readonly double Duration;
  public readonly MidiTrackInfo[] Tracks;

  public bool Exists => ((FileSystemInfo) this.FileInfo).Exists;

  public bool IsMidi => this.MidiFile != null;

  public long SizeKB => (long) ((double) this.FileInfo.Length / 1000.0);

  public int TracksCount => this.Tracks.Length;

  public string FormatText
  {
    get
    {
      if (!this.IsMidi)
        return "Unknown";
      switch ((int) this.MidiFile.OriginalFormat)
      {
        case 0:
          return "Single track";
        case 1:
          return "Multi track";
        case 2:
          return "Multi song";
        default:
          return "Unknown";
      }
    }
  }

  public MidiFileInfo(string path)
  {
    this.FileInfo = new FileInfo(path);
    if (!((FileSystemInfo) this.FileInfo).Exists)
      return;
    try
    {
      this.MidiFile = MidiFile.Read(path, (ReadingSettings) null);
    }
    catch
    {
      return;
    }
    TempoMap tempoMap = TempoMapManagingUtilities.GetTempoMap(this.MidiFile);
    this.BPM = (int) Math.Round(tempoMap.GetTempoAtTime((ITimeSpan) new MetricTimeSpan(0L)).BeatsPerMinute);
    TimedEvent timedEvent = TimedEventsManagingUtilities.GetTimedEvents(this.MidiFile, (TimedEventDetectionSettings) null).LastOrDefault<TimedEvent>();
    this.Duration = timedEvent == null ? 0.0 : TimeConverter.ConvertTo<MetricTimeSpan>(timedEvent.Time, tempoMap).TotalSeconds;
    TrackChunk[] array = Melanchall.DryWetMidi.Core.TrackChunkUtilities.GetTrackChunks(this.MidiFile).ToArray<TrackChunk>();
    this.Tracks = new MidiTrackInfo[array.Length];
    for (int trackIndex = 0; trackIndex < array.Length; ++trackIndex)
      this.Tracks[trackIndex] = new MidiTrackInfo(this.MidiFile, trackIndex, tempoMap);
  }

  public MidiFile GetMidiFile() => this.MidiFile;
}
