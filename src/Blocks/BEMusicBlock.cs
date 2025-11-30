using VSInstrumentsBase.src.GUI;
using VSInstrumentsBase.src.Items;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using VSInstrumentsBase.src.Network.Packets;
using VSInstrumentsBase.src.Network.Playback;
using VSInstrumentsBase.src.Types;

#nullable disable
namespace VSInstrumentsBase.src.Blocks;

internal class BEMusicBlock : BlockEntityContainer
{
  private string blockName = "Music Block";
  private string bandName = "";
  private string songPath = "";
  private string songName = "No MIDI selected!";
  internal MusicBlockInventory inventory;
  private MusicBlockGUI musicBlockGUI;
  private string instrumentType = "";
  public bool isPlaying = false;

  public BEMusicBlock()
  {
    this.inventory = new MusicBlockInventory((string) null, (ICoreAPI) null);
    this.inventory.SlotModified += new Action<int>(this.OnSlotModified);
  }

  public override InventoryBase Inventory => (InventoryBase) this.inventory;

  public override string InventoryClassName => "musicblock";

  public virtual string DialogTitle => Lang.Get("Music Block", Array.Empty<object>());

  public override void Initialize(ICoreAPI api)
  {
    base.Initialize(api);
    this.OnSlotModified(0);
  }

  public override void ToTreeAttributes(ITreeAttribute tree)
  {
    base.ToTreeAttributes(tree);
    tree.SetString("name", this.blockName);
    tree.SetString("band", this.bandName);
    tree.SetString("file", this.songPath);
    tree.SetString("songname", this.songName);
  }

  public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
  {
    base.FromTreeAttributes(tree, worldAccessForResolve);
    this.blockName = tree.GetString("name", (string) null);
    this.bandName = tree.GetString("band", (string) null);
    this.songPath = tree.GetString("file", (string) null);
    this.songName = tree.GetString("songname", (string) null);
  }

  public override void OnBlockPlaced(ItemStack byItemStack = null)
  {
    base.OnBlockPlaced(byItemStack);
  }

  public override void OnBlockRemoved()
  {
    ((BlockEntity) this).OnBlockRemoved();
    if (((BlockEntity) this).Api.Side != EnumAppSide.Server || !this.isPlaying)
      return;
    this.StopPlayback();
  }

  public void OnUse(IPlayer byPlayer)
  {
    if (!byPlayer.WorldData.EntityControls.Sneak)
    {
      if (!this.isPlaying)
        this.StartPlayback(byPlayer);
      else
        this.StopPlayback();
      this.isPlaying = !this.isPlaying;
    }
    else
    {
      byte[] array;
      using (MemoryStream memoryStream = new MemoryStream())
      {
        BinaryWriter binaryWriter = new BinaryWriter((Stream) memoryStream);
        binaryWriter.Write(byPlayer.PlayerName);
        binaryWriter.Write(this.bandName);
        binaryWriter.Write(this.songPath);
        TreeAttribute treeAttribute = new TreeAttribute();
        ((InventoryBase) this.inventory).ToTreeAttributes((ITreeAttribute) treeAttribute);
        treeAttribute.ToBytes(binaryWriter);
        array = memoryStream.ToArray();
      }
      BlockPos blockPos = new BlockPos(((BlockEntity) this).Pos.X, ((BlockEntity) this).Pos.Y, ((BlockEntity) this).Pos.Z);
      ((ICoreServerAPI) ((BlockEntity) this).Api).Network.SendBlockEntityPacket((IServerPlayer) byPlayer, blockPos, 69, array);
      byPlayer.InventoryManager.OpenInventory((IInventory) this.inventory);
    }
  }

  private void StartPlayback(IPlayer byPlayer)
  {
    if (!(this.blockName != "") || !(this.songName != "") || !(this.instrumentType != "none") || !(this.instrumentType != ""))
      return;
    int instrumentId = this.GetInstrumentId(this.instrumentType);
    if (instrumentId == -1)
    {
      ((ICoreAPI) (((BlockEntity) this).Api as ICoreServerAPI)).Logger.Error("[MusicBlock] Invalid instrument type: " + this.instrumentType);
    }
    else
    {
      ((ICoreAPI) (((BlockEntity) this).Api as ICoreServerAPI)).Logger.Notification($"[MusicBlock] Sending play request to {byPlayer.PlayerName}: {this.songName}");
      MusicBlockPlayRequest blockPlayRequest = new MusicBlockPlayRequest()
      {
        SongPath = this.songPath,
        Channel = 0,
        InstrumentId = instrumentId
      };
      (((BlockEntity) this).Api as ICoreServerAPI).Network.GetChannel("instrumentsMusicBlock").SendPacket<MusicBlockPlayRequest>(blockPlayRequest, new IServerPlayer[1]
      {
        byPlayer as IServerPlayer
      });
    }
  }

