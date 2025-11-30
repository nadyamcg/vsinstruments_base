// Decompiled with JetBrains decompiler
// Type: Instruments.Mapping.NoteMapping`1
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src.Midi;

#nullable disable
namespace VSInstrumentsBase.src.Mapping;

public class NoteMapping<T> : NoteMappingBase<T>
{
  public float GetRelativePitch(Pitch target)
  {
    Pitch source = this.GetItem(target).Source;
    return NoteMappingUtility.ComputeRelativePitch(target, source);
  }
}
