// Decompiled with JetBrains decompiler
// Type: Instruments.Players.MidiPlayer
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src;
using VSInstrumentsBase.src.Types;
using VSInstrumentsBase.src.Midi;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

#nullable disable
namespace VSInstrumentsBase.src.Players;

public class MidiPlayer(ICoreAPI api, IPlayer source, InstrumentType instrumentType) : 
  MidiPlayerBase(api, instrumentType),
  IDisposable
{
  private readonly ILoadedSound[] _sounds;
  private readonly IPlayer _source = source;

  protected override void OnNoteOn(Pitch pitch, float velocity, int channel, float time)
  {
    if (!(this.CoreAPI is ICoreClientAPI coreApi))
      return;
    int index = (int) pitch;
    this.TryRemoveSound(index, 0.25f);
    SoundParams soundParams = this.CreateSoundParams(pitch, velocity, channel, time, this.InstrumentType);
    if (soundParams == null)
      return;
    ILoadedSound iloadedSound = coreApi.World.LoadSound(soundParams);
    if (iloadedSound != null)
    {
      this._sounds[index] = iloadedSound;
      iloadedSound.Start();
    }
  }

  protected override void OnNoteOff(Pitch pitch, float velocity, int channel, float time)
  {
    if (!(this.CoreAPI is ICoreClientAPI))
      return;
    this.TryRemoveSound((int) pitch, Constants.Playback.GetFadeDurationFromVelocity(velocity));
  }

  protected override bool IsSourceValid() => this._source != null && this._source.Entity != null;

  protected override Vec3f GetSourcePosition() => ((Entity) this._source.Entity).Pos.XYZFloat;

  protected override void SetPosition(Vec3f sourcePosition)
  {
    for (int index = 0; index < this._sounds.Length; ++index)
      this._sounds[index]?.SetPosition(sourcePosition);
  }

  private void TryRemoveSound(int index, float fadeDuration)
  {
    ILoadedSound sound = this._sounds[index];
    if (sound == null)
      return;
    if ((double) fadeDuration <= 0.0)
    {
      ((IDisposable) sound).Dispose();
      this._sounds[index] = (ILoadedSound) null;
    }
    else
    {
      sound.FadeOutAndStop(fadeDuration);
      this._sounds[index] = (ILoadedSound) null;
    }
  }

  protected override void OnStop()
  {
    this.StopAllSounds(0.25f);
    base.OnStop();
  }

  protected void StopAllSounds(float fadeDuration)
  {
    for (int index = 0; index < this._sounds.Length; ++index)
      this.TryRemoveSound(index, fadeDuration);
    Array.Clear((Array) this._sounds);
  }

  public override void Dispose() => this.StopAllSounds(0.0f);
}
