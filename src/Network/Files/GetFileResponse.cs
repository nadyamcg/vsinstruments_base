using ProtoBuf;
using VSInstrumentsBase.src.Files;

namespace VSInstrumentsBase.src.Network.Files
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class GetFileResponse
	{
		//     the unique identifier of this request.
		public ulong RequestId;

		//     uncompressed (original) size.
		public int Size;

		//     used compression size.
		public CompressionMethod Compression;

		//     actual file data.
		public byte[] Data;
	}
}