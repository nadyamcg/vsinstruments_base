// Decompiled with JetBrains decompiler
// Type: Instruments.Network.Playback.StartPlaybackOwner
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using ProtoBuf;

#nullable disable
namespace VSInstrumentsBase.src.Network.Playback;

[ProtoContract]
public class StartPlaybackOwner
{
  public string File;
  public int Channel;
  public int Instrument;
}
