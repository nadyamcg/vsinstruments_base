using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Playback
{
	//
	// Summary:
	//     Response packet sent to the instigator (the requesting client) of the playback, when the playback gets denied.
	[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
	public class StartPlaybackDenyOwner
	{
		//
		// Summary:
		//     The reason why the playback was denied.
		public DenyPlaybackReason Reason;
	}
}