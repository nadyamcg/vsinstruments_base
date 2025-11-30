// Decompiled with JetBrains decompiler
// Type: Instruments.GUI.MusicBlockGUI
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src.Items;
using VSInstrumentsBase.src.Types;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using VSInstrumentsBase.src.GUI;


#nullable disable
namespace VSInstrumentsBase.src.GUI;

public class MusicBlockGUI : GuiDialogBlockEntity
{
  private InstrumentType _instrumentType;

  public MusicBlockGUI(
    string title,
    InventoryBase inventory,
    BlockPos bePos,
    ICoreClientAPI capi,
    string blockName,
    string bandName,
    string songName,
    InstrumentType instrumentType = null)
    : base(title, inventory, bePos, capi)
  {
        _instrumentType = instrumentType;
    if (IsDuplicate)
      return;
     capi.World.Player.InventoryManager.OpenInventory(Inventory);
        SetupDialog(blockName, bandName, songName);
  }

  private void SetupDialog(string name, string bandName, string songName)
  {
    ItemSlot itemSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
    if (itemSlot != null && itemSlot.Inventory == Inventory)
            capi.Input.TriggerOnMouseLeaveSlot(itemSlot);
    else
      itemSlot =  null;
    ElementBounds elementBounds1 = ElementBounds.Fixed(0.0, 0.0, 300.0, 150.0);
    ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 30.0, 300.0, 30.0);
    ElementBounds elementBounds3 = ElementBounds.Fixed(0.0, 60.0, 300.0, 30.0);
    ElementBounds elementBounds4 = ElementBounds.Fixed(0.0, 100.0, 300.0, 30.0);
    ElementBounds elementBounds5 = ElementBounds.Fixed(0.0, 130.0, 300.0, 30.0);
    ElementBounds elementBounds6 = ElementBounds.Fixed(0.0, 180.0, 300.0, 30.0);
    ElementBounds elementBounds7 = ElementStdBounds.SlotGrid( 0, 10.0, 210.0, 4, 1);
    ElementBounds elementBounds8 = ElementBounds.Fixed(100.0, 180.0, 200.0, 90.0);
    ElementBounds elementBounds9 = ElementStdBounds.SlotGrid( 0, 0.0, 210.0, 1, 1);
    ElementBounds elementBounds10 = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(elementBounds9, 10.0).WithAlignment((EnumDialogArea) 8).WithFixedPadding(10.0, 2.0);
    ElementBounds elementBounds11 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
    elementBounds11.BothSizing = (ElementSizing) 2;
    elementBounds11.WithChildren(new ElementBounds[1]
    {
      elementBounds1
    });
    ElementBounds elementBounds12 = ElementStdBounds.AutosizedMainDialog.WithAlignment((EnumDialogArea) 10).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
        ClearComposers();
        // ISSUE: method pointer
        SingleComposer = capi.Gui.CreateCompo("blockentitymusicblock" + BlockEntityPosition?.ToString(), elementBounds12).AddShadedDialogBG(elementBounds11, true, 5.0, 0.75f).AddDialogTitleBar(DialogTitle, new Action(OnTitleBarClose),  null,  null,  null).BeginChildElements(elementBounds11).AddDynamicText(Lang.Get($"Name: \"{name}\"", Array.Empty<object>()), CairoFont.WhiteSmallText(), elementBounds2, nameof (name)).AddTextInput(elementBounds3, new Action<string>(OnNameChange),  null,  null).AddDynamicText(Lang.Get($"Band Name: \"{bandName}\"", Array.Empty<object>()), CairoFont.WhiteSmallText(), elementBounds4, nameof (bandName)).AddTextInput(elementBounds5, new Action<string>(OnBandNameChange),  null,  null).AddItemSlotGrid(Inventory, new Action<object>(SendInvPacket), 1, new int[1], elementBounds7,  null).AddStaticText(Lang.Get("Instrument", Array.Empty<object>()), CairoFont.WhiteSmallText(), elementBounds6,  null).AddDynamicText(Lang.Get($"Song File: \n\"{songName}\"", Array.Empty<object>()), CairoFont.WhiteSmallText(), elementBounds8, nameof (songName)).AddSmallButton(Lang.Get("Song Select", Array.Empty<object>()), () => OnSongSelect(), elementBounds10, (EnumButtonStyle) 2, "songSelectButton").EndChildElements().Compose(true);
    if (itemSlot == null)
      return;
        SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
  }

  private void OnNameChange(string newName)
  {
    string str = !(newName != "") ? "Please give me a name!" : $"Name: \"{newName}\"";
        SingleComposer.GetDynamicText("name").SetNewText(str, false, false, false);
    if (!(newName != ""))
      return;
    byte[] array;
    using (MemoryStream memoryStream = new MemoryStream())
    {
      new BinaryWriter( memoryStream).Write(newName);
      array = memoryStream.ToArray();
    }
        capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1004, array);
  }

  private void OnBandNameChange(string newBand)
  {
    string str = !(newBand != "") ? "No Band" : $"Band Name: \"{newBand}\"";
        SingleComposer.GetDynamicText("bandName").SetNewText(str, false, false, false);
    byte[] array;
    using (MemoryStream memoryStream = new MemoryStream())
    {
      new BinaryWriter( memoryStream).Write(newBand);
      array = memoryStream.ToArray();
    }
        capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1005, array);
  }

  private bool OnSongSelect()
  {
    if (_instrumentType == null)
    {
      ItemStack itemstack = Inventory[0].Itemstack;
      InstrumentItem instrumentItem = null;
      int num;
      if (itemstack != null)
      {
        instrumentItem = itemstack.Item as InstrumentItem;
        num = instrumentItem != null ? 1 : 0;
      }
      else
        num = 0;
      if (num != 0)
                _instrumentType = instrumentItem.InstrumentType;
    }
    new SongSelectGUI(capi, _instrumentType, title: "Select MIDI File for Music Block", onFileSelect:  (songPath, songName) => SetSong(songPath, songName)).TryOpen();
    return true;
  }

  private void SetSong(string songPath, string songName)
  {
        SingleComposer.GetDynamicText(nameof (songName)).SetNewText($"Song File: \n\"{songName}\"", false, false, false);
    byte[] array;
    using (MemoryStream memoryStream = new MemoryStream())
    {
      BinaryWriter binaryWriter = new BinaryWriter( memoryStream);
      binaryWriter.Write(songName);
      binaryWriter.Write(songPath);
      array = memoryStream.ToArray();
    }
        capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1006, array);
  }

  private void SendInvPacket(object p)
  {
        capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
  }

  private void OnTitleBarClose() => TryClose();

  private void OnInventorySlotModified(int slotid)
  {
    if (slotid != 0)
      ;
  }

  public override void OnGuiOpened()
  {
    base.OnGuiOpened();
        Inventory.SlotModified += new Action<int>(OnInventorySlotModified);
  }

  public override bool OnEscapePressed()
  {
    base.OnEscapePressed();
        OnTitleBarClose();
    return TryClose();
  }
}
