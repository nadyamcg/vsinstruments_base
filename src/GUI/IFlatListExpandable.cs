// Decompiled with JetBrains decompiler
// Type: Instruments.GUI.IFlatListExpandable
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

#nullable disable
using vsinstruments_base;

namespace VSInstrumentsBase.src.GUI;

public interface IFlatListExpandable
{
  bool IsExpanded { get; }

  int Depth { get; }
}
