using VSInstrumentsBase.src.Midi;
using System;


namespace VSInstrumentsBase.src.Mapping.Mappers;

public sealed class NoteMapperOctave<T> : NoteMappingBase<

T>.NoteMapperBase
{
  private readonly 
  
  T?[] _values;
  private readonly Pitch _pitch;

  public NoteMapperOctave(Pitch pitch)
  {
    this._values = new T[(int) sbyte.MaxValue];
    this._pitch = pitch;
  }

  public override bool Add(Pitch pitch, T value)
  {
    if (pitch.PositionInOctave() != this._pitch.PositionInOctave())
      return false;
    int index = (int) pitch;
    if ((object) this._values[index] != null)
      return false;
    this._values[index] = value;
    return true;
  }

  public override bool Map(NoteMappingBase<T> destination)
  {
    int num1 = this._pitch.PositionInOctave();
    for (int index = 0; index < 10; ++index)
    {
      int num2 = index * 12;
      int sampleIndex = num2 + num1;
      T obj = this._values[sampleIndex];
      if ((object) obj != null)
      {
        int num3 = num2 + 12;
        for (int valueIndex = num2; valueIndex < num3; ++valueIndex)
          NoteMappingBase<T>.NoteMapperBase.Set(destination, valueIndex, sampleIndex, obj);
      }
    }
    return true;
  }

  public override void Dispose() => Array.Clear((Array) this._values);
}
