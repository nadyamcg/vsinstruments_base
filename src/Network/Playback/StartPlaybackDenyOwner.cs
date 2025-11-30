using ProtoBuf;


namespace VSInstrumentsBase.src.Network.Playback;

[ProtoContract]
public class StartPlaybackDenyOwner
{
  public DenyPlaybackReason Reason;
}
