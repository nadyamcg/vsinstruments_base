using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

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
