using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Playback
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class StartPlaybackBroadcast
	{
		// playback slot id. for player playback this is the player's client id.
		// for music block playback this is a synthetic id derived from block position.
		public int ClientId;
		public string File;
		public int Channel;
		public int Instrument;
		public double StartTimeOffsetSec;
		public bool IsBlockSource;
		public int BlockX;
		public int BlockY;
		public int BlockZ;
	}
}
