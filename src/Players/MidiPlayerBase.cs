using VSInstrumentsBase.src;
using VSInstrumentsBase.src.Types;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using VSInstrumentsBase.src.Midi;
using System;
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

  private TimedEvent[] _events;

  private int _channel;

  private bool _isPaused;

  // wall-clock anchor for playback time. _elapsedTime is derived as
  // _seekOffsetSec + (now - _anchorMs)/1000 so multiple players stay locked
  // to a shared monotonic clock instead of accumulating float deltaTime.
  private long _anchorMs;

  private double _seekOffsetSec;

  private long _pausedAtMs;

  // wall-clock time of the previous Update, and a smoothed estimate of how far
  // apart updates actually land. used to centre note dispatch, see Update.
  private long _lastUpdateMs;

  private double _updateIntervalSec;

  protected ICoreAPI CoreAPI { get; private set; } = api;

  protected InstrumentType InstrumentType { get; private set; } = instrumentType;

  public double Duration => this._duration;

  public double ElapsedTime => this._elapsedTime;

  public bool IsPaused => this._isPaused;

  public bool IsPlaying => this._midiTrack != null && this._elapsedTime < this._duration;

  public bool IsFinished => this._midiTrack != null && this._elapsedTime >= this._duration;

  private static long GetDuration(TrackChunk track)
  {
    TimedEvent timedEvent = TimedEventsManagingUtilities.GetTimedEvents(track, (TimedEventDetectionSettings) null).LastOrDefault<TimedEvent>();
    return timedEvent != null ? timedEvent.Time : 0L;
  }

  private long TimeToTicks(double seconds)
  {
    // use the tempo map (not a single fixed BPM) so tempo events and
    // fractional BPMs in the source MIDI are honoured. matches the time-base
    // used by the server in MidiExtensions.ReadTrackDuration.
    long us = (long) Math.Round(seconds * 1_000_000.0);
    return TimeConverter.ConvertFrom(new MetricTimeSpan(us), this._tempoMap);
  }

  private double TicksToTime(long ticks)
  {
    return TimeConverter.ConvertTo<MetricTimeSpan>(ticks, this._tempoMap).TotalSeconds;
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

    // cache the timed-event array once. previously this was rebuilt every
    // Update tick which allocated per frame and caused GC hitches on long
    // tracks, which in turn manifested as audible timing jitter on drums.
    this._events = TimedEventsManagingUtilities.GetTimedEvents(this._midiTrack, (TimedEventDetectionSettings) null).ToArray();

    this._elapsedTime = 0.0;
    this._ticksDuration = (int) MidiPlayerBase.GetDuration(this._midiTrack);
    this._duration = this.TicksToTime((long) this._ticksDuration);

    this._channel = channel;
    this._eventIndex = 0;
    this._seekOffsetSec = 0.0;
    this._anchorMs = ((IWorldAccessor) this.CoreAPI.World).ElapsedMilliseconds;
    this._pausedAtMs = 0L;
    this._lastUpdateMs = 0L;
    this._updateIntervalSec = 0.0;
  }

  public void Play(string midiFilePath, int channel)
  {
    MidiFile midiFile = MidiFile.Read(midiFilePath, (ReadingSettings) null);
    this.Play(midiFile, channel);
  }

  public void Pause()
  {
    if (!this.IsPlaying)
      throw new InvalidOperationException("Cannot pause, player is not playing!");
    this._isPaused = true;
    this._pausedAtMs = ((IWorldAccessor) this.CoreAPI.World).ElapsedMilliseconds;
  }

  public void Resume()
  {
    if (!this._isPaused)
      throw new InvalidOperationException("Cannot resume, player is not paused!");
    // shift the anchor forward by the paused duration so derived elapsed time
    // continues from where it was, not from a phantom moment in the past.
    long now = ((IWorldAccessor) this.CoreAPI.World).ElapsedMilliseconds;
    this._anchorMs += (now - this._pausedAtMs);
    this._pausedAtMs = 0L;
    this._isPaused = false;
    // the pause gap is not a frame time; drop it rather than let it skew the
    // interval estimate. the smoothed value itself is still valid.
    this._lastUpdateMs = 0L;
  }

  public void Update(float deltaTime)
  {
    if (!this.IsPlaying)
      throw new InvalidOperationException("Player is not playing!");

    if (this._isPaused)
      return;

    // derive elapsed time from the world clock anchor instead of accumulating
    // deltaTime.
    long nowMs = ((IWorldAccessor) this.CoreAPI.World).ElapsedMilliseconds;

    // track how far apart updates actually land. this is measured rather than
    // assumed because the render loop's frame time varies.
    if (this._lastUpdateMs != 0L)
    {
      double observed = (double) (nowMs - this._lastUpdateMs) / 1000.0;
      // ignore hitches
      if (observed > 0.0 && observed < 0.5)
      {
        this._updateIntervalSec = this._updateIntervalSec <= 0.0
          ? observed
          : this._updateIntervalSec * 0.9 + observed * 0.1;
      }
    }
    this._lastUpdateMs = nowMs;

    this._elapsedTime = this._seekOffsetSec + (double) (nowMs - this._anchorMs) / 1000.0;
    long elapsedTicks = this.TimeToTicks(this._elapsedTime);

    // clamp to bounds, playback is complete.
    if (elapsedTicks > (long) this._ticksDuration)
      this._elapsedTime = this._duration;

    // a note can only ever be started on an update boundary, so dispatching on
    // "time has passed" alone makes every note late by up to a full interval,
    // and by a different amount each time. reaching half an interval ahead
    // centres that error on zero instead of skewing it entirely late, and it
    // keeps clients running at different framerates in phase with each other.
    long dispatchTicks = this.TimeToTicks(this._elapsedTime + this._updateIntervalSec * 0.5);

    // process all MIDI events that should occur at this time
    TimedEvent[] array = this._events;
    for (; this._eventIndex < array.Length; ++this._eventIndex)
    {
      TimedEvent timedEvent = array[this._eventIndex];
      if (timedEvent.Time <= dispatchTicks)
        this.ProcessMidiEvent(timedEvent.Event);
      else
        break;
    }

    // per-frame subclass hook (modulation LFO, etc.)
    this.OnUpdate(deltaTime);

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
      case PitchBendEvent pitchBendEvent:
        this.OnPitchBend((int)(byte)pitchBendEvent.Channel, pitchBendEvent.PitchValue, elapsedTime);
        break;
      case ControlChangeEvent controlChangeEvent:
        this.OnControlChange((int)(byte)controlChangeEvent.Channel, (byte)controlChangeEvent.ControlNumber, (byte)controlChangeEvent.ControlValue, elapsedTime);
        break;
    }
  }

  // processes only expression events (pitch bend, CC)
  // used during seek state rollup
  private void ProcessExpressionEvent(MidiEvent midiEvent)
  {
    if (midiEvent is PitchBendEvent || midiEvent is ControlChangeEvent)
      this.ProcessMidiEvent(midiEvent);
  }

  public void Seek(double time)
  {
    if (!this.IsPlaying)
      throw new InvalidOperationException("Player is not playing!");

    long timeInTicks = this.TimeToTicks(time);
    long durationInTicks = MidiPlayerBase.GetDuration(this._midiTrack);
    if (timeInTicks > durationInTicks)
      throw new ArgumentOutOfRangeException("Player cannot seek beyond its end!");

    TimedEvent[] array = this._events;

    // default past end in case seek target is beyond all events
    this._eventIndex = array.Length;
    for (int index = 0; index < array.Length; ++index)
    {
      if (array[index].Time >= timeInTicks)
      {
        this._eventIndex = index;
        break;
      }
      // replay expression events before the seek point so pitch bend / CC state
      // is correct when playback resumes
      // note events are skipped intentionally
      this.ProcessExpressionEvent(array[index].Event);
    }

    // re-anchor the wall-clock baseline so derived elapsed time picks up from
    // the seek target. without this, the next Update would snap right back.
    this._elapsedTime = this.TicksToTime(timeInTicks);
    this._seekOffsetSec = this._elapsedTime;
    this._anchorMs = ((IWorldAccessor) this.CoreAPI.World).ElapsedMilliseconds;
    this._pausedAtMs = 0L;
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
    this._events = null;
    this._beatsPerMinute = 120;
    this._ticksPerQuarterNote = 0;
    this._elapsedTime = 0.0;
    this._ticksDuration = 0;
    this._duration = 0.0;
    this._eventIndex = 0;
    this._channel = 0;
    this._seekOffsetSec = 0.0;
    this._anchorMs = 0L;
    this._pausedAtMs = 0L;
    this._lastUpdateMs = 0L;
    this._updateIntervalSec = 0.0;

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

  protected abstract void OnPitchBend(int channel, ushort pitchValue, float time);

  protected abstract void OnControlChange(int channel, byte controlNumber, byte controlValue, float time);

  protected virtual void OnUpdate(float deltaTime)
  {
  }

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
