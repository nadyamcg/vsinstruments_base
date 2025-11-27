using ProtoBuf;

namespace Instruments.Network.Files
{
	//
	// Summary:
	//     Packet sent to request a file.
	[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
	public class GetFileRequest
	{
		//
		// Summary:
		//     The unique identifier of this request.
		public ulong RequestId;
		//
		// Summary:
		//     File path relative to the user directory tree.
		public string File;
	}
}