using VSInstrumentsBase.src.Midi;
using System;


namespace VSInstrumentsBase.src.Mapping.Mappers;

public sealed class NoteMapperDirect<T> : NoteMappingBase<

T>.NoteMapperBase
{
  private readonly 
  
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
