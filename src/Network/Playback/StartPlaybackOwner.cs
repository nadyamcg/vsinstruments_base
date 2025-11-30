using ProtoBuf;


namespace VSInstrumentsBase.src.Network.Playback;

[ProtoContract]
public class StartPlaybackOwner
{
  public string File;
  public int Channel;
  public int Instrument;
}
