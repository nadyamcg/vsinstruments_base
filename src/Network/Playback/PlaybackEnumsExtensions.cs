// Decompiled with JetBrains decompiler
// Type: Instruments.Network.Playback.PlaybackEnumsExtensions
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

#nullable disable
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
