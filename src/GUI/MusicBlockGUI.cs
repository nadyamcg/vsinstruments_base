using VSInstrumentsBase.src.Items;
using VSInstrumentsBase.src.Types;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using VSInstrumentsBase.src.GUI;



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
    try
    {
      SetupDialog(blockName, bandName, songName);
    }
    catch (Exception ex)
    {
      capi.Logger.Error("[MusicBlockGUI] failed to compose dialog: " + ex);
    }
  }

  private void SetupDialog(string name, string bandName, string songName)
  {
    ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
    if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
      capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
    else
      hoveredSlot = null;

    ElementBounds mainBounds        = ElementBounds.Fixed(0, 0, 300, 150);
    ElementBounds nameBounds        = ElementBounds.Fixed(0, 30, 300, 30);
    ElementBounds nameInputBounds   = ElementBounds.Fixed(0, 60, 300, 30);
    ElementBounds bandnameBounds    = ElementBounds.Fixed(0, 100, 300, 30);
    ElementBounds bandnameInputBounds = ElementBounds.Fixed(0, 130, 300, 30);
    ElementBounds instrumentTextBounds = ElementBounds.Fixed(0, 180, 300, 30);
    ElementBounds instrumentSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 10, 210, 4, 1);
    ElementBounds songNameBounds    = ElementBounds.Fixed(100, 180, 200, 90);
    ElementBounds buttonAnchorBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 210, 1, 1);
    ElementBounds sendButtonBounds  = ElementBounds.FixedSize(0, 0).FixedUnder(buttonAnchorBounds, 10).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPadding(10, 2);

    ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
    bgBounds.BothSizing = ElementSizing.FitToChildren;
    bgBounds.WithChildren(mainBounds);

    ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
      .WithAlignment(EnumDialogArea.RightMiddle)
      .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

    ClearComposers();
    SingleComposer = capi.Gui
      .CreateCompo("blockentitymusicblock" + BlockEntityPosition?.ToString(), dialogBounds)
      .AddShadedDialogBG(bgBounds)
      .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
      .BeginChildElements(bgBounds)
        .AddDynamicText(Lang.Get($"Name: \"{name}\""), CairoFont.WhiteSmallText(), nameBounds, "name")
        .AddTextInput(nameInputBounds, OnNameChange)
        .AddDynamicText(Lang.Get($"Band Name: \"{bandName}\""), CairoFont.WhiteSmallText(), bandnameBounds, "bandName")
        .AddTextInput(bandnameInputBounds, OnBandNameChange)
        .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, instrumentSlotBounds)
        .AddStaticText(Lang.Get("Instrument"), CairoFont.WhiteSmallText(), instrumentTextBounds)
        .AddDynamicText(Lang.Get($"Song File: \n\"{songName}\""), CairoFont.WhiteSmallText(), songNameBounds, "songName")
      .AddSmallButton(Lang.Get("Song Select"), OnSongSelect, sendButtonBounds, EnumButtonStyle.Normal, "songSelectButton")
      .EndChildElements()
      .Compose();

    if (hoveredSlot != null)
      SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
  }

  private void OnNameChange(string newName)
  {
    string str = !(newName != "") ? "Please give me a name!" : $"Name: \"{newName}\"";
        SingleComposer.GetDynamicText("name").SetNewText(str, false, false, false);
    if (!(newName != ""))
      return;
    byte[] array;
    using (MemoryStream memoryStream = new())
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
    using (MemoryStream memoryStream = new())
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
    new SongSelectGUI(capi, _instrumentType, title: "Select MIDI File for Music Block", onFileSelect:  (songPath, songName, trackIndex) => SetSong(songPath, songName, trackIndex)).TryOpen();
    return true;
  }

  private void SetSong(string songPath, string songName, int trackIndex)
  {
        SingleComposer.GetDynamicText(nameof (songName)).SetNewText($"Song File: \n\"{songName}\"", false, false, false);
    byte[] array;
    using (MemoryStream memoryStream = new())
    {
      BinaryWriter binaryWriter = new( memoryStream);
      binaryWriter.Write(songName);
      binaryWriter.Write(songPath);
      binaryWriter.Write(trackIndex);
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
    // Slot change handling can be implemented here if needed.
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
