using ProtoBuf;

namespace Instruments.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class ABCSendSongFromServer
	{
		public string abcFilename;
	}
}