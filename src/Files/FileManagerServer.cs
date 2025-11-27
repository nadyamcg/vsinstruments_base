using System.IO;
using Vintagestory.API.Server;
using Instruments.Core;
using Instruments.Network.Files;

namespace Instruments.Files
{
	//
	// Summary:
	//     This class handles file transfers on the server side.
	public class FileManagerServer : FileManager
	{
		//
		// Summary:
		//     Returns the interface to the game.
		protected ICoreServerAPI ServerAPI { get; }
		//	 
		// Summary:	 
		//     Returns the networking channel for file transactions.
		protected IServerNetworkChannel ServerChannel { get; private set; }
		//
		// Summary:
		//     Creates new file manager.
		// Parameters:
		//   api: The game interface.
		//   localPath: Root directory of the user path.
		//   dataPath: Root directory of the data path.
		public FileManagerServer(ICoreServerAPI api, string localPath, string dataPath) :
			base(api, localPath, dataPath)
		{
			ServerAPI = api;
			ServerChannel = api.Network.RegisterChannel(Constants.Channel.FileManager)
				.RegisterMessageType<GetFileRequest>()
				.RegisterMessageType<GetFileResponse>()

				.SetMessageHandler<GetFileRequest>(OnGetFileRequest)
				.SetMessageHandler<GetFileResponse>(OnGetFile);
		}
		//
		// Summary:
		//     Creates new file manager.
		public FileManagerServer(ICoreServerAPI api, InstrumentModSettings settings) :
			this(api, settings.LocalSongsDirectory, settings.DataSongsDirectory)
		{
		}
		//
		// Summary:
		//     Submits a file request for processing.
		protected override void SubmitRequest(FileRequest request)
		{
			GetFileRequest requestPacket = new GetFileRequest();
			requestPacket.RequestId = (ulong)request.RequestId;
			requestPacket.File = request.RelativePath;
			ServerChannel.SendPacket(requestPacket, request.Source as IServerPlayer);
		}
		//
		// Summary:
		//     Requests the provided file from the source player. Fires callback on completion.
		//     This method will return locally cached files, if any are present before dispatching requests.
		protected void OnGetFile(IServerPlayer source, GetFileResponse packet)
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
		//
		// Summary:
		//     Callback raised when a client requests song from the server.
		protected void OnGetFileRequest(IServerPlayer source, GetFileRequest packet)
		{
			string dataPath = GetDataPath(source, packet.File);
			FileTree.Node node = DataTree.Find(dataPath);
			if (node == null)
			{
				throw new FileNotFoundException();
			}

			// The server sends the playback ticket only once it has obtained its
			// own copy of the file. Fire straight away.
			// TODO@exocs:
			//   Cache the last used files in already compressed state,
			//   and just dispatch the available data directly.
			GetFileResponse response = new GetFileResponse();
			response.RequestId = packet.RequestId;
			FileToPacket(node, response);

			ServerChannel.SendPacket(response, source);
		}
	}
}
