using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Files;

[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
public class GetFileRequest
{
  public ulong RequestId;
  public string File;
}