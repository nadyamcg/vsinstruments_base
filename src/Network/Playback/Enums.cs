namespace VSInstrumentsBase.src.Network.Playback
{
	//
	// Summary:
	//     Specifies the reasons for which a playback may be denied.
	public enum DenyPlaybackReason
	{
		//
		// Summary:
		//     Unspecified, general failure response.
		Unspecified,
		//
		// Summary:
		//     Request was denied because the file was invalid.
		InvalidFile,
		//
		// Summary:
		//     Request was denied because there were too many requests.
		TooManyRequests,
		//
		// Summary:
		//     Request was denied because there is an ongoing operation already.
		OperationInProgress,
	}

	//
	// Summary:
	//     Specifies the reasons for which a playback may be stopped.
	public enum StopPlaybackReason
	{
		//
		// Summary:
		//     Unspecified, general failure response.
		Unspecified,
		//
		// Summary:
		//     Playback was cancelled by the user.
		Cancelled,
		//
		// Summary:
		//     Playback was abruptly terminated by the user - for instance the instrument was dropped or moved from the hotbar.
		Terminated,
		//
		// Summary:
		//     The playback has been completed.
		Finished,
		//
		// Summary:
		//     Playback was stopped, because the player disconnected.
		ClientDisconnected,
		//
		// Summary:
		//     Playback was stopped, because the active slot changed.
		ChangedSlot,
		//
		// Summary:
		//     Playback was stopped, because the player died.
		Died
	}

	//
	// Summary:
	//     This class provides various extensions and utility method for playback related enums.
	public static class PlaybackEnumsExtensions
	{
		//
		// Summary:
		//     Returns the text representation of reason for why a request was denied.
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
			}

			return "Unspecified reason.";
		}
		//
		// Summary:
		//     Returns the text representation of reason for why a playback was stopped.
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
				case StopPlaybackReason.ChangedSlot:
					return "Active slot changed.";
				case StopPlaybackReason.Died:
					return "Player died.";
			}

			return "Unspecified reason.";
		}
	}
}
