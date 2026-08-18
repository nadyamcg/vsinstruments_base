using ProtoBuf;
using VSInstrumentsBase.src.Files;

namespace VSInstrumentsBase.src.Network.Files
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class GetFileResponse
	{
		//     the unique identifier of this request.
		public ulong RequestId;

		//     false when the peer could not supply the file. missing, too large,
		//     or refused. Data is meaningless in that case.
		public bool Found;

		//     uncompressed (original) size.
		public int Size;

		//     used compression size.
		public CompressionMethod Compression;

		//     actual file data.
		public byte[] Data;
	}
}