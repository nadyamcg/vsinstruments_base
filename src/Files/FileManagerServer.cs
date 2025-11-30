// Decompiled with JetBrains decompiler
// Type: Instruments.Files.FileManagerServer
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src.Network.Files;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Core;


#nullable disable
namespace VSInstrumentsBase.src.Files;

public class FileManagerServer : FileManager
{
  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected ICoreServerAPI ServerAPI { get; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected IServerNetworkChannel ServerChannel { get; private set; }

  public FileManagerServer(ICoreServerAPI api, string localPath, string dataPath)
    : base(api, localPath, dataPath)
  {
        ServerAPI = api;
        ServerChannel = api.Network.RegisterChannel("FileTransferChannel")
      .RegisterMessageType<GetFileRequest>()
      .RegisterMessageType<GetFileResponse>()
      .SetMessageHandler<GetFileRequest>(OnGetFileRequest)
      .SetMessageHandler<GetFileResponse>(OnGetFile);
  }

  public FileManagerServer(ICoreServerAPI api, InstrumentModSettings settings)
    : this(api, settings.LocalSongsDirectory, settings.DataSongsDirectory)
  {
  }

  protected override void SubmitRequest(FileRequest request)
  {
        ServerChannel.SendPacket(new GetFileRequest()
    {
      RequestId = (ulong) request.RequestId,
      File = request.RelativePath
    }, new IServerPlayer[1]
    {
      request.Source as IServerPlayer
    });
  }

  protected void OnGetFile(IServerPlayer source, GetFileResponse packet)
  {
        CompleteRequest((RequestId) packet.RequestId,  request =>
    {
      using (FileStream file = CreateFile(request.DataPath))
            Decompress(packet.Data,  file, packet.Compression);
      return DataTree.Find(request.DataPath);
    });
  }

  protected void OnGetFileRequest(IServerPlayer source, GetFileRequest packet)
  {
    FileTree.Node node = DataTree.Find(GetDataPath(source, packet.File));
    if (node == null)
      throw new FileNotFoundException();
    GetFileResponse packet1 = new GetFileResponse();
    packet1.RequestId = packet.RequestId;
        FileToPacket(node, packet1);
        ServerChannel.SendPacket(packet1, new IServerPlayer[1]
    {
      source
    });
  }
}
