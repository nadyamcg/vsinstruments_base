using ProtoBuf;

namespace Instruments.Network.Playback
{
	//
	// Summary:
	//     Request packet sent to the server from clients to stop a playback.
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class StopPlaybackRequest
	{
	}
}