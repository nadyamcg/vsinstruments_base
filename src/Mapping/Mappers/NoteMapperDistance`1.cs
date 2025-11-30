using VSInstrumentsBase.src.Midi;
using System;


namespace VSInstrumentsBase.src.Mapping.Mappers;

public sealed class NoteMapperDistance<T> : NoteMappingBase<

T>.NoteMapperBase
{
  private readonly 
  
  T?[] values;

  public NoteMapperDistance() => this.values = new T[(int) sbyte.MaxValue];

  public override bool Add(Pitch pitch, T value)
  {
    int index = (int) pitch;
    if ((object) this.values[index] != null)
      return false;
    this.values[index] = value;
    return true;
  }

  public override bool Map(NoteMappingBase<T> destination)
  {
    if (countValues() <= 0)
      return false;
    int? lo = new int?();
    int? upperBound = findUpperBound(new int?(0));
    fill(lo, upperBound);
    do
    {
      int? nullable = upperBound;
      upperBound = findUpperBound(nullable);
      fill(nullable, upperBound);
    }
    while (upperBound.HasValue);
    return true;

    int countValues()
    {
      int num = 0;
      for (int index = 0; index < this.values.Length; ++index)
      {
        if ((object) this.values[index] != null)
          ++num;
      }
      return num;
    }

    int? findUpperBound(int? start)
    {
      if (!start.HasValue)
        return new int?();
      for (int index = start.Value + 1; index < (int) sbyte.MaxValue; ++index)
      {
        if ((object) this.values[index] != null)
          return new int?(index);
      }
      return new int?();
    }

    void fill(int? lo, int? hi, int length = 127 )
    {
      if (!lo.HasValue && hi.HasValue)
      {
        T obj = this.values[hi.Value];
        for (int valueIndex = 0; valueIndex <= hi.Value; ++valueIndex)
          NoteMappingBase<T>.NoteMapperBase.Set(destination, valueIndex, hi.Value, obj);
      }
      else if (!hi.HasValue && lo.HasValue)
      {
        T obj = this.values[lo.Value];
        for (int valueIndex = lo.Value; valueIndex < length; ++valueIndex)
          NoteMappingBase<T>.NoteMapperBase.Set(destination, valueIndex, lo.Value, obj);
      }
      else
      {
        if (!hi.HasValue || !lo.HasValue)
          return;
        for (int valueIndex = lo.Value; valueIndex <= hi.Value; ++valueIndex)
        {
          int num = valueIndex - lo.Value;
          int sampleIndex = hi.Value - valueIndex >= num ? lo.Value : hi.Value;
          NoteMappingBase<T>.NoteMapperBase.Set(destination, valueIndex, sampleIndex, this.values[sampleIndex]);
        }
      }
    }
  }

  public override void Dispose() => Array.Clear((Array) this.values);
}
