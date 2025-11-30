// Decompiled with JetBrains decompiler
// Type: Instruments.Files.FileManager
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using VSInstrumentsBase.src.Network.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using VSInstrumentsBase.src.Files;

#nullable disable
namespace VSInstrumentsBase.src.Files;

public abstract class FileManager
{
  private static int _nextRequestId;
  private static readonly string _sanitizeFileNameRegex = $"[{Regex.Escape(new string( Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray()))}]";

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected Dictionary<RequestId, FileRequest> Requests { get; private set; }

  protected static RequestId NextRequestID(int clientId)
  {
    return new RequestId(clientId, _nextRequestId++);
  }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public FileTree UserTree { get; private set; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public FileTree DataTree { get; private set; }

  private static byte[] CompressDeflate(Stream source)
  {
    using (MemoryStream memoryStream = new MemoryStream((int) source.Length))
    {
      using (DeflateStream deflateStream = new DeflateStream( memoryStream, CompressionMode.Compress, true))
        source.CopyTo( deflateStream);
       memoryStream.SetLength( memoryStream.Position);
       memoryStream.Seek(0L,  0);
      return memoryStream.ToArray();
    }
  }

  private static void DecompressDeflate(byte[] source, Stream destination)
  {
    using (DeflateStream deflateStream = new DeflateStream( new MemoryStream(source), CompressionMode.Decompress, true))
      deflateStream.CopyTo(destination);
  }

  public static byte[] Compress(Stream source, CompressionMethod compression)
  {
    switch (compression)
    {
      case CompressionMethod.None:
        byte[] numArray = new byte[source.Length - source.Position];
        source.Read(numArray);
        return numArray;
      case CompressionMethod.Deflate:
        return CompressDeflate(source);
      default:
        throw new NotImplementedException();
    }
  }

  public static void Decompress(byte[] source, Stream destination, CompressionMethod compression)
  {
    switch (compression)
    {
      case CompressionMethod.None:
        destination.Write(source);
        break;
      case CompressionMethod.Deflate:
                DecompressDeflate(source, destination);
        break;
      default:
        throw new NotImplementedException();
    }
  }

  protected FileManager(ICoreAPI api, string userPath, string dataPath)
  {
    try
    {
      Directory.CreateDirectory(userPath);
      Directory.CreateDirectory(dataPath);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("Failed to create required directories: " + ex.Message);
    }
        UserTree = new FileTree(userPath);
        DataTree = new FileTree(dataPath);
        Requests = new Dictionary<RequestId, FileRequest>(32 /*0x20*/);
    api.Event.RegisterGameTickListener(new Action<float>(Update), 10, 0);
  }

  protected virtual void Update(float deltaTime)
  {
        UserTree.Update(deltaTime);
        DataTree.Update(deltaTime);
  }

  protected FileStream CreateFile(string file) => DataTree.CreateFile(file);

  protected static void FileToPacket(
    FileTree.Node node,
    GetFileResponse packet,
    CompressionMethod compression = CompressionMethod.Deflate)
  {
    using (FileStream source = File.OpenRead(node.FullPath))
    {
      packet.Size = (int) ( source.Length -  source.Position);
      packet.Data = Compress(source, compression);
      packet.Compression = compression;
    }
  }

  public static string SanitizeUID(string uuid, string replacement = "")
  {
    return Regex.Replace(uuid, _sanitizeFileNameRegex, replacement);
  }

  public static string GetDataPath(IPlayer player, string file)
  {
    if (Path.IsPathFullyQualified(file))
      throw new ArgumentException("File must be relative path!");
    string str = SanitizeUID(player.PlayerUID);
    return !file.Contains(str) ? Path.Combine(str, file) : file;
  }

  protected FileRequest CreateRequest(
    IPlayer source,
    string file,
    RequestFileCallback callback,
    object context)
  {
    string dataPath = GetDataPath(source, file);
    FileTree.Node file1 = DataTree.Find(dataPath);
    if (file1 != null)
    {
      callback(file1, context);
      return  null;
    }
    if (Path.IsPathFullyQualified(dataPath) || Path.IsPathRooted(dataPath))
      throw new InvalidDataException();
        RequestId requestId = NextRequestID(source.ClientId);
        FileRequest request = new FileRequest(source, requestId, file, callback, context);
        Requests.Add(requestId, request);
    return request;
  }

  protected abstract void SubmitRequest(FileRequest request);

  protected void CompleteRequest(
    RequestId requestID,
    CreateFileCallback createFile)
  {
        FileRequest request;
    if (!Requests.TryGetValue(requestID, out request) || !Requests.Remove(requestID))
      return;
    FileTree.Node file = createFile(request);
    request.Complete(file);
  }

  public void RequestFile(
    IPlayer source,
    string file,
    RequestFileCallback completionCallback,
    object context = null)
  {
        FileRequest request = CreateRequest(source, file, completionCallback, context);
    if (request == null)
      return;
        SubmitRequest(request);
  }

  protected readonly struct RequestId
  {
    private readonly ulong _value;

    public RequestId(ulong value) => _value = value;

    public RequestId(int high, int low)
    {
            _value = (ulong) (uint) high << 32 /*0x20*/ |  (uint) low;
    }

    public RequestId(RequestId other) => _value = other._value;

    public int High => (int) (_value >> 32 /*0x20*/);

    public int Low => (int) ((long)_value &  uint.MaxValue);

    public static explicit operator RequestId(ulong value)
    {
      return new RequestId(value);
    }

    public static explicit operator ulong(RequestId requestId) => requestId._value;

    public override int GetHashCode() => _value.GetHashCode();
  }

  public delegate void RequestFileCallback(FileTree.Node file, object context);

  protected delegate FileTree.Node CreateFileCallback(FileRequest request);

  protected class FileRequest(
    IPlayer source,
    RequestId id,
    string file,
    RequestFileCallback callback,
    object context = null)
  {
    private readonly RequestId _id = id;
    private readonly IPlayer _source = source;
    private readonly string _file = file;
    private readonly RequestFileCallback _completionCallback = callback;
    private readonly object _context = context;

    public IPlayer Source => _source;

    public RequestId RequestId => _id;

    public string RelativePath => _file;

    public string DataPath => GetDataPath(_source, _file);

    public void Complete(FileTree.Node file) => _completionCallback(file, _context);
  }
}
