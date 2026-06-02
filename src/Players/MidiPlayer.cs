using VSInstrumentsBase.src;
using VSInstrumentsBase.src.Types;
using VSInstrumentsBase.src.Midi;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;


namespace VSInstrumentsBase.src.Players;

public class MidiPlayer(ICoreAPI api, IPlayer source, InstrumentType instrumentType, Vec3f fixedPosition = null) :
  MidiPlayerBase(api, instrumentType),
  IDisposable
{
  private readonly ILoadedSound[] _sounds = new ILoadedSound[Constants.Note.NoteCount];
  private readonly float[] _soundBasePitches = new float[Constants.Note.NoteCount];
  private readonly float[] _soundBaseVolumes = new float[Constants.Note.NoteCount];

  private readonly IPlayer _source = source;
  private readonly Vec3f _fixedPosition = fixedPosition;

  // pitch bend state
  private float _pitchBendRange = Constants.Midi.DefaultPitchBendRange;
  private float _pitchBendMultiplier = 1f;

  // RPN selection state (for setting pitch bend range and other parameters)
  private int _rpnMsb = -1;
  private int _rpnLsb = -1;

  // modulation / vibrato LFO
  private float _modulationDepth = 0f;
  private float _lfoPhase = 0f;

  // volume scaling from CC#7 / CC#11
  private float _volumeScale = 1f;

  // sustain pedal
  private bool _sustainHeld = false;
  private readonly HashSet<int> _pendingNoteOffs = new();

  protected override void OnNoteOn(Pitch pitch, float velocity, int channel, float time)
  {
    if (CoreAPI is not ICoreClientAPI clientAPI)
      return;

    int index = (int)pitch;
    TryRemoveSound(index, Constants.Playback.MinFadeOutDuration);

    // clear any pending note-off for this pitch if we're retriggering
    _pendingNoteOffs.Remove(index);

    SoundParams soundParams = CreateSoundParams(pitch, velocity, channel, time, InstrumentType);
    if (soundParams == null)
      return;

    // scale volume by current channel volume CC state
    soundParams.Volume *= _volumeScale;

    ILoadedSound sound = clientAPI.World.LoadSound(soundParams);
    if (sound == null)
      return;

    _sounds[index] = sound;
    _soundBasePitches[index] = soundParams.Pitch;
    _soundBaseVolumes[index] = soundParams.Volume;
    sound.Start();

    // apply any active pitch bend immediately so notes starting mid-bend are correct
    if (_pitchBendMultiplier != 1f || _modulationDepth > 0f)
      ApplyPitchToSound(index);
  }

  protected override void OnNoteOff(Pitch pitch, float velocity, int channel, float time)
  {
    if (CoreAPI is not ICoreClientAPI)
      return;

    int index = (int)pitch;

    // sustain pedal holds the note: defer the note-off until the pedal releases
    if (_sustainHeld)
    {
      _pendingNoteOffs.Add(index);
      return;
    }

    float fadeDuration = Constants.Playback.GetFadeDurationFromVelocity(velocity);
    TryRemoveSound(index, fadeDuration);
  }

  protected override void OnPitchBend(int channel, ushort pitchValue, float time)
  {
    float normalized = (pitchValue - Constants.Midi.PitchBendCenter) / (float)Constants.Midi.PitchBendCenter;
    float semitones = normalized * _pitchBendRange;
    _pitchBendMultiplier = (float)Math.Pow(2.0, semitones / 12.0);
    UpdateAllPitch();
  }

  protected override void OnControlChange(int channel, byte controlNumber, byte controlValue, float time)
  {
    switch (controlNumber)
    {
      case Constants.Midi.CC_Modulation:
        _modulationDepth = controlValue / 127f;
        // reset LFO phase on modulation activation for consistent feel
        if (_modulationDepth == 0f)
        {
          _lfoPhase = 0f;
          UpdateAllPitch();
        }
        break;

      case Constants.Midi.CC_ChannelVolume:
      case Constants.Midi.CC_Expression:
        _volumeScale = controlValue / 127f;
        UpdateAllVolume();
        break;

      case Constants.Midi.CC_Sustain:
        bool held = controlValue >= 64;
        if (_sustainHeld && !held)
        {
          // pedal released: flush all deferred note-offs
          _sustainHeld = false;
          foreach (int index in _pendingNoteOffs)
            TryRemoveSound(index, Constants.Playback.MinFadeOutDuration);
          _pendingNoteOffs.Clear();
        }
        else
        {
          _sustainHeld = held;
        }
        break;

      case Constants.Midi.CC_RpnMsb:
        _rpnMsb = controlValue;
        break;

      case Constants.Midi.CC_RpnLsb:
        _rpnLsb = controlValue;
        break;

      case Constants.Midi.CC_DataEntry:
        // RPN 0,0 is pitch bend sensitivity (semitones)
        if (_rpnMsb == 0 && _rpnLsb == 0)
          _pitchBendRange = controlValue;
        break;
    }
  }

  protected override void OnUpdate(float deltaTime)
  {
    if (_modulationDepth <= 0f)
      return;

    _lfoPhase += deltaTime * Constants.Midi.VibratoFrequency * 2f * MathF.PI;
    UpdateAllPitch();
  }

  // applies current pitch bend + vibrato to a single sound slot
  private void ApplyPitchToSound(int index)
  {
    ILoadedSound sound = _sounds[index];
    if (sound == null)
      return;

    float vibratoSemitones = MathF.Sin(_lfoPhase) * _modulationDepth * Constants.Midi.MaxVibratoSemitones;
    float vibratoMultiplier = vibratoSemitones != 0f
      ? (float)Math.Pow(2.0, vibratoSemitones / 12.0)
      : 1f;

    sound.SetPitch(_soundBasePitches[index] * _pitchBendMultiplier * vibratoMultiplier);
  }

  // recomputes and applies pitch to all active sounds
  private void UpdateAllPitch()
  {
    float vibratoSemitones = MathF.Sin(_lfoPhase) * _modulationDepth * Constants.Midi.MaxVibratoSemitones;
    float vibratoMultiplier = vibratoSemitones != 0f
      ? (float)Math.Pow(2.0, vibratoSemitones / 12.0)
      : 1f;

    for (int i = 0; i < _sounds.Length; i++)
    {
      ILoadedSound sound = _sounds[i];
      if (sound == null)
        continue;

      sound.SetPitch(_soundBasePitches[i] * _pitchBendMultiplier * vibratoMultiplier);
    }
  }

  // applies the current volume scale to all active sounds
  private void UpdateAllVolume()
  {
    for (int i = 0; i < _sounds.Length; i++)
    {
      ILoadedSound sound = _sounds[i];
      if (sound == null)
        continue;

      sound.SetVolume(_soundBaseVolumes[i] * _volumeScale);
    }
  }

  protected override bool IsSourceValid()
  {
    if (_fixedPosition != null)
      return true;
    return _source != null && _source.Entity != null;
  }

  protected override Vec3f GetSourcePosition()
  {
    return _fixedPosition ?? _source.Entity.Pos.XYZFloat;
  }

  protected override void SetPosition(Vec3f sourcePosition)
  {
    for (int i = 0; i < _sounds.Length; ++i)
    {
      ILoadedSound sound = _sounds[i];
      if (sound == null)
        continue;

      sound.SetPosition(sourcePosition);
    }
  }

  private void TryRemoveSound(int index, float fadeDuration)
  {
    ILoadedSound sound = _sounds[index];
    if (sound == null)
      return;

    if (fadeDuration <= 0)
      sound.Dispose();
    else
      sound.FadeOutAndStop(fadeDuration);

    _sounds[index] = null;
  }

  protected override void OnStop()
  {
    // clear expression state so it doesn't bleed into the next playback
    _sustainHeld = false;
    _pendingNoteOffs.Clear();
    _pitchBendMultiplier = 1f;
    _modulationDepth = 0f;
    _lfoPhase = 0f;
    _volumeScale = 1f;
    _pitchBendRange = Constants.Midi.DefaultPitchBendRange;
    _rpnMsb = -1;
    _rpnLsb = -1;

    StopAllSounds(Constants.Playback.MinFadeOutDuration);
    base.OnStop();
  }

  protected void StopAllSounds(float fadeDuration)
  {
    for (int i = 0; i < _sounds.Length; ++i)
      TryRemoveSound(i, fadeDuration);

    Array.Clear(_sounds);
  }

  public override void Dispose()
  {
    StopAllSounds(0);
  }
}
