// Decompiled with JetBrains decompiler
// Type: Midi.InstrumentExtensions
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using Melanchall.DryWetMidi.Common;

#nullable disable
namespace VSInstrumentsBase.src.Midi;

public static class InstrumentExtensions
{
  public static string Name(this Instrument instrument) => instrument.ToString();

    public static SevenBitNumber ToSevenBitNumber(this Instrument instrument)
  {
    return (SevenBitNumber)(byte)instrument;
  }
}
