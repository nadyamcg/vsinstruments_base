// Decompiled with JetBrains decompiler
// Type: Instruments.Types.InstrumentTypeExtensions
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using System;
using Vintagestory.API.Common;

#nullable disable
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
