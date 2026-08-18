using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Playback
{
	// request packet sent to the server when a client loads a track, ahead of actually playing it.
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class PreloadPlaybackRequest
	{
		public string File;
		public int Channel;
	}
}
