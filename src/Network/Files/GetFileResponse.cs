// Decompiled with JetBrains decompiler
// Type: Instruments.Network.Files.GetFileResponse
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using ProtoBuf;
using VSInstrumentsBase.src.Files;

#nullable disable
namespace VSInstrumentsBase.src.Network.Files;

[ProtoContract]
public class GetFileResponse
{
  public ulong RequestId;
  public int Size;
  public CompressionMethod Compression;
  public byte[] Data;
}
