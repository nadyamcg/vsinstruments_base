// Decompiled with JetBrains decompiler
// Type: Instruments.Mapping.NoteMappingUtility
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src.Midi;
using System;

#nullable disable
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
