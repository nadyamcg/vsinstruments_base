using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Instruments.Network.Files;

namespace Instruments.Files
{
	//
	// Summary:
	//     Determines the method of compression used.
	public enum CompressionMethod
	{
		None,
		Deflate
	}
	//
	// Summary:
	//     Base class for all file management operations, compressions and file transfers.
	public abstract class FileManager
	{
		//
		// Summary:
		//     This structure represents a single file request identifier.
		//     It is composed of two 32 bit values to create unique ranges for individual clients.
		protected readonly struct RequestId
		{
			//
			// Summary:
			//     The internal composed value.
			private readonly ulong _value;
			//
			// Summary:
			//     Creates new request identifier from the provided composed value.
			public RequestId(ulong value)
			{
				_value = value;
			}
			//
			// Summary:
			//     Creates new request identifier from the high and low components of the composed value.
			public RequestId(int high, int low)
			{
				_value = ((ulong)(uint)high << 32) | (uint)low;
			}
			//
			// Summary:
			//     Creates a copy of a request identifier.
			public RequestId(RequestId other)
			{
				_value = other._value;
			}
			//
			// Summary:
			//     Returns the high component of the internal value.
			public int High
			{
				get
				{
					return (int)(_value >> 32);
				}
			}
			//
			// Summary:
			//     Returns the low component of the internal value.
			public int Low
			{
				get
				{
					return (int)(_value & 0xFFFFFFFF);
				}
			}
			//
			// Summary:
			//     Converts an internal value to a request identifier.
			public static explicit operator RequestId(ulong value)
			{
				return new RequestId(value);
			}
			//
			// Summary:
			//     Converts a request identifier to its internal value.
			public static explicit operator ulong(RequestId requestId)
			{
				return requestId._value;
			}
			//
			// Summary:
			//     Returns hash code for this identifier.
			public override int GetHashCode()
			{
				return _value.GetHashCode();
			}
		}

