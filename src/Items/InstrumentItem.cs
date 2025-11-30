// Decompiled with JetBrains decompiler
// Type: Instruments.Items.InstrumentItem
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src.GUI;
using VSInstrumentsBase.src.Types;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using vsinstruments_base.src;

#nullable disable
namespace VSInstrumentsBase.src.Items;

public class InstrumentItem : Item
{
  private float currentPitch;
  private ICoreClientAPI capi;
  private bool holding = false;
  private InstrumentType _instrumentType;
  private SkillItem[] toolModes;

  public virtual void OnLoaded(ICoreAPI api)
  {
    if (api.Side != EnumAppSide.Client)
      return;
    this.Startup();
    this.toolModes = ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "instrumentToolModes", () => new SkillItem[0]);
  }

  public virtual void OnUnloaded(ICoreAPI api)
  {
    for (int index = 0; this.toolModes != null && index < this.toolModes.Length; ++index)
      this.toolModes[index]?.Dispose();
  }

  public virtual SkillItem[] GetToolModes(
    ItemSlot slot,
    IClientPlayer forPlayer,
    BlockSelection blocksel)
  {
    return (SkillItem[]) null;
  }

  public virtual void SetToolMode(
    ItemSlot slot,
    IPlayer byPlayer,
    BlockSelection blockSel,
    int toolMode)
  {
  }

  public virtual int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => 0;

  public virtual void OnHeldInteractStart(
    ItemSlot slot,
    EntityAgent byEntity,
    BlockSelection blockSel,
    EntitySelection entitySel,
    bool firstEvent,
    ref EnumHandHandling handling)
  {
    if (!firstEvent || this.api.Side != EnumAppSide.Client || !(byEntity is EntityPlayer))
      return;
    if (byEntity.Controls.Sneak)
    {
      base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
    }
    else
    {
      handling = EnumHandHandling.PreventDefault;
      if (this.api is ICoreClientAPI api)
      {
        api.Logger.Notification("[InstrumentItem] Opening SongSelectGUI for " + (this.InstrumentType?.Name ?? "Unknown Instrument"));
        new SongSelectGUI(api, this.InstrumentType, title: "Select MIDI File - " + (this.InstrumentType?.Name ?? "Instrument")).TryOpen();
      }
    }
  }

  private void Startup() => this.capi = this.api as ICoreClientAPI;

  private IClientWorldAccessor GetClient(EntityAgent entity, out bool isClient)
  {
    isClient = ((Entity) entity).World.Side == EnumAppSide.Client;
    return isClient ? ((Entity) entity).World as IClientWorldAccessor : (IClientWorldAccessor) null;
  }

  private void ChangeFromInstrument(ActiveSlotChangeEventArgs args)
  {
    this.capi.Event.AfterActiveSlotChanged -= new Action<ActiveSlotChangeEventArgs>(this.ChangeFromInstrument);
    this.holding = false;
    if (!Definitions.Instance.IsPlaying())
      return;
    Definitions.Instance.SetIsPlaying(false);
  }

  private void SetPlayMode(ItemSlot slot, PlayMode playMode)
  {
    slot.Itemstack.Attributes.SetInt("toolMode", (int) playMode);
  }

  private static PlayMode GetPlayMode(ItemSlot slot)
  {
    return (PlayMode) slot.Itemstack.Attributes.GetInt("toolMode", 3);
  }

  public InstrumentType InstrumentType
  {
    get
    {
      if (this._instrumentType != null)
        return this._instrumentType;
      string path = ((RegistryObject) this).Code?.Path;
      if (this.api != null)
        this.api.Logger.Debug("[InstrumentItem] Looking for instrument type: " + path);
      if (!string.IsNullOrEmpty(path))
      {
        this._instrumentType = InstrumentType.Find(path);
        if (this._instrumentType != null && this.api != null)
          this.api.Logger.Notification($"[InstrumentItem] Found instrument type: {this._instrumentType.Name} (ID: {this._instrumentType.ID})");
      }
      if (this._instrumentType == null)
      {
        this._instrumentType = InstrumentType.Find("grandpiano");
        if (this.api != null)
          this.api.Logger.Warning($"[InstrumentItem] Could not find instrument type for '{path}', defaulting to grandpiano");
      }
      return this._instrumentType;
    }
  }

  public int InstrumentTypeID
  {
    get
    {
      InstrumentType instrumentType = this.InstrumentType;
      return instrumentType != null ? instrumentType.ID : -1;
    }
  }
}
