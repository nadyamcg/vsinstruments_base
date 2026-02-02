using ProtoBuf;


namespace VSInstrumentsBase.src.Network.Packets;

[ProtoContract]
public class MusicBlockPlayRequest
{
  public string SongPath;
  public int Channel;
  public int InstrumentId;
}
