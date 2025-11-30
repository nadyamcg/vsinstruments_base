// Decompiled with JetBrains decompiler
// Type: Instruments.Core.InstrumentModExtensions
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using VSInstrumentsBase.src.Core;
using VSInstrumentsBase.src.Core;

#nullable disable
namespace VSInstrumentsBase.src.Core;

public static class InstrumentModExtensions
{
  private static T GetInstrumentMod<T>(this ICoreAPI coreAPI) where T : InstrumentModBase
  {
    return coreAPI.ModLoader.GetModSystem<T>(true);
  }

  public static InstrumentModBase GetInstrumentMod(this ICoreAPI coreAPI)
  {
    return coreAPI.GetInstrumentMod<InstrumentModBase>();
  }

  public static InstrumentModClient GetInstrumentMod(this ICoreClientAPI clientAPI)
  {
    return  clientAPI.GetInstrumentMod<InstrumentModClient>();
  }

  public static InstrumentModServer GetInstrumentMod(this ICoreServerAPI serverAPI)
  {
    return ((ICoreAPI) serverAPI).GetInstrumentMod<InstrumentModServer>();
  }
}
