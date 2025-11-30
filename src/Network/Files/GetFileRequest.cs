using ProtoBuf;

namespace VSInstrumentsBase.src.Network.Files
{

	//     packet sent to request a file.
	[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
	public class GetFileRequest
	{
		//     the unique identifier of the request.
		public ulong RequestId;

		//     file path relative to the user directory tree.
		public string File;
	}
}