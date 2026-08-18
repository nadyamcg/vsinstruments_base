using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Playback
{
	// reply to a VSInstrumentsBase.src.Network.Playback.PreloadPlaybackRequest,
	// telling the client whether the server now holds a usable copy of the file.
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class PreloadPlaybackResponse
	{
		public string File;
		public int Channel;
		public bool Ready;
		public DenyPlaybackReason Reason;
	}
}
