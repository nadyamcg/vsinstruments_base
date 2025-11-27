using ProtoBuf;

namespace Instruments.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class ABCStopFromServer
	{
		public int fromClientID;
	}
}