using ProtoBuf;
using VSInstrumentsBase.src.Files;


namespace VSInstrumentsBase.src.Network.Files;

[ProtoContract]
public class GetFileResponse
{
  public ulong RequestId;
  public int Size;
  public CompressionMethod Compression;
  public byte[] Data;
}
