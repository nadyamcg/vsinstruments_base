using VSInstrumentsBase.src.Midi;
using System;


namespace VSInstrumentsBase.src.Mapping;

public static class NoteMappingUtility
{
  public static float ComputeRelativePitch(Pitch target, Pitch source)
  {
    return (float) Math.Pow(2.0, (double) (target - source) / 12.0);
  }

  public static float RelativePitch(this Pitch source, Pitch target)
  {
    return NoteMappingUtility.ComputeRelativePitch(target, source);
  }
}
