using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Playback
{
	//
	// Summary:
	//     Response packet sent to the instigator (the requesting client) from the server upon successfull playback request.
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class StartPlaybackOwner
	{
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