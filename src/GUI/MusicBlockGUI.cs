using System.IO;                // Binary writer n that
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;  // Lang stuff
using Vintagestory.API.MathTools;

namespace Instruments.GUI
{
    public class MusicBlockGUI : GuiDialogBlockEntity
    {
        public MusicBlockGUI(string title, InventoryBase inventory, BlockPos bePos, ICoreClientAPI capi, string blockName, string bandName, string songName) : base(title, inventory, bePos, capi)
        {
            if (IsDuplicate)
                return;
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            SetupDialog(blockName, bandName, songName);
        }
        private void SetupDialog(string name, string bandName, string songName)
        {
            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
            {
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            }
            else
            {
                hoveredSlot = null;
            }

            ElementBounds mainBounds = ElementBounds.Fixed(0, 0, 300, 150);

            ElementBounds nameBounds = ElementBounds.Fixed(0, 30, 300, 30);

            ElementBounds nameInputBounds = ElementBounds.Fixed(0, 60, 300, 30);

            ElementBounds bandnameBounds = ElementBounds.Fixed(0, 100, 300, 30);

            ElementBounds bandnameInputBounds = ElementBounds.Fixed(0, 130, 300, 30);

            ElementBounds instrumentTextBounds = ElementBounds.Fixed(0, 180, 300, 30);

            ElementBounds instrumentSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 10, 210, 4, 1);

            ElementBounds songNameBounds = ElementBounds.Fixed(100, 180, 200, 90);

            ElementBounds mediaSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 210, 1, 1);

            ElementBounds sendButtonBounds = ElementBounds.FixedSize(0, 0).FixedUnder(mediaSlotBounds, 2 * 5).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPadding(10, 2);

            // 2. Around all that is 10 pixel padding
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(mainBounds);

            // 3. Finally Dialog
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);


            ClearComposers();
            SingleComposer = capi.Gui
                .CreateCompo("blockentitymusicblock" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddDynamicText(Lang.Get("Name: \"" + name + "\""), CairoFont.WhiteSmallText(), nameBounds, "name")
                    .AddTextInput(nameInputBounds, OnNameChange)
                    .AddDynamicText(Lang.Get("Band Name: \"" + bandName + "\""), CairoFont.WhiteSmallText(), bandnameBounds, "bandName")
                    .AddTextInput(bandnameInputBounds, OnBandNameChange)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, instrumentSlotBounds)
                    .AddStaticText(Lang.Get("Instrument"), CairoFont.WhiteSmallText(), instrumentTextBounds)
                    .AddDynamicText(Lang.Get("Song File: \n\"" + songName + "\""), CairoFont.WhiteSmallText(), songNameBounds, "songName")
                .AddSmallButton(Lang.Get("Song Select"), OnSongSelect, sendButtonBounds, EnumButtonStyle.Normal, "songSelectButton")
                .EndChildElements()
                .Compose()
            ;

            if (hoveredSlot != null)
            {
                SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
            }
        }
        private void OnNameChange(string newName)
        {
            string newText;
            if (newName != "")
                newText = "Name: \"" + newName + "\"";
            else
                newText = "Please give me a name!";
            SingleComposer.GetDynamicText("name").SetNewText(newText);

            if (newName != "")
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(newName);
                    data = ms.ToArray();
                }

                capi.Network.SendBlockEntityPacket(BlockEntityPosition, Constants.Packet.NameChangeID, data);
            }
        }
        private void OnBandNameChange(string newBand)
        {
            // Called when the band name needs to change. Update the SingleComposer's Dynamic text field.
            string newText;
            if (newBand != "")
                newText = "Band Name: \"" + newBand + "\"";
            else
                newText = "No Band";
            SingleComposer.GetDynamicText("bandName").SetNewText(newText);

            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(newBand);
                data = ms.ToArray();
            }

            capi.Network.SendBlockEntityPacket(BlockEntityPosition, Constants.Packet.BandChangeID, data);
        }
        private bool OnSongSelect()
        {
            //SongSelectGUI songGui = new SongSelectGUI(capi, SetSong, Definitions.Instance.GetSongList());
            //songGui.TryOpen();
            return true;
        }
        private int SetSong(string filePath)
        {
            // Read the selected file, and send the contents to the server
            string songData = "";
            // Try to read the file. If it failed, it's propably a server file, so we should send the filename when starting playback, just as with handheld +.
            RecursiveFileProcessor.ReadFile(Definitions.Instance.ABCBasePath() + Path.DirectorySeparatorChar + filePath, ref songData);

            SingleComposer.GetDynamicText("songName").SetNewText("Song File: \n\"" + filePath + "\"");

            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(filePath);
                writer.Write(songData);
                data = ms.ToArray();
            }

            capi.Network.SendBlockEntityPacket(BlockEntityPosition, Constants.Packet.SongSelectID, data);
            return 1;
        }
        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }
        private void OnInventorySlotModified(int slotid)
        {
            if (slotid == 0)
            {
                //if (Inventory[0].Itemstack?.Collectible.GetType())// FirstCodePart().Equals("parcel") == true)
                ;   // Allow playback? Only allow it if the item is in the slot (and disallow other items)
            }
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            Inventory.SlotModified += OnInventorySlotModified;
        }

        public override bool OnEscapePressed()
        {
            base.OnEscapePressed();
            OnTitleBarClose();
            return TryClose();
        }
    }
}
