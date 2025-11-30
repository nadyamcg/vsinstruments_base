// Decompiled with JetBrains decompiler
// Type: Instruments.Blocks.MusicBlock
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using Vintagestory.API.Common;

#nullable disable
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
