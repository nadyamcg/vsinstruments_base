using ProtoBuf;


namespace VSInstrumentsBase.src.Network.Packets;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MusicBlockPlayRequest
{
  public string SongPath;
  public int Channel;
  public int InstrumentId;
  public string BandName;
  public int BlockX;
  public int BlockY;
  public int BlockZ;
}
