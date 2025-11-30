using ProtoBuf;


namespace VSInstrumentsBase.src.Network.Files;

[ProtoContract]
public class GetFileRequest
{
  public ulong RequestId;
  public string File;
}
