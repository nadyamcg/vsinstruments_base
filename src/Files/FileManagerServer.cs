using VSInstrumentsBase.src.Network.Files;
using System;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Core;



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
      // the client declined or could not supply the file.
      if (!packet.Found || packet.Data == null)
        return null;

      long maxBytes = InstrumentModSettings.Instance.MaxMidiFileSizeBytes;

      // reject on the compressed size first, so an obviously oversized payload
      // never reaches the decompressor at all.
      if (packet.Data.LongLength > maxBytes)
        return null;

      string fullPath = null;
      try
      {
        using (FileStream file = CreateFile(request.DataPath))
        {
          fullPath = file.Name;
          // the real defence: a small deflate payload can expand without bound,
          // so the output is capped as it is written and the partial file is
          // discarded if it runs over.
          if (!TryDecompress(packet.Data, file, packet.Compression, maxBytes))
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

      return DataTree.Find(request.DataPath);
    });
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
      // the partial file is unusable either way; nothing further to do.
    }
  }

  protected void OnGetFileRequest(IServerPlayer source, GetFileRequest packet)
  {
    // answered rather than thrown. this handler runs on client-supplied input,
    // so a missing or malformed path is an ordinary outcome, not an exception:
    // throwing here let any client raise one on demand, and left the requester
    // waiting on a reply that never came.
    void Refuse()
    {
      ServerChannel.SendPacket(new GetFileResponse()
      {
        RequestId = packet.RequestId,
        Found = false
      }, [source]);
    }

    if (!IsSafeRelativePath(packet.File))
    {
      Refuse();
      return;
    }

    FileTree.Node node = DataTree.Find(GetDataPath(source, packet.File));
    if (node == null || !File.Exists(node.FullPath))
    {
      Refuse();
      return;
    }

    if (new FileInfo(node.FullPath).Length > InstrumentModSettings.Instance.MaxMidiFileSizeBytes)
    {
      Refuse();
      return;
    }

    GetFileResponse packet1 = new GetFileResponse();
    packet1.RequestId = packet.RequestId;
    packet1.Found = true;
        FileToPacket(node, packet1);
        ServerChannel.SendPacket(packet1, new IServerPlayer[1]
    {
      source
    });
  }
}
