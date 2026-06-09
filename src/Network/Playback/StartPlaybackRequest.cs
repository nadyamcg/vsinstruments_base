using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Playback
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class StartPlaybackRequest
	{
		public string File;
		public int Channel;
		public int Instrument;
		public string BandName;
		// when true, this request is from a music block, not a player.
		// the server uses BlockX/Y/Z to derive a per-block playback slot.
		public bool IsBlockSource;
		public int BlockX;
		public int BlockY;
		public int BlockZ;
	}
}
