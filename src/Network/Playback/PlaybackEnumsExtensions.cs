
namespace VSInstrumentsBase.src.Network.Playback;

public static class PlaybackEnumsExtensions
{
  public static string GetText(this DenyPlaybackReason reason)
  {
    switch (reason)
    {
      case DenyPlaybackReason.InvalidFile:
        return "Invalid file request.";
      case DenyPlaybackReason.TooManyRequests:
        return "Too many requests.";
      case DenyPlaybackReason.OperationInProgress:
        return "An operation is already in progress.";
      default:
        return "Unspecified reason.";
    }
  }

  public static string GetText(this StopPlaybackReason reason)
  {
    switch (reason)
    {
      case StopPlaybackReason.Cancelled:
        return "Cancelled by the user.";
      case StopPlaybackReason.Terminated:
        return "Terminated by the user.";
      case StopPlaybackReason.Finished:
        return "Playback has finished.";
      case StopPlaybackReason.ClientDisconnected:
        return "Client has disconnected.";
      default:
        return "Unspecified reason.";
    }
  }
}
