using ProtoBuf;

namespace Instruments.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class ABCStartFromClient
	{
		public string abcData;
		public string bandName;
		public string instrument;
		public bool isServerFile;
	}
}