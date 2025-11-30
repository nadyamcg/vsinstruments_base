namespace VSInstrumentsBase.src.Network.Playback;

public enum StopPlaybackReason
{
  Unspecified,
  Cancelled,
  Terminated,
  Finished,
  ClientDisconnected,
  ChangedSlot,
  Died,
}
