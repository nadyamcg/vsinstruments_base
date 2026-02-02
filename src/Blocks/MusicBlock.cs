using Vintagestory.API.Common;


namespace VSInstrumentsBase.src.Blocks;

internal class MusicBlock : Block
{
  public override bool OnBlockInteractStart(
    IWorldAccessor world,
    IPlayer byPlayer,
    BlockSelection blockSel)
  {
    if (world.Api.Side == EnumAppSide.Server && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMusicBlock blockEntity)
      blockEntity.OnUse(byPlayer);
    return true;
  }
}
