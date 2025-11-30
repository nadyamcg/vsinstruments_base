using Melanchall.DryWetMidi.Common;


namespace VSInstrumentsBase.src.Midi;

public static class InstrumentExtensions
{
  public static string Name(this Instrument instrument) => instrument.ToString();

    public static SevenBitNumber ToSevenBitNumber(this Instrument instrument)
  {
    return (SevenBitNumber)(byte)instrument;
  }
}
