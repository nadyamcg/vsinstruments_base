using VSInstrumentsBase.src;
using VSInstrumentsBase.src.Types;
using VSInstrumentsBase.src.Midi;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;


namespace VSInstrumentsBase.src.Players;

public class MidiPlayer(ICoreAPI api, IPlayer source, InstrumentType instrumentType) :
  MidiPlayerBase(api, instrumentType),
  IDisposable
{
  private readonly ILoadedSound[] _sounds = new ILoadedSound[Constants.Note.NoteCount];

  private readonly IPlayer _source = source;

  protected override void OnNoteOn(Pitch pitch, float velocity, int channel, float time)
  {
    if (CoreAPI is not ICoreClientAPI clientAPI)
    {
      return;
    }

    int index = (int)pitch;
    TryRemoveSound(index, Constants.Playback.MinFadeOutDuration);

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

  protected override void OnNoteOff(Pitch pitch, float velocity, int channel, float time)
  {
    if (CoreAPI is not ICoreClientAPI)
    {
      return;
    }

    int index = (int)pitch;
    float fadeDuration = Constants.Playback.GetFadeDurationFromVelocity(velocity);
    TryRemoveSound(index, fadeDuration);
  }

  protected override bool IsSourceValid()
  {
    return _source != null && _source.Entity != null;
  }

  protected override Vec3f GetSourcePosition()
  {
    return _source.Entity.Pos.XYZFloat;
  }

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

  protected override void OnStop()
  {
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
