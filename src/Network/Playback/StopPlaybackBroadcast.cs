using ProtoBuf;

namespace Instruments.Network.Playback
{
	//
	// Summary:
	//     Response packet broadcast to all clients from the server to stop a playback.
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class StopPlaybackBroadcast
	{
		//
		// Summary:
		//     Id of the player of which the playback is about to stop.
		public int ClientId;
		//
		// Summary:
		//     Determines the reason for why the playback was stopped.
		public StopPlaybackReason Reason;
	}
}