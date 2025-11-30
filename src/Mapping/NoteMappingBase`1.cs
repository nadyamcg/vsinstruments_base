// Decompiled with JetBrains decompiler
// Type: Instruments.Mapping.NoteMappingBase`1
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src.Midi;
using System;

#nullable disable
namespace VSInstrumentsBase.src.Mapping;

public abstract class NoteMappingBase<T>
{
  private readonly NoteMappingBase<T>.Item[] _entries;

  public NoteMappingBase() => this._entries = new NoteMappingBase<T>.Item[(int) sbyte.MaxValue];

  protected void Set(Pitch pitch, Pitch samplePitch, T value)
  {
    this._entries[(int) pitch] = new NoteMappingBase<T>.Item(value, samplePitch);
  }

  protected void Clear() => Array.Clear((Array) this._entries);

  protected NoteMappingBase<T>.Item GetItem(Pitch pitch) => this._entries[(int) pitch];

  public T GetValue(Pitch pitch) => this._entries[(int) pitch].Value;

  protected struct Item(T value, Pitch source)
  {
    public T Value = value;
    public Pitch Source = source;
  }

  public abstract class NoteMapperBase : IDisposable
  {
    public abstract bool Add(Pitch pitch, T value);

    public abstract bool Map(NoteMappingBase<T> destination);

    public abstract void Dispose();

    protected static void Set(
      NoteMappingBase<T> destination,
      int valueIndex,
      int sampleIndex,
      T value)
    {
      Pitch pitch = (Pitch) valueIndex;
      Pitch samplePitch = (Pitch) sampleIndex;
      destination.Set(pitch, samplePitch, value);
    }

    protected static void Clear(NoteMappingBase<T> destination) => destination.Clear();
  }
}
