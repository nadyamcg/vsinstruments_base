using VSInstrumentsBase.src;
using VSInstrumentsBase.src.Types;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using VSInstrumentsBase.src.Midi;
using System;
using System.Diagnostics;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;


namespace VSInstrumentsBase.src.Players;

public abstract class MidiPlayerBase(ICoreAPI api, InstrumentType instrumentType) : IDisposable
{
  private TrackChunk _midiTrack;

  private int _beatsPerMinute;

  private int _ticksPerQuarterNote;

  private int _ticksDuration;

  private double _elapsedTime;

  private double _duration;

  private int _eventIndex;

  private TempoMap _tempoMap;

  private int _channel;

  protected ICoreAPI CoreAPI { get; private set; } = api;

  protected InstrumentType InstrumentType { get; private set; } = instrumentType;

  public double Duration => this._duration;

  public double ElapsedTime => this._elapsedTime;

  public bool IsPlaying => this._midiTrack != null && this._elapsedTime < this._duration;

  public bool IsFinished => this._midiTrack != null && this._elapsedTime >= this._duration;

  private static long GetDuration(TrackChunk track)
  {
    TimedEvent timedEvent = TimedEventsManagingUtilities.GetTimedEvents(track, (TimedEventDetectionSettings) null).LastOrDefault<TimedEvent>();
    return timedEvent != null ? timedEvent.Time : 0L;
  }

  private long TimeToTicks(double seconds)
  {
    return MidiExtensions.TimeToTicks(seconds, this._beatsPerMinute, this._ticksPerQuarterNote);
  }

  private double TicksToTime(long ticks)
  {
    return MidiExtensions.TicksToTime(ticks, this._beatsPerMinute, this._ticksPerQuarterNote);
  }

  public void Play(MidiFile midi, int channel)
  {
    if (this.IsPlaying)
      throw new InvalidOperationException("Cannot start MIDI playback, the player is already playing!");

    if (midi == null)
      throw new InvalidOperationException("Cannot start MIDI playback, the provided file is invalid!");

    TrackChunk[] trackChunkArray = Melanchall.DryWetMidi.Core.TrackChunkUtilities.GetTrackChunks(midi).ToArray<TrackChunk>();
    if (channel >= trackChunkArray.Length)
      throw new ArgumentOutOfRangeException(nameof (channel), "Track index out of range");

    this.CoreAPI.Logger.Notification($"[MidiPlayerBase] Starting playback: {trackChunkArray.Length} tracks, channel {channel}, instrument {this.InstrumentType?.Name ?? "unknown"}");

    this._midiTrack = trackChunkArray[channel];
    this._tempoMap = TempoMapManagingUtilities.GetTempoMap(midi);

    this._beatsPerMinute = midi.ReadBPM();
    this._ticksPerQuarterNote = midi.TimeDivision is TicksPerQuarterNoteTimeDivision timeDivision ? (int) timeDivision.TicksPerQuarterNote : 480;

    this._elapsedTime = 0.0;
    this._ticksDuration = (int) MidiPlayerBase.GetDuration(this._midiTrack);
    this._duration = this.TicksToTime((long) this._ticksDuration);

    this._channel = channel;
    this._eventIndex = 0;
  }

  public void Play(string midiFilePath, int channel)
  {
    MidiFile midiFile = MidiFile.Read(midiFilePath, (ReadingSettings) null);
    this.Play(midiFile, channel);
  }

  public void Update(float deltaTime)
  {
    if (!this.IsPlaying)
      throw new InvalidOperationException("Player is not playing!");

    this._elapsedTime += (double) deltaTime;
    long elapsedTicks = this.TimeToTicks(this._elapsedTime);

    // clamp to bounds, playback is complete.
    if (elapsedTicks > (long) this._ticksDuration)
      this._elapsedTime = this._duration;

    // process all MIDI events that should occur at this time
    TimedEvent[] array = TimedEventsManagingUtilities.GetTimedEvents(this._midiTrack, (TimedEventDetectionSettings) null).ToArray<TimedEvent>();
    for (; this._eventIndex < array.Length; ++this._eventIndex)
    {
      TimedEvent timedEvent = array[this._eventIndex];
      if (timedEvent.Time <= elapsedTicks)
        this.ProcessMidiEvent(timedEvent.Event);
      else
        break;
    }

    // grab new source position and update all sounds
    Vec3f sourcePosition = this.GetSourcePosition();
    this.SetPosition(sourcePosition);
  }

