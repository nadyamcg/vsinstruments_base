using VSInstrumentsBase.src.Network.Files;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using VSInstrumentsBase.src.Core;
using VSInstrumentsBase.src.Files;


namespace VSInstrumentsBase.src.Files;

public class FileManagerClient : FileManager
{
  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected ICoreClientAPI ClientAPI { get; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected IClientNetworkChannel ClientChannel { get; private set; }

  public FileManagerClient(ICoreClientAPI api, string localPath, string dataPath)
    : base(api, localPath, dataPath)
  {
    this.ClientAPI = api;
    this.ClientChannel = api.Network.RegisterChannel("FileTransferChannel")
      .RegisterMessageType<GetFileRequest>()
      .RegisterMessageType<GetFileResponse>()
      .SetMessageHandler<GetFileRequest>(OnFileRequested)
      .SetMessageHandler<GetFileResponse>(OnGetFile);
    List<FileTree.Node> destination1 = new();
    this.UserTree.GetNodes(destination1, FileTree.Filter.Files);
    api.Logger.Notification($"[FileManagerClient] UserTree initialized: {this.UserTree.Root.FullPath}, Files: {destination1.Count}");
    List<FileTree.Node> destination2 = new();
    this.DataTree.GetNodes(destination2, FileTree.Filter.Files);
    api.Logger.Notification($"[FileManagerClient] DataTree initialized: {this.DataTree.Root.FullPath}, Files: {destination2.Count}");
  }

  public FileManagerClient(ICoreClientAPI api, InstrumentModSettings settings)
    : this(api, settings.LocalSongsDirectory, settings.DataSongsDirectory)
  {
  }

  protected void OnFileRequested(GetFileRequest request)
  {
    FileTree.Node node = this.UserTree.Find(request.File);
    if (node == null)
    {
      this.ClientAPI.ShowChatMessage("Why are you like this?");
    }
    else
    {
      GetFileResponse packet = new GetFileResponse()
      {
        RequestId = request.RequestId
      };
      FileManager.FileToPacket(node, packet);
      this.ClientChannel.SendPacket<GetFileResponse>(packet);
    }
  }

  protected override void SubmitRequest(FileManager.FileRequest request)
  {
    this.ClientChannel.SendPacket<GetFileRequest>(new GetFileRequest()
    {
      RequestId = (ulong) request.RequestId,
      File = request.RelativePath
    });
  }

  protected void OnGetFile(GetFileResponse packet)
  {
    this.CompleteRequest((FileManager.RequestId) packet.RequestId, (FileManager.CreateFileCallback) (request =>
    {
      using (FileStream file = this.CreateFile(request.DataPath))
        FileManager.Decompress(packet.Data, (Stream) file, packet.Compression);
      return this.DataTree.Find(request.DataPath);
    }));
  }
}
