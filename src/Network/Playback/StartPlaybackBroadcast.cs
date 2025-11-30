using ProtoBuf;


namespace VSInstrumentsBase.src.Network.Playback;

[ProtoContract]
public class StartPlaybackBroadcast
{
  public int ClientId;
  public string File;
  public int Channel;
  public int Instrument;
}
