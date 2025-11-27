using System.IO;
using Vintagestory.API.Client;
using Instruments.Core;
using Instruments.Network.Files;

namespace Instruments.Files
{
	//
	// Summary:
	//     This class handles file transfers on the client side.
	public class FileManagerClient : FileManager
	{
		//	 
		// Summary:	 
		//     Returns the interface to the game.
		protected ICoreClientAPI ClientAPI { get; }
		//	 
		// Summary:	 
		//     Returns the networking channel for file transactions.
		protected IClientNetworkChannel ClientChannel { get; private set; }
		//
		// Summary:
		//     Creates new file manager.
		// Parameters:
		//   api: The game interface.
		//   localPath: Root directory of the user path.
		//   dataPath: Root directory of the data path.
		public FileManagerClient(ICoreClientAPI api, string localPath, string dataPath) :
			base(api, localPath, dataPath)
		{
			ClientAPI = api;
			ClientChannel = api.Network.RegisterChannel(Constants.Channel.FileManager)
				.RegisterMessageType<GetFileRequest>()
				.RegisterMessageType<GetFileResponse>()

				.SetMessageHandler<GetFileRequest>(OnFileRequested)
				.SetMessageHandler<GetFileResponse>(OnGetFile);

		}
		//
		// Summary:
		//     Creates new file manager.
		public FileManagerClient(ICoreClientAPI api, InstrumentModSettings settings) :
			this(api, settings.LocalSongsDirectory, settings.DataSongsDirectory)
		{
		}
		//
		// Summary:
		//     Callback raised when the server requests a file.
		//   TODO@exocs: This is a bit unconventional in relation to the rest of the API
		protected void OnFileRequested(GetFileRequest request)
		{
			FileTree.Node node = UserTree.Find(request.File);
			if (node == null)
			{
				// TODO@exocs:
				//  If the user moved or removed the file shortly after they sent a request..
				//  just scold them for being an idiot honestly. Fix this later.
				ClientAPI.ShowChatMessage("Why are you like this?");
				return;
			}

			// Directly send the response back.
			GetFileResponse response = new GetFileResponse();
			response.RequestId = request.RequestId;
			FileToPacket(node, response);
			ClientChannel.SendPacket(response);
		}
		//
		// Summary:
		//     Submits a file request for processing.
		protected override void SubmitRequest(FileRequest request)
		{
			GetFileRequest requestPacket = new GetFileRequest();
			requestPacket.RequestId = (ulong)request.RequestId;
			requestPacket.File = request.RelativePath;
			ClientChannel.SendPacket(requestPacket);
		}
		//
		// Summary:
		//     Callback raised when the server requests a file.
		protected void OnGetFile(GetFileResponse packet)
		{
			CompleteRequest((RequestId)packet.RequestId, (request) =>
			{
				using (FileStream file = CreateFile(request.DataPath))
				{
					Decompress(packet.Data, file, packet.Compression);
				}
				return DataTree.Find(request.DataPath);
			});
		}
	}
}