		//
		// Summary:
		//     Method delegate for callbacks that request a file.
		public delegate void RequestFileCallback(FileTree.Node file, object context);
		//
		// Summary:
		//     Method delegate for callbacks that finish a file request.
		protected delegate FileTree.Node CreateFileCallback(FileRequest request);
		//
		// Summary:
		//     A single file request that can be queued for the manager to process.
		protected class FileRequest
		{
			//
			// Summary:
			//     Unique identifier of this request.
			private RequestId _id;
			//
			// Summary:
			//     The player the file is requested from.
			private IPlayer _source;
			//
			// Summary:
			//     Relative file path to the requested file.
			private string _file;
			//
			// Summary:
			//     Completion callback.
			private RequestFileCallback _completionCallback;
			//
			// Summary:
			//     User-provided context object or null.
			private object _context;
			//
			// Summary:
			//     Creates new file request.
			public FileRequest(IPlayer source, RequestId id, string file, RequestFileCallback callback, object context = null)
			{
				_source = source;
				_file = file;
				_id = id;
				_completionCallback = callback;
				_context = context;
			}
			//
			// Summary:
			//     Returns the source client.
			public IPlayer Source
			{
				get
				{
					return _source;
				}
			}
			//
			// Summary:
			//     Returns the unique identifier of this request.
			public RequestId RequestId
			{
				get
				{
					return _id;
				}
			}
			//
			// Summary:
			//     Returns the target relative path of this request.
			public string RelativePath
			{
				get
				{
					return _file;
				}
			}
			//
			// Summary:
			//     Returns the target data path of this request.
			public string DataPath
			{
				get
				{
					return GetDataPath(_source, _file);
				}
			}
			//
			// Summary:
			//     Completes this request.
			public void Complete(FileTree.Node file)
			{
				_completionCallback(file, _context);
			}
		}
		//
		// Summary:
		//     Pending requests by their id.
		// TODO@exocs: Timeout and clear on player disconnection
		protected Dictionary<RequestId, FileRequest> Requests { get; private set; }
		//
		// Summary:
		//     Last used request ID.
		private static int _nextRequestId;
		//
		// Summary:
		//     Returns next request ID in sequence.
		protected static RequestId NextRequestID(int clientId)
		{
			return new RequestId(clientId, _nextRequestId++);
		}
		//
		// Summary:	 
		//     The file tree that represents the user directory with local files.
		public FileTree UserTree { get; private set; }
		//
		// Summary:	 
		//     The file tree that represents the shared data directory with files received from the server or other clients.
		public FileTree DataTree { get; private set; }
		//
		// Summary:
		//     Compresses the provided stream using the Deflate algorithm.
		private static byte[] CompressDeflate(Stream source)
		{
			// Allocate enough memory up front to prevent unnecessary allocations. In vast majority of the cases
			// the compressed size should be lower than the initial size. There may be rare cases where e.g. the
			// input stream is already compressed and the size may end up larger - let the memory stream grow in
			// such cases, but don't account for them initially as these are unlikely.
			using (MemoryStream destination = new MemoryStream((int)source.Length))
			{
				// Run the source through the compression first and make sure the compression stream is closed,
				// to ensure it has been closed and disposed of properly before manipulating it further.
				using (DeflateStream compress = new DeflateStream(destination, CompressionMode.Compress, leaveOpen: true))
				{
					source.CopyTo(compress);
				}

				// Now that the compression is done, resize the output to the actual compressed size,
				// compating the resulting buffer in the process.
				destination.SetLength(destination.Position);
				destination.Seek(0, SeekOrigin.Begin);
				return destination.ToArray();
			}
		}
		//
		// Summary:
		//     Decompresses the provided stream using the Deflate algorithm.
		private static void DecompressDeflate(byte[] source, Stream destination)
		{
			MemoryStream sourceStream = new MemoryStream(source);
			using (DeflateStream decompressionStream = new DeflateStream(sourceStream, CompressionMode.Decompress, true))
			{
				// Run the source through the compression stream before resizing the output to the actual
				// size, dropping our initial conservative reserve and return to the start of the stream.
				decompressionStream.CopyTo(destination);
			}
		}
		//
		// Summary:
		//     Compresses the provided stream into a byte stream via the provided compression method.
		public static byte[] Compress(Stream source, CompressionMethod compression)
		{
			switch (compression)
			{
				case CompressionMethod.None:
					long size = (source.Length - source.Position);
					byte[] buffer = new byte[size];
					source.Read(buffer);
					return buffer;
				case CompressionMethod.Deflate:
					return CompressDeflate(source);
				default:
					throw new NotImplementedException();
			}
		}
		//
		// Summary:
		//     Decompresses the provided stream into a byte stream via the provided compression method.
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
		//
		// Summary:
		//     Creates new file manager.
		// Parameters:
		//   api: The game interface.
		//   localPath: Root directory of the user path.
		//   dataPath: Root directory of the data path.
		protected FileManager(ICoreAPI api, string userPath, string dataPath)
		{
			UserTree = new FileTree(userPath);
			DataTree = new FileTree(dataPath);
			Requests = new Dictionary<RequestId, FileRequest>(32);

			api.Event.RegisterGameTickListener(Update, Constants.Files.ManagerTickInterval);
		}
		//
		// Summary:
		//     Periodic update called to poll and propagate file tree changes.
		protected virtual void Update(float deltaTime)
		{
			UserTree.Update(deltaTime);
			DataTree.Update(deltaTime);
		}
		//
		// Summary:
		//     Creates file for writing at the provided location in the Data tree.
		protected FileStream CreateFile(string file)
		{
			return DataTree.CreateFile(file);
		}
		//
		// Summary:
		//     Fills the provided packet with the provided file data.
		protected void FileToPacket(FileTree.Node node, GetFileResponse packet, CompressionMethod compression = CompressionMethod.Deflate)
		{
			using (FileStream file = File.OpenRead(node.FullPath))
			{
				packet.Size = (int)(file.Length - file.Position);
				packet.Data = Compress(file, compression);
				packet.Compression = compression;
			}
		}
		//
		// Summary:
		//     Regex used for sanitization of file names.
		private static string _sanitizeFileNameRegex;
		//
		// Summary:
		//     Initialize static properties of the file manager.
		static FileManager()
		{
			char[] invalidCharacters = Path.GetInvalidFileNameChars()
				.Concat(Path.GetInvalidPathChars())
				.Distinct()
				.ToArray();

			_sanitizeFileNameRegex = $"[{Regex.Escape(new string(invalidCharacters))}]";
		}
		//
		// Summary:
		//     Returns sanitized unique user id from the provided user id.
		// Parameters:
		//   uid: Unique user id to sanitize.
		//   replacement: The string disallowed characters are replaced by.
		public static string SanitizeUID(string uuid, string replacement = "")
		{
			return Regex.Replace(uuid, _sanitizeFileNameRegex, replacement);
		}
		//
		// Summary:
		//     Returns the relative path for a file of provided player as a path in the data tree.
		public static string GetDataPath(IPlayer player, string file)
		{
			if (Path.IsPathFullyQualified(file))
				throw new ArgumentException("File must be relative path!");

			string uid = SanitizeUID(player.PlayerUID);
			if (!file.Contains(uid))
			{
				return Path.Combine(uid, file);
			}

			return file;
		}
		//
		// Summary:
		//     Creates new file request with the provided data. The returned request may be null and the 
		//     specified completion callback may be invoked immediately if a cached file for given file
		//     is already present and managed by this manager.
		protected FileRequest CreateRequest(IPlayer source, string file, RequestFileCallback callback, object context)
		{
			// If the file is already present, there is no need to create a request,
			// return the file directly instead:
			string dataPath = GetDataPath(source, file);
			FileTree.Node data = DataTree.Find(dataPath);
			if (data != null)
			{
				callback.Invoke(data, context);
				return null;
			}

			// TODO@exocs: Validate the path and make sure it's not illicit!
			// For now at least something C:
			if (Path.IsPathFullyQualified(dataPath) || Path.IsPathRooted(dataPath))
				throw new InvalidDataException();

			// With the file not present, add the request to the "queue".
			RequestId requestID = NextRequestID(source.ClientId);
			FileRequest request = new FileRequest(source, requestID, file, callback, context);
			Requests.Add(requestID, request);

			return request;
		}
		//
		// Summary:
		//     Submits the request for processing, for instance by sending a packet.
		protected abstract void SubmitRequest(FileRequest request);
		//
		// Summary:
		//     Completes the provided request, returning the resulting file node.
		protected void CompleteRequest(RequestId requestID, CreateFileCallback createFile)
		{
			if (Requests.TryGetValue(requestID, out FileRequest request))
			{
				if (Requests.Remove(requestID))
				{
					FileTree.Node result = createFile(request);
					request.Complete(result);
				}
			}
		}
		//
		// Summary:
		//     Requests a file from this manager.
		// Parameters:
		//   source: The source of this file or in other words, the original owner.
		//   file: Relative path of the file to be requested.
		//   completionCallback: Callback raised when the requested file becomes available.
		//   context: Optional user-provided context passed to completion callback.
		public void RequestFile(IPlayer source, string file, RequestFileCallback completionCallback, object context = null)
		{
			FileRequest request = CreateRequest(source, file, completionCallback, context);
			if (request != null)
			{
				SubmitRequest(request);
			}
		}
	}
}
