using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Instruments.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class ABCUpdateFromServer
	{
		public Vec3d positon;
		public Chord newChord;
		public int fromClientID;
		//
		// Summary:
		//     Unique identifier of the associated instrument type. See also InstrumentItemType.
		public int InstrumentTypeID;
	}
}