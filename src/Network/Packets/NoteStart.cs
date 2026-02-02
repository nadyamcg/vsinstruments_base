using ProtoBuf;
using Vintagestory.API.MathTools;

namespace VSInstrumentsBase.src.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class NoteStart
	{
		public float pitch;
		public Vec3d positon;
		public int ID;
		public string instrument;
	}
}
