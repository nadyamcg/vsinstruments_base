using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;


namespace VSInstrumentsBase.src.Blocks;

internal class MusicBlockInventory : InventoryBase, ISlotProvider
{
  private ItemSlot[] slots;

  public ItemSlot[] Slots => slots;

  public override int Count => slots.Length;

  public MusicBlockInventory(string inventoryID, ICoreAPI api)
    : base(inventoryID, api)
  {
        slots = GenEmptySlots(1);
  }

  public MusicBlockInventory(string className, string instanceID, ICoreAPI api)
    : base(className, instanceID, api)
  {
        slots = GenEmptySlots(1);
  }

  public override ItemSlot this[int slotId]
  {
    get => slotId < 0 || slotId >= Count ?  null : slots[slotId];
        set
    {
      if (slotId < 0 || slotId >= Count)
        throw new ArgumentOutOfRangeException(nameof (slotId));
            slots[slotId] = value != null ? value : throw new ArgumentNullException(nameof (value));
    }
  }

  public override void FromTreeAttributes(ITreeAttribute tree)
  {
        slots = SlotsFromTreeAttributes(tree,  null,  null);
  }

  public override void ToTreeAttributes(ITreeAttribute tree)
  {
        SlotsToTreeAttributes(slots, tree);
  }
}
