using ProtoBuf;
using Instruments.Files;

namespace Instruments.Network.Files
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class GetFileResponse
	{
		//
		// Summary:
		//     The unique identifier of this request.
		public ulong RequestId;
		//
		// Summary:
		//     Uncompressed (original) size.
		public int Size;
		//
		// Summary:
		//     Used compression size.
		public CompressionMethod Compression;
		//
		// Summary:
		//     Actual file data.
		public byte[] Data;
	}
}