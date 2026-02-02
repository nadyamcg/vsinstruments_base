using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Playback
{
	//
	// Summary:
	//     Packet broadcast to clients from the server informing them about a playback start.
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class StartPlaybackBroadcast
	{
		//
		// Summary:
		//     Id of the player that started the playback.
		public int ClientId;
		//
		// Summary:
		//     Relative path to the file to be played.
		public string File;
		//
		// Summary:
		//     The channel index to start playing.
		public int Channel;
		//
		// Summary:
		//     The unique identifier of instrument type used.
		public int Instrument;
	}
}