using System;
using Vintagestory.API.Common;


namespace VSInstrumentsBase.src.Types;

public static class InstrumentTypeExtensions
{
  public static void RegisterInstrumentItem(
    this ICoreAPI api,
    Type itemType,
    InstrumentType instrumentType)
  {
    ((ICoreAPICommon) api).RegisterItemClass(instrumentType.Name, itemType);
    InstrumentType.Register(api, itemType, instrumentType);
  }
}
