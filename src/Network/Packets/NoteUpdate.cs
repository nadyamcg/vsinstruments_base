using ProtoBuf;
using Vintagestory.API.MathTools;

namespace VSInstrumentsBase.src.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class NoteUpdate // Same as NoteStart, any better way to do this?
	{
		public float pitch;
		public Vec3d positon;
		public int ID;
	}
}
