using VSInstrumentsBase.src.Network.Files;
using System;
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
    void Refuse()
    {
      this.ClientChannel.SendPacket<GetFileResponse>(new GetFileResponse()
      {
        RequestId = request.RequestId,
        Found = false
      });
    }

    if (!FileManager.IsSafeRelativePath(request.File))
    {
      Refuse();
      return;
    }

    FileTree.Node node = this.UserTree.Find(request.File);
    if (node == null)
    {
      Refuse();
      return;
    }

    // refuse locally too, so an oversized file gives the player a clear reason
    // here instead of being sent and silently dropped by the server.
    long maxBytes = InstrumentModSettings.Instance.MaxMidiFileSizeBytes;
    long size = new FileInfo(node.FullPath).Length;
    if (size > maxBytes)
    {
      this.ClientAPI.ShowChatMessage($"Instruments: \"{node.Name}\" is {size / 1024L} KiB, over the {maxBytes / 1024L} KiB limit, so it was not sent.");
      Refuse();
      return;
    }

    GetFileResponse packet = new GetFileResponse()
    {
      RequestId = request.RequestId,
      Found = true
    };
    FileManager.FileToPacket(node, packet);
    this.ClientChannel.SendPacket<GetFileResponse>(packet);
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
      // the peer could not supply it. complete with no node so the waiting
      // caller takes its failure path instead of hanging on the request.
      if (!packet.Found || packet.Data == null)
        return null;

      long maxBytes = InstrumentModSettings.Instance.MaxMidiFileSizeBytes;
      string fullPath = null;
      try
      {
        using (FileStream file = this.CreateFile(request.DataPath))
        {
          fullPath = file.Name;
          if (!FileManager.TryDecompress(packet.Data, (Stream) file, packet.Compression, maxBytes))
          {
            file.Dispose();
            TryDelete(fullPath);
            return null;
          }
        }
      }
      catch (Exception)
      {
        TryDelete(fullPath);
        return null;
      }

      return this.DataTree.Find(request.DataPath);
    }));
  }

  private static void TryDelete(string fullPath)
  {
    if (string.IsNullOrEmpty(fullPath))
      return;
    try
    {
      if (File.Exists(fullPath))
        File.Delete(fullPath);
    }
    catch (Exception)
    {
      // the partial file is unusable either way.
    }
  }
}