  private void StopPlayback()
  {
    StopPlaybackRequest stopPlaybackRequest = new StopPlaybackRequest();
    (((BlockEntity) this).Api as ICoreServerAPI).Network.GetChannel("PlaybackChannel").BroadcastPacket<StopPlaybackRequest>(stopPlaybackRequest, Array.Empty<IServerPlayer>());
  }

  private int GetInstrumentId(string instrumentType)
  {
    ItemStack itemstack = ((InventoryBase) this.inventory)[0].Itemstack;
    return itemstack != null && itemstack.Item is InstrumentItem instrumentItem ? instrumentItem.InstrumentTypeID : -1;
  }

  public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
  {
    if (packetid <= 1000)
      this.inventory.InvNetworkUtil.HandleClientPacket(fromPlayer, packetid, data);
    if (packetid == 1004)
    {
      if (data != null)
      {
        using (MemoryStream memoryStream = new MemoryStream(data))
        {
          this.blockName = new BinaryReader((Stream) memoryStream).ReadString();
          if (this.blockName == null)
            this.blockName = "";
        }
        ((BlockEntity) this).MarkDirty(false, (IPlayer) null);
      }
      if (fromPlayer.InventoryManager != null)
        fromPlayer.InventoryManager.CloseInventory((IInventory) this.Inventory);
    }
    if (packetid == 1005)
    {
      if (data != null)
      {
        using (MemoryStream memoryStream = new MemoryStream(data))
        {
          this.bandName = new BinaryReader((Stream) memoryStream).ReadString();
          if (this.bandName == null)
            this.bandName = "";
        }
        ((BlockEntity) this).MarkDirty(false, (IPlayer) null);
      }
      if (fromPlayer.InventoryManager != null)
        fromPlayer.InventoryManager.CloseInventory((IInventory) this.Inventory);
    }
    if (packetid != 1006)
      return;
    if (data != null)
    {
      using (MemoryStream memoryStream = new MemoryStream(data))
      {
        BinaryReader binaryReader = new BinaryReader((Stream) memoryStream);
        this.songName = binaryReader.ReadString();
        this.songPath = binaryReader.ReadString();
        if (this.songPath == null)
          this.songPath = "";
      }
      ((BlockEntity) this).MarkDirty(false, (IPlayer) null);
    }
    if (fromPlayer.InventoryManager != null)
      fromPlayer.InventoryManager.CloseInventory((IInventory) this.Inventory);
  }

  public override void OnReceivedServerPacket(int packetid, byte[] data)
  {
    ((BlockEntity) this).OnReceivedServerPacket(packetid, data);
    if (packetid != 69)
      return;
    using (MemoryStream memoryStream = new MemoryStream(data))
    {
      BinaryReader binaryReader = new BinaryReader((Stream) memoryStream);
      binaryReader.ReadString();
      this.bandName = binaryReader.ReadString();
      this.songPath = binaryReader.ReadString();
      TreeAttribute treeAttribute = new TreeAttribute();
      treeAttribute.FromBytes(binaryReader);
      this.Inventory.FromTreeAttributes((ITreeAttribute) treeAttribute);
      this.Inventory.ResolveBlocksOrItems();
      if (this.musicBlockGUI == null)
      {
        InstrumentType instrumentType = null;
        ItemStack itemstack = ((InventoryBase) this.inventory)[0].Itemstack;
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
          instrumentType = instrumentItem.InstrumentType;
        this.musicBlockGUI = new MusicBlockGUI(this.DialogTitle, this.Inventory, ((BlockEntity) this).Pos, ((BlockEntity) this).Api as ICoreClientAPI, this.blockName, this.bandName, this.songName, instrumentType);
        ((GuiDialog) this.musicBlockGUI).OnClosed += (Action) (() => this.musicBlockGUI = (MusicBlockGUI) null);
      }
      ((GuiDialog) this.musicBlockGUI).TryOpen();
    }
  }

  private void OnSlotModified(int slotid)
  {
    ItemStack itemstack = ((InventoryBase) this.inventory)[slotid].Itemstack;
    if (itemstack != null)
    {
      if (!(itemstack.Item is InstrumentItem instrumentItem))
        return;
      this.instrumentType = instrumentItem.InstrumentType?.Name ?? "unknown";
    }
    else
      this.instrumentType = "none";
  }
}
