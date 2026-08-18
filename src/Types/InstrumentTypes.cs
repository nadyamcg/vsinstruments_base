using Vintagestory.API.Common;
using VSInstrumentsBase.src.Mapping;


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
    // instruments previously contributed by the sf2packs. moved into base so the
    // items always exist regardless of which sound pack (if any) is installed;
    // sf2packs now only override the audio samples.
    InstrumentType.Register(api, typeof (InstrumentTypes.Flute), (InstrumentType) new InstrumentTypes.Flute());
    InstrumentType.Register(api, typeof (InstrumentTypes.Marimba), (InstrumentType) new InstrumentTypes.Marimba());
    InstrumentType.Register(api, typeof (InstrumentTypes.Cello), (InstrumentType) new InstrumentTypes.Cello());
    InstrumentType.Register(api, typeof (InstrumentTypes.Trombone), (InstrumentType) new InstrumentTypes.Trombone());
    InstrumentType.Register(api, typeof (InstrumentTypes.Banjo), (InstrumentType) new InstrumentTypes.Banjo());
    InstrumentType.Register(api, typeof (InstrumentTypes.Oboe), (InstrumentType) new InstrumentTypes.Oboe());
    api.Logger.Notification("[InstrumentTypes] Registered 22 instrument types by name");
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

    // drum samples are named <midi-note>.ogg, not a0..a7.
    protected override NoteMapping<string> CreateNoteMap()
      => new NoteMappingDrum("sounds/" + this.Name);
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

  public class Flute : InstrumentType
  {
    public Flute() : base("flute", "holdbothhandslarge") { }
  }

  public class Marimba : InstrumentType
  {
    public Marimba() : base("marimba", "holdbothhandslarge") { }
  }

  public class Cello : InstrumentType
  {
    public Cello() : base("cello", "holdbothhandslarge") { }
  }

  public class Trombone : InstrumentType
  {
    public Trombone() : base("trombone", "holdbothhandslarge") { }
  }

  public class Banjo : InstrumentType
  {
    public Banjo() : base("banjo", "holdbothhandslarge") { }
  }

  public class Oboe : InstrumentType
  {
    public Oboe() : base("oboe", "holdbothhandslarge") { }
  }
}
