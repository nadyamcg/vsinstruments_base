using VSInstrumentsBase.src.GUI;
using VSInstrumentsBase.src.Types;
using VSInstrumentsBase.src.Core;
using VSInstrumentsBase.src.Playback;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using vsinstruments_base.src;


namespace VSInstrumentsBase.src.Items;

public class InstrumentItem : Item
{
  private ICoreClientAPI capi;
  private InstrumentType _instrumentType;
  private SkillItem[] toolModes;

  public override void OnLoaded(ICoreAPI api)
  {
    if (api.Side != EnumAppSide.Client)
      return;
    this.Startup();
    this.toolModes = ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "instrumentToolModes", () => []);
  }

  public override void OnUnloaded(ICoreAPI api)
  {
    for (int index = 0; this.toolModes != null && index < this.toolModes.Length; ++index)
      this.toolModes[index]?.Dispose();
  }

  public override SkillItem[] GetToolModes(
    ItemSlot slot,
    IClientPlayer forPlayer,
    BlockSelection blocksel)
  {
    return (SkillItem[]) null;
  }

  public override void SetToolMode(
    ItemSlot slot,
    IPlayer byPlayer,
    BlockSelection blockSel,
    int toolMode)
  {
  }

  public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => 0;

  public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
  {
    if (hand == EnumHand.Right && forEntity.Attributes.GetBool("isPlayingInstrument"))
      return InstrumentType?.Animation;
    return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);
  }

  public override void OnHeldInteractStart(
    ItemSlot slot,
    EntityAgent byEntity,
    BlockSelection blockSel,
    EntitySelection entitySel,
    bool firstEvent,
    ref EnumHandHandling handling)
  {
    if (!firstEvent || this.api.Side != EnumAppSide.Client || byEntity is not EntityPlayer)
      return;
    if (byEntity.Controls.Sneak)
    {
      base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
    }
    else
    {
      handling = EnumHandHandling.PreventDefault;
      if (this.api is ICoreClientAPI capi)
      {
        if (byEntity.Attributes.GetBool("isPlayingInstrument"))
        {
          var pm = capi.ModLoader.GetModSystem<InstrumentModClient>()?.PlaybackManager as PlaybackManagerClient;
          pm?.RequestStopPlayback();
          return;
        }

        try
        {
          var gui = new SongSelectGUI(capi, this.InstrumentType, title: "Select MIDI File - " + (this.InstrumentType?.Name ?? "Instrument"));
          gui.TryOpen();
        }
        catch (Exception ex)
        {
          capi.Logger.Error("[InstrumentItem] Exception while opening GUI: " + ex.Message);
        }
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
      this.api?.Logger.Debug("[InstrumentItem] Looking for instrument type: " + path);
      if (!string.IsNullOrEmpty(path))
      {
        this._instrumentType = InstrumentType.Find(path);
        if (this._instrumentType != null && this.api != null)
          this.api.Logger.Notification($"[InstrumentItem] Found instrument type: {this._instrumentType.Name} (ID: {this._instrumentType.ID})");
      }
      if (this._instrumentType == null)
      {
        this._instrumentType = InstrumentType.Find("grandpiano");
        this.api?.Logger.Warning($"[InstrumentItem] Could not find instrument type for '{path}', defaulting to grandpiano");
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
