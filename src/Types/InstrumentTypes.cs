// Decompiled with JetBrains decompiler
// Type: Instruments.Types.InstrumentTypes
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using Vintagestory.API.Common;

#nullable disable
namespace VSInstrumentsBase.src.Types;

public static class InstrumentTypes
{
  public static void RegisterAll(ICoreAPI api)
  {
    InstrumentType.Register(api, typeof (InstrumentTypes.GrandPiano), (InstrumentType) new InstrumentTypes.GrandPiano());
    InstrumentType.Register(api, typeof (InstrumentTypes.AcousticGuitar), (InstrumentType) new InstrumentTypes.AcousticGuitar());
    InstrumentType.Register(api, typeof (InstrumentTypes.Violin), (InstrumentType) new InstrumentTypes.Violin());
    InstrumentType.Register(api, typeof (InstrumentTypes.Harp), (InstrumentType) new InstrumentTypes.Harp());
    InstrumentType.Register(api, typeof (InstrumentTypes.Dulcimer), (InstrumentType) new InstrumentTypes.Dulcimer());
    InstrumentType.Register(api, typeof (InstrumentTypes.Clarinet), (InstrumentType) new InstrumentTypes.Clarinet());
    InstrumentType.Register(api, typeof (InstrumentTypes.Trumpet), (InstrumentType) new InstrumentTypes.Trumpet());
    InstrumentType.Register(api, typeof (InstrumentTypes.Sax), (InstrumentType) new InstrumentTypes.Sax());
    InstrumentType.Register(api, typeof (InstrumentTypes.Drum), (InstrumentType) new InstrumentTypes.Drum());
    InstrumentType.Register(api, typeof (InstrumentTypes.SteelDrum), (InstrumentType) new InstrumentTypes.SteelDrum());
    InstrumentType.Register(api, typeof (InstrumentTypes.Accordion), (InstrumentType) new InstrumentTypes.Accordion());
    InstrumentType.Register(api, typeof (InstrumentTypes.MusicBox), (InstrumentType) new InstrumentTypes.MusicBox());
    InstrumentType.Register(api, typeof (InstrumentTypes.Mic), (InstrumentType) new InstrumentTypes.Mic());
    InstrumentType.Register(api, typeof (InstrumentTypes.MBoxComb), (InstrumentType) new InstrumentTypes.MBoxComb());
    InstrumentType.Register(api, typeof (InstrumentTypes.VBow), (InstrumentType) new InstrumentTypes.VBow());
    InstrumentType.Register(api, typeof (InstrumentTypes.MusicBlockCone), (InstrumentType) new InstrumentTypes.MusicBlockCone());
    api.Logger.Notification("[InstrumentTypes] Registered 16 instrument types by name");
    InstrumentType instrumentType = InstrumentType.Find("grandpiano");
    if (instrumentType != null)
      api.Logger.Notification($"[InstrumentTypes] Verification SUCCESS: Found 'grandpiano' -> {instrumentType.Name} (ID: {instrumentType.ID})");
    else
      api.Logger.Error("[InstrumentTypes] Verification FAILED: Find(\"grandpiano\") returned null!");
  }

  public class GrandPiano : InstrumentType
  {
    public GrandPiano()
      : base("grandpiano", "holdbothhandslarge")
    {
    }
  }

  public class AcousticGuitar : InstrumentType
  {
    public AcousticGuitar()
      : base("acousticguitar", "holdbothhandslarge")
    {
    }
  }

  public class Violin : InstrumentType
  {
    public Violin()
      : base("violin", "holdbothhandslarge")
    {
    }
  }

  public class Harp : InstrumentType
  {
    public Harp()
      : base("harp", "holdbothhandslarge")
    {
    }
  }

  public class Dulcimer : InstrumentType
  {
    public Dulcimer()
      : base("dulcimer", "holdbothhandslarge")
    {
    }
  }

  public class Clarinet : InstrumentType
  {
    public Clarinet()
      : base("clarinet", "holdbothhandslarge")
    {
    }
  }

  public class Trumpet : InstrumentType
  {
    public Trumpet()
      : base("trumpet", "holdbothhandslarge")
    {
    }
  }

  public class Sax : InstrumentType
  {
    public Sax()
      : base("sax", "holdbothhandslarge")
    {
    }
  }

  public class Drum : InstrumentType
  {
    public Drum()
      : base("drum", "holdbothhandslarge")
    {
    }
  }

  public class SteelDrum : InstrumentType
  {
    public SteelDrum()
      : base("steeldrum", "holdbothhandslarge")
    {
    }
  }

  public class Accordion : InstrumentType
  {
    public Accordion()
      : base("accordion", "holdbothhandslarge")
    {
    }
  }

  public class MusicBox : InstrumentType
  {
    public MusicBox()
      : base("musicbox", "holdbothhandslarge")
    {
    }
  }

  public class Mic : InstrumentType
  {
    public Mic()
      : base("mic", "holdbothhandslarge")
    {
    }
  }

  public class MBoxComb : InstrumentType
  {
    public MBoxComb()
      : base("mboxcomb", "holdbothhandslarge")
    {
    }
  }

  public class VBow : InstrumentType
  {
    public VBow()
      : base("vbow", "holdbothhandslarge")
    {
    }
  }

  public class MusicBlockCone : InstrumentType
  {
    public MusicBlockCone()
      : base("musicblockcone", "holdbothhandslarge")
    {
    }
  }
}
