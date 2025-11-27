using ProtoBuf;

namespace Instruments.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class ABCStopFromClient
	{
		public bool dummy;
	}
}