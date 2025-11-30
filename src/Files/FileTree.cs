using VSInstrumentsBase.src.GUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSInstrumentsBase.src.Files;

public class FileTree : IDisposable
{
  private readonly 
  
  FileTree.RootNode _rootNode;
  private FileSystemWatcher _watcher;
  private readonly string _searchPattern = "*";
  protected ConcurrentQueue<FileTree.IQueuedEvent> _eventQueue = new ConcurrentQueue<FileTree.IQueuedEvent>();
  public 
  
  FileTree.NodeChange? NodeCreated;
  public FileTree.NodeChange? NodeChanged;
  public FileTree.NodeRename? NodeRenamed;
  public FileTree.NodeChange? NodeDeleted;

  private void BuildBranches(
  
  FileTree.Node node)
  {
    if (!node.IsDirectory)
      return;
    string fullPath = node.FullPath;
    EnumerationOptions enumerationOptions = new EnumerationOptions()
    {
      IgnoreInaccessible = true,
      RecurseSubdirectories = false
    };
    foreach (string enumerateFileSystemEntry in Directory.EnumerateFileSystemEntries(fullPath, this._searchPattern, enumerationOptions))
    {
      FileTree.Node node1 = new FileTree.Node(enumerateFileSystemEntry);
      node.AddChild(node1);
      this.BuildBranches(node1);
    }
    node.SortChildren();
  }

  private FileTree.RootNode BuildTree(string rootFullPath)
  {
    FileTree.RootNode rootNode = new FileTree.RootNode(this, rootFullPath);
    this.BuildBranches((FileTree.Node) rootNode);
    return rootNode;
  }

  private static void DeleteNode(FileTree.Node node) => node.Delete();

  public FileTree(string root)
  {
    this._rootNode = Path.Exists(root) ? this.BuildTree(root) : throw new ArgumentException("File tree creation failed, provided path doesn't exist!");
    this._watcher = new FileSystemWatcher(root);
    this._watcher.Changed += new FileSystemEventHandler(this.OnWatcherChangedEvent);
    this._watcher.Created += new FileSystemEventHandler(this.OnWatcherCreatedEvent);
    this._watcher.Renamed += new RenamedEventHandler(this.OnWatcherRenamedEvent);
    this._watcher.Deleted += new FileSystemEventHandler(this.OnWatcherDeletedEvent);
    this._watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite;
    this._watcher.IncludeSubdirectories = true;
    this._watcher.EnableRaisingEvents = true;
  }

  protected void PushEvent(FileTree.IQueuedEvent queuedEvent)
  {
    lock (this._eventQueue)
      this._eventQueue.Enqueue(queuedEvent);
  }

  protected void PollEvents()
  {
    lock (this._eventQueue)
    {
      FileTree.IQueuedEvent result;
      while (!this._eventQueue.IsEmpty && this._eventQueue.TryDequeue(out result))
        result.Invoke(this);
    }
  }

  private void OnWatcherRenamedEvent(object sender, RenamedEventArgs args)
  {
    FileTree.Node node = this.Find(args.OldFullPath);
    if (node == null || args.Name == null)
      return;
    string fileName = Path.GetFileName(args.Name);
    string name = node.Name;
    node.Rename(fileName);
    this.PushEvent((FileTree.IQueuedEvent) new FileTree.RenamedEvent(node, name, fileName));
    node.Parent?.SortChildren();
  }

  private void OnWatcherCreatedEvent(object sender, FileSystemEventArgs args)
  {
    string directoryName = Path.GetDirectoryName(args.FullPath);
    if (directoryName == null)
      return;
    FileTree.Node node1 = this.Find(directoryName);
    if (node1 == null)
      return;
    FileTree.Node node2 = new FileTree.Node(args.FullPath);
    node1.AddChild(node2);
    this.BuildBranches(node2);
    this.PushEvent((FileTree.IQueuedEvent) new FileTree.CreatedEvent(node2));
  }

  private void OnWatcherChangedEvent(object sender, FileSystemEventArgs args)
  {
    FileTree.Node node = this.Find(args.FullPath);
    if (node == null)
      return;
    this.PushEvent((FileTree.IQueuedEvent) new FileTree.ChangedEvent(node));
  }

  private void OnWatcherDeletedEvent(object sender, FileSystemEventArgs args)
  {
    FileTree.Node node = this.Find(args.FullPath);
    if (node == null || node.Parent == null)
      return;
    this.PushEvent((FileTree.IQueuedEvent) new FileTree.DeletedEvent(node));
    node.Parent.RemoveChild(node);
  }

