// Decompiled with JetBrains decompiler
// Type: Instruments.Network.Packets.MusicBlockPlayRequest
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using ProtoBuf;

#nullable disable
namespace VSInstrumentsBase.src.Network.Packets;

[ProtoContract]
public class MusicBlockPlayRequest
{
  public string SongPath;
  public int Channel;
  public int InstrumentId;
}
