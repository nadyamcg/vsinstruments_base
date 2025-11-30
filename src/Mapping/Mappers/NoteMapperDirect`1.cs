// Decompiled with JetBrains decompiler
// Type: Instruments.Mapping.Mappers.NoteMapperDirect`1
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src.Midi;
using System;

#nullable enable
namespace VSInstrumentsBase.src.Mapping.Mappers;

public sealed class NoteMapperDirect<T> : NoteMappingBase<
#nullable disable
T>.NoteMapperBase
{
  private readonly 
  #nullable enable
  T?[] values;

  public NoteMapperDirect() => this.values = new T[(int) sbyte.MaxValue];

  public override bool Add(Pitch pitch, T value)
  {
    this.values[(int) pitch] = value;
    return true;
  }

  public override bool Map(NoteMappingBase<T> destination)
  {
    for (int index = 0; index < (int) sbyte.MaxValue; ++index)
      NoteMappingBase<T>.NoteMapperBase.Set(destination, index, index, this.values[index]);
    return true;
  }

  public override void Dispose() => Array.Clear((Array) this.values);
}