  public FileStream CreateFile(string relativePath)
  {
    using (new FileTree.BlockWatcherEventsScope(this._watcher))
    {
      string fullPath = Path.Combine(this.Root.FullPath, relativePath);
      string directoryName = Path.GetDirectoryName(fullPath);
      if (!Directory.Exists(directoryName))
        Directory.CreateDirectory(directoryName);
      using (FileStream file = new FileStream(fullPath, (FileMode) 2))
      {
        createNodeRecursive(fullPath);
        return file;
      }
    }

    FileTree.Node createNodeRecursive(string fullPath)
    {
      string directoryName = Path.GetDirectoryName(fullPath);
      FileTree.Node node1 = this.Find(directoryName) ?? createNodeRecursive(directoryName);
      FileTree.Node node2 = new FileTree.Node(fullPath);
      node1.AddChild(node2);
      this.BuildBranches(node2);
      this.PushEvent((FileTree.IQueuedEvent) new FileTree.CreatedEvent(node2));
      return node2;
    }
  }

  public FileTree.RootNode Root => this._rootNode;

  public bool IsValid => this._rootNode != null && Path.Exists(this._rootNode.FullPath);

  public FileTree.Node Find(string path, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
  {
    if (!this.IsValid)
      return (FileTree.Node) null;
    path = Path.TrimEndingDirectorySeparator(path);
    string str;
    for (path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar); path.StartsWith(Path.DirectorySeparatorChar); path = str.Substring(1, str.Length - 1))
      str = path;
    if (Path.IsPathFullyQualified(path))
    {
      string fullPath = this._rootNode.FullPath;
      if (!path.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
        return (FileTree.Node) null;
      path = Path.GetRelativePath(fullPath, path);
    }
    return path == "." ? (FileTree.Node) this._rootNode : this._rootNode.Find(path, stringComparison);
  }

  public void GetNodes(List<FileTree.Node> destination, FileTree.Filter filter = FileTree.Filter.All, int maxDepth = 0)
  {
    this._rootNode.GetNodes(destination, filter, maxDepth);
  }

  public void Dispose()
  {
    if (this._watcher != null)
    {
      this._watcher.Dispose();
      this._watcher = (FileSystemWatcher) null;
    }
    if (this._rootNode == null)
      return;
    FileTree.DeleteNode((FileTree.Node) this._rootNode);
  }

  public void Update(float deltaTime) => this.PollEvents();

  [Flags]
  public enum Filter
  {
    Directories = 1,
    Files = 2,
    ExpandedOnly = 4,
    SelectedOnly = 8,
    All = ExpandedOnly | Files | Directories, // 0x00000007
  }

  public class Node : IFlatListItem, IFlatListExpandable
  {
    private readonly List<FileTree.Node> _children;

    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private LoadedTexture Texture { get; set; }

    public virtual string FullPath
    {
      get => this.Parent != null ? Path.Combine(this.Parent.FullPath, this.Name) : this.Name;
    }

    public virtual string RelativePath
    {
      get
      {
        return this.Parent != null && !this.Parent.IsRoot ? Path.Combine(this.Parent.RelativePath, this.Name) : this.Name;
      }
    }

    public string DirectoryPath => Path.GetDirectoryName(this.FullPath);

    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string Name { get; private set; }

    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public FileTree.Node Parent { get; private set; }

    public int ChildCount => this._children.Count;

    public bool IsEmpty => this._children.Count == 0;

    public int ChildDirectoryCount
    {
      get
      {
        int childDirectoryCount = 0;
        for (int index = 0; index < this._children.Count; ++index)
        {
          if (this._children[index].IsDirectory)
            ++childDirectoryCount;
        }
        return childDirectoryCount;
      }
    }

    public IReadOnlyCollection<FileTree.Node> Children
    {
      get => (IReadOnlyCollection<FileTree.Node>) this._children;
    }

    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private FileTree.Node.NodeFlags Flags { get; set; }

    private bool HasFlag(FileTree.Node.NodeFlags flag) => this.Flags.HasFlag((Enum) flag);

    private bool SetFlag(FileTree.Node.NodeFlags flag)
    {
      FileTree.Node.NodeFlags flags = this.Flags;
      this.Flags |= flag;
      return this.Flags != flags;
    }

    private bool ClearFlag(FileTree.Node.NodeFlags flag)
    {
      FileTree.Node.NodeFlags flags = this.Flags;
      this.Flags &= ~flag;
      return this.Flags != flags;
    }

    public bool IsDirectory
    {
      get => this.Flags.HasFlag((Enum) FileTree.Node.NodeFlags.IsDirectory);
      private set
      {
        if (value)
          this.Flags |= FileTree.Node.NodeFlags.IsDirectory;
        else
          this.Flags &= ~FileTree.Node.NodeFlags.IsDirectory;
      }
    }

    private bool IsDirty
    {
      get => this.HasFlag(FileTree.Node.NodeFlags.IsDirty);
      set => this.SetFlag(FileTree.Node.NodeFlags.IsDirty);
    }

    public bool IsValid => Path.IsPathRooted(this.FullPath);

    public bool IsExpanded
    {
      get => this.HasFlag(FileTree.Node.NodeFlags.IsExpanded);
      set
      {
        if (value)
          this.SetFlag(FileTree.Node.NodeFlags.IsExpanded);
        else
          this.ClearFlag(FileTree.Node.NodeFlags.IsExpanded);
        this.SetFlag(FileTree.Node.NodeFlags.IsDirty);
      }
    }

    public bool IsSelected
    {
      get => this.HasFlag(FileTree.Node.NodeFlags.IsSelected);
      set
      {
        if (value)
          this.SetFlag(FileTree.Node.NodeFlags.IsSelected);
        else
          this.ClearFlag(FileTree.Node.NodeFlags.IsSelected);
        this.SetFlag(FileTree.Node.NodeFlags.IsDirty);
      }
    }

    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public 
    
    object? Context { get; set; } = (object) null;

    public int Depth
    {
      get
      {
        int depth = 0;
        for (FileTree.Node parent = this.Parent; parent != null; parent = parent.Parent)
          ++depth;
        return depth;
      }
    }

    public virtual bool IsRoot => false;

    public Node(
    
    string fullPath)
    {
      this.Name = Path.GetFileName(fullPath);
      this.IsDirectory = Directory.Exists(fullPath);
      this.IsExpanded = this.IsDirectory;
      this._children = new List<FileTree.Node>();
      this.Parent = (FileTree.Node) null;
    }

    internal void AddChild(FileTree.Node node)
    {
      if (node.Parent != null)
        throw new Exception("Node already has a parent set!");
      this._children.Add(node);
      node.Parent = this;
    }

    internal void SortChildren()
    {
      this._children.Sort((IComparer<FileTree.Node>) new FileTree.Node.NodeComparer());
    }

    internal void RemoveChild(FileTree.Node child)
    {
      if (child.Parent != this)
        throw new Exception("Trying to remove a child from an unrelated parent!");
      this._children.Remove(child);
      child.Parent = (FileTree.Node) null;
    }

    internal void Rename(string newName) => this.Name = newName;

    public override string ToString() => this.Name;

    public FileTree.Node Find(string path, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
    {
      int length = path.IndexOf(Path.DirectorySeparatorChar);
      if (length == -1)
      {
        foreach (FileTree.Node child in this._children)
        {
          if (string.Compare(path, child.Name, stringComparison) == 0)
            return child;
        }
        return (FileTree.Node) null;
      }
      string strB = path.Substring(0, length);
      foreach (FileTree.Node child in this._children)
      {
        if (string.Compare(child.Name, strB, stringComparison) == 0)
        {
          string str = path;
          int startIndex = length + 1;
          string path1 = str.Substring(startIndex, str.Length - startIndex);
          return child.Find(path1, stringComparison);
        }
      }
      return (FileTree.Node) null;
    }

    public void FindChildren(
      string name,
      List<FileTree.Node> destination,
      StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
    {
      foreach (FileTree.Node child in (IEnumerable<FileTree.Node>) this.Children)
      {
        if (string.Compare(child.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
          destination.Add(child);
      }
    }

    public bool Visible => true;

    public void Recompose(ICoreClientAPI capi)
    {
      this.Texture?.Dispose();
      CairoFont cairoFont = CairoFont.WhiteDetailText();
      if (this.IsSelected)
        ((FontConfig) cairoFont).Color = GuiStyle.ActiveButtonTextColor;
      this.Texture = new TextTextureUtil(capi).GenTextTexture(this.Name, cairoFont, (TextBackground) null);
    }

    public void RenderListEntryTo(
      ICoreClientAPI capi,
      float dt,
      double x,
      double y,
      double cellWidth,
      double cellHeight)
    {
      if (this.Texture == null || this.IsDirty)
      {
        this.Recompose(capi);
        this.IsDirty = false;
      }
      double num1 = (cellHeight - (double) this.Texture.Height) / 4.0;
      double num2 = GuiElement.scaled(5.0);
      capi.Render.Render2DTexturePremultipliedAlpha(this.Texture.TextureId, x + num2, y + num1, (double) this.Texture.Width, (double) this.Texture.Height, 50f, (Vec4f) null);
    }

    public void Dispose()
    {
      if (this.Texture == null)
        return;
      this.Texture.Dispose();
      this.Texture = (LoadedTexture) null;
    }

    internal void Delete()
    {
      this.Parent?.RemoveChild(this);
      FileTree.Node[] array = new FileTree.Node[this._children.Count];
      this._children.CopyTo(array);
      foreach (FileTree.Node node in array)
        node.Delete();
      this.Dispose();
    }

    public void GetNodes(List<FileTree.Node> destination, FileTree.Filter filter = FileTree.Filter.All, int maxDepth = -1)
    {
      bool includeDirectory = filter.HasFlag((Enum) FileTree.Filter.Directories);
      bool includeFiles = filter.HasFlag((Enum) FileTree.Filter.Files);
      bool includeExpandedOnly = filter.HasFlag((Enum) FileTree.Filter.ExpandedOnly);
      bool includeSelectedOnly = filter.HasFlag((Enum) FileTree.Filter.SelectedOnly);
      int depth = 0;
      appendNode(this, destination);

      void appendNode(FileTree.Node node, List<FileTree.Node> destination)
      {
        bool flag = !includeSelectedOnly || node.IsSelected;
        if (node.IsDirectory)
        {
          if (includeDirectory & flag)
            destination.Add(node);
          if (includeExpandedOnly && !node.IsExpanded || maxDepth > 0 && ++depth > maxDepth)
            return;
          foreach (FileTree.Node child in (IEnumerable<FileTree.Node>) node.Children)
            appendNode(child, destination);
        }
        else
        {
          if (!(includeFiles & flag))
            return;
          destination.Add(node);
        }
      }
    }

    [Flags]
    private enum NodeFlags : byte
    {
      IsDirectory = 1,
      IsExpanded = 2,
      IsDirty = 4,
      IsSelected = 8,
    }

    [StructLayout(LayoutKind.Sequential, Size = 1)]
    private struct NodeComparer : IComparer<FileTree.Node>
    {
      public readonly int Compare(FileTree.Node x, FileTree.Node y)
      {
        if (x.IsDirectory && !y.IsDirectory)
          return -1;
        return !x.IsDirectory && y.IsDirectory ? 1 : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
      }
    }
  }

  public class RootNode(FileTree tree, string fullPath) : FileTree.Node(fullPath)
  {
    private readonly FileTree _tree = tree;
    private readonly string _rootPath = Path.GetDirectoryName(fullPath);

    public override string FullPath => Path.Combine(this._rootPath, this.Name);

    public override bool IsRoot => true;

    public FileTree Tree => this._tree;
  }

  protected readonly struct BlockWatcherEventsScope : IDisposable
  {
    private readonly FileSystemWatcher _watcher;
    private readonly bool _previousState;

    public BlockWatcherEventsScope(FileSystemWatcher watcher)
    {
      this._watcher = watcher;
      this._previousState = watcher.EnableRaisingEvents;
      this._watcher.EnableRaisingEvents = false;
    }

    public void Dispose() => this._watcher.EnableRaisingEvents = this._previousState;
  }

  protected interface IQueuedEvent
  {
    void Invoke(FileTree owner);
  }

  protected readonly struct RenamedEvent(FileTree.Node node, string oldName, string newName) : 
    FileTree.IQueuedEvent
  {
    private readonly FileTree.Node _node = node;
    private readonly string _oldName = oldName;
    private readonly string _newName = newName;

    public void Invoke(FileTree owner)
    {
      FileTree.NodeRename nodeRenamed = owner.NodeRenamed;
      if (nodeRenamed == null)
        return;
      nodeRenamed(this._node, this._oldName, this._newName);
    }
  }

  protected readonly struct ChangedEvent(FileTree.Node node) : FileTree.IQueuedEvent
  {
    private readonly FileTree.Node _node = node;

    public void Invoke(FileTree owner)
    {
      FileTree.NodeChange nodeChanged = owner.NodeChanged;
      if (nodeChanged == null)
        return;
      nodeChanged(this._node);
    }
  }

  protected readonly struct CreatedEvent(FileTree.Node node) : FileTree.IQueuedEvent
  {
    private readonly FileTree.Node _node = node;

    public void Invoke(FileTree owner)
    {
      FileTree.NodeChange nodeCreated = owner.NodeCreated;
      if (nodeCreated == null)
        return;
      nodeCreated(this._node);
    }
  }

  protected readonly struct DeletedEvent(FileTree.Node node) : FileTree.IQueuedEvent
  {
    private readonly FileTree.Node _node = node;

    public void Invoke(FileTree owner)
    {
      FileTree.NodeChange nodeDeleted = owner.NodeDeleted;
      if (nodeDeleted == null)
        return;
      nodeDeleted(this._node);
    }
  }

  public delegate void NodeChange(FileTree.Node node);

  public delegate void NodeRename(FileTree.Node node, string oldName, string newName);
}
