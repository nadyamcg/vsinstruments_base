using ProtoBuf;


namespace VSInstrumentsBase.src.Network.Playback;

[ProtoContract]
public class StopPlaybackBroadcast
{
  public int ClientId;
  public StopPlaybackReason Reason;
}