  private void ProcessMidiEvent(MidiEvent midiEvent)
  {
    float elapsedTime = (float) this._elapsedTime;
    switch (midiEvent)
    {
      case NoteOffEvent noteOffEvent:
        this.OnNoteOff((Pitch)(byte)(((NoteEvent)noteOffEvent).NoteNumber), Constants.Midi.NormalizeVelocity((byte)(((NoteEvent)noteOffEvent).Velocity)), (int)(byte)(((ChannelEvent)noteOffEvent).Channel), elapsedTime);
        break;
      case NoteOnEvent noteOnEvent:
        if ((byte)(((NoteEvent)noteOnEvent).Velocity) == (byte) 0)
        {
          this.OnNoteOff((Pitch)(byte)(((NoteEvent)noteOnEvent).NoteNumber), Constants.Midi.NormalizeVelocity((byte)(((NoteEvent)noteOnEvent).Velocity)), (int)(byte)(((ChannelEvent)noteOnEvent).Channel), elapsedTime);
          break;
        }
        this.OnNoteOn((Pitch)(byte)(((NoteEvent)noteOnEvent).NoteNumber), Constants.Midi.NormalizeVelocity((byte)(((NoteEvent)noteOnEvent).Velocity)), (int)(byte)(((ChannelEvent)noteOnEvent).Channel), elapsedTime);
        break;
    }
  }

  public void Seek(double time)
  {
    if (!this.IsPlaying)
      throw new InvalidOperationException("Player is not playing!");

    long timeInTicks = this.TimeToTicks(time);
    long durationInTicks = MidiPlayerBase.GetDuration(this._midiTrack);
    if (timeInTicks > durationInTicks)
      throw new ArgumentOutOfRangeException("Player cannot seek beyond its end!");

    // find the nearest event
    TimedEvent[] array = TimedEventsManagingUtilities.GetTimedEvents(this._midiTrack, (TimedEventDetectionSettings) null).ToArray<TimedEvent>();
    for (int index = 0; index < array.Length; ++index)
    {
      if (array[index].Time >= timeInTicks)
      {
        this._eventIndex = index;
        break;
      }
    }

    this._elapsedTime = this.TicksToTime(timeInTicks);
  }

  public bool TrySeek(double time)
  {
    if (!this.IsPlaying)
      return false;

    double clampedTime = Math.Min(Math.Max(time, 0.0), this.Duration);
    this.Seek(clampedTime);
    return true;
  }

  public void Stop()
  {
    if (!this.IsPlaying && !this.IsFinished)
      throw new InvalidOperationException("Cannot stop MIDI playback, the player is not playing!");

    this._midiTrack = (TrackChunk) null;
    this._tempoMap = (TempoMap) null;
    this._beatsPerMinute = 120;
    this._ticksPerQuarterNote = 0;
    this._elapsedTime = 0.0;
    this._ticksDuration = 0;
    this._duration = 0.0;
    this._eventIndex = 0;
    this._channel = 0;

    this.OnStop();
  }

  public bool TryStop()
  {
    if (!this.IsPlaying)
      return false;
    this.Stop();
    return true;
  }

  protected abstract void OnNoteOn(Pitch pitch, float velocity, int channel, float time);

  protected abstract void OnNoteOff(Pitch pitch, float velocity, int channel, float time);

  protected virtual void OnStop()
  {
  }

  protected SoundParams CreateSoundParams(
    Pitch pitch,
    float velocity,
    int channel,
    float time,
    InstrumentType instrumentType)
  {
    if (this.IsSourceValid() && instrumentType != null && instrumentType.GetPitchSound(pitch, out string assetPath, out float soundPitch))
    {
            SoundParams soundParams = new(new AssetLocation("instruments", assetPath))
            {
                Volume = Constants.Playback.GetVolumeFromVelocity(velocity),
                DisposeOnFinish = true,
                RelativePosition = false,
                Position = this.GetSourcePosition(),
                Pitch = soundPitch
            };
            return soundParams;
    }

    return null;
  }

  protected abstract bool IsSourceValid();

  protected abstract Vec3f GetSourcePosition();

  protected abstract void SetPosition(Vec3f sourcePosition);

  public abstract void Dispose();
}
