// Decompiled with JetBrains decompiler
// Type: Instruments.Types.InstrumentType
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src.Mapping;
using VSInstrumentsBase.src.Midi;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

#nullable disable
namespace VSInstrumentsBase.src.Types;

public abstract class InstrumentType(string name, string animation)
{
  private ICoreAPI _api;
  private int _id;
  private NoteMapping<string> _noteMap;
  private SkillItem[] _toolModes;
  private static readonly Dictionary<int, InstrumentType> _instrumentTypes = new Dictionary<int, InstrumentType>();
  private static readonly Queue<InstrumentType> _initializationQueue = new Queue<InstrumentType>();

  public static void Register(ICoreAPI api, Type instanceType, InstrumentType instrumentType)
  {
    int id = InstrumentType.ComputeID(instanceType);
    if (!InstrumentType._instrumentTypes.TryAdd(id, instrumentType))
      return;
    instrumentType._api = api;
    instrumentType._id = id;
    InstrumentType._initializationQueue.Enqueue(instrumentType);
  }

  public static void InitializeTypes()
  {
    while (InstrumentType._initializationQueue.Count > 0)
      InstrumentType._initializationQueue.Dequeue().Initialize();
  }

  public static void UnregisterAll()
  {
    foreach (InstrumentType instrumentType in new InstrumentType[InstrumentType._instrumentTypes.Count])
    {
      if (instrumentType != null && InstrumentType._instrumentTypes.Remove(instrumentType._id))
        instrumentType.Cleanup();
    }
    InstrumentType._instrumentTypes.Clear();
  }

  protected virtual void Initialize()
  {
    this._toolModes = new SkillItem[4];
    this._toolModes[3] = new SkillItem()
    {
      Code = new AssetLocation(PlayMode.midi.ToString()),
      Name = Lang.Get("MIDI Mode", Array.Empty<object>())
    };
    this._toolModes[2] = new SkillItem()
    {
      Code = new AssetLocation(PlayMode.fluid.ToString()),
      Name = Lang.Get("Fluid Play", Array.Empty<object>())
    };
    this._toolModes[1] = new SkillItem()
    {
      Code = new AssetLocation(PlayMode.lockedSemiTone.ToString()),
      Name = Lang.Get("Locked Play: Semi Tone", Array.Empty<object>())
    };
    this._toolModes[0] = new SkillItem()
    {
      Code = new AssetLocation(PlayMode.lockedTone.ToString()),
      Name = Lang.Get("Locked Play: Tone", Array.Empty<object>())
    };
    if (this.Api is ICoreClientAPI api)
    {
      this._toolModes[3].WithIcon(api, api.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/3.svg"), 48 /*0x30*/, 48 /*0x30*/, 5, new int?(-1)));
      this._toolModes[3].TexturePremultipliedAlpha = false;
      this._toolModes[2].WithIcon(api, api.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/3.svg"), 48 /*0x30*/, 48 /*0x30*/, 5, new int?(-1)));
      this._toolModes[2].TexturePremultipliedAlpha = false;
      this._toolModes[1].WithIcon(api, api.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/2.svg"), 48 /*0x30*/, 48 /*0x30*/, 5, new int?(-1)));
      this._toolModes[1].TexturePremultipliedAlpha = false;
      this._toolModes[0].WithIcon(api, api.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/1.svg"), 48 /*0x30*/, 48 /*0x30*/, 5, new int?(-1)));
      this._toolModes[0].TexturePremultipliedAlpha = false;
    }
    this._noteMap = (NoteMapping<string>) new NoteMappingLegacy("sounds/" + this.Name);
  }

  internal static void UnregisterType(Type instanceType)
  {
    int id = InstrumentType.ComputeID(instanceType);
    if (!InstrumentType._instrumentTypes.Remove(id, out InstrumentType instrumentType))
      return;
    instrumentType.Cleanup();
  }

  protected virtual void Cleanup()
  {
    foreach (SkillItem toolMode in this._toolModes)
      toolMode.Dispose();
    Array.Clear((Array) this._toolModes);
    this._toolModes = (SkillItem[]) null;
  }

  public ICoreAPI Api => this._api;

  public int ID => this._id;

  public string Name => name;

  public string Animation => animation;

  public NoteMapping<string> NoteMap => this._noteMap;

  public SkillItem[] ToolModes => this._toolModes;

  public virtual bool GetPitchSound(Pitch pitch, out string assetPath, out float modPitch)
  {
    assetPath = this.NoteMap.GetValue(pitch);
    if (string.IsNullOrEmpty(assetPath))
    {
      modPitch = 1f;
      return false;
    }
    modPitch = this.NoteMap.GetRelativePitch(pitch);
    return true;
  }

  internal static InstrumentType Find(int id)
  {
    InstrumentType instrumentType;
    return InstrumentType._instrumentTypes.TryGetValue(id, out instrumentType) ? instrumentType : (InstrumentType) null;
  }

  internal static InstrumentType Find(Type instanceType)
  {
    return InstrumentType.Find(InstrumentType.ComputeID(instanceType));
  }

  internal static InstrumentType Find(string name)
  {
    if (string.IsNullOrEmpty(name))
      return (InstrumentType) null;
    foreach (KeyValuePair<int, InstrumentType> instrumentType1 in InstrumentType._instrumentTypes)
    {
      InstrumentType instrumentType2 = instrumentType1.Value;
      if (string.Compare(name, instrumentType2.Name, StringComparison.OrdinalIgnoreCase) == 0)
        return instrumentType2;
    }
    return (InstrumentType) null;
  }

  private static int ComputeID(Type type) => type.FullName.GetHashCode();

  internal static void Find(
    string name,
    List<InstrumentType> destination,
    StringComparison comparison = StringComparison.OrdinalIgnoreCase)
  {
    foreach (KeyValuePair<int, InstrumentType> instrumentType1 in InstrumentType._instrumentTypes)
    {
      InstrumentType instrumentType2 = instrumentType1.Value;
      if (string.Compare(name, instrumentType2.Name, comparison) == 0)
        destination.Add(instrumentType2);
    }
  }
}
