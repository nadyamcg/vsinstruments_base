using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class NoteStop
	{
		public int ID;
	}
}
