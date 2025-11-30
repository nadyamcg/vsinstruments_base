using VSInstrumentsBase.src.Files;
using VSInstrumentsBase.src.Playback;
using VSInstrumentsBase.src.Players;
using VSInstrumentsBase.src.Types;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using VSInstrumentsBase.src.Core;


namespace VSInstrumentsBase.src.GUI;

public class SongSelectGUI : GuiDialog
{
  private readonly FileTree _fileTree;
  private readonly List<FileTree.Node> _treeNodes = (List<FileTree.Node>) null;
  private FileTree.Node _treeSelection = (FileTree.Node) null;
  private readonly List<FileTree.Node> _contentNodes = (List<FileTree.Node>) null;
  private FileTree.Node _contentSelection = (FileTree.Node) null;
  private string _textFilter;
  private MidiPlayerBase _previewMusicPlayer;
  private readonly InstrumentType _instrumentType;
  private int _activeTrack = -1;
  private readonly Action<string, string> _fileSelectionCallback;
  private bool _initializationFailed = false;

  public override string ToggleKeyCombinationCode => null;

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  private event Action<string> BandNameChange;

    private GuiElementFlatList TreeList
  {
    get => this.SingleComposer.GetFlatList("treeList");
  }

    private GuiElementScrollbar TreeScrollBar
  {
    get => this.SingleComposer.GetScrollbar("treeScrollBar");
  }

    private GuiElementFlatList ContentList
  {
    get => this.SingleComposer.GetFlatList("contentList");
  }

    private GuiElementScrollbar ContentListScrollBar
  {
    get => this.SingleComposer.GetScrollbar("contentScrollBar");
  }

    private GuiElementDynamicText LocationText
  {
    get => this.SingleComposer.GetDynamicText("locationText");
  }

    private GuiElementTextInput SearchTextInput
  {
    get => this.SingleComposer.GetTextInput("searchTextInput");
  }

    private GuiElementRichtext DetailsText
  {
    get => this.SingleComposer.GetRichtext("detailsText");
  }

    private GuiElementScrollbar DetailsScrollBar
  {
    get => this.SingleComposer.GetScrollbar("detailsScrollBar");
  }

  public SongSelectGUI(
    ICoreClientAPI capi,
    InstrumentType instrumentType,
    Action<string> bandChange = null,
    string bandName = "",
    string title = "Song Selection",
    Action<string, string> onFileSelect = null)
    : base(capi)
  {
    ((ICoreAPI) capi).Logger.Notification("[SongSelectGUI] Constructor entered");
    ((ICoreAPI) capi).Logger.Notification("[SongSelectGUI] About to call GetInstrumentMod()");
    InstrumentModClient instrumentMod = capi.GetInstrumentMod();
    if (instrumentMod == null)
    {
      ((ICoreAPI) capi).Logger.Error("[SongSelectGUI] CRITICAL: InstrumentMod is null! Mod system not loaded properly. Cannot open GUI.");
      _initializationFailed = true;
      return;
    }
    ((ICoreAPI) capi).Logger.Debug("[SongSelectGUI] InstrumentMod found: " + ((object) instrumentMod).GetType().Name);
    if (instrumentMod.FileManager == null)
    {
      ((ICoreAPI) capi).Logger.Error("[SongSelectGUI] CRITICAL: FileManager is null! Cannot open GUI.");
      _initializationFailed = true;
      return;
    }
    this._fileTree = instrumentMod.FileManager.UserTree;
    this._fileSelectionCallback = onFileSelect;
    this._fileTree.NodeChanged += new FileTree.NodeChange(this.OnNodeChanged);
    this._treeNodes = new List<FileTree.Node>();
    this._contentNodes = new List<FileTree.Node>();
    this._previewMusicPlayer = (MidiPlayerBase) new MidiPlayer((ICoreAPI) capi, (IPlayer) capi.World.Player, instrumentType);
    this._instrumentType = instrumentType;
    this.BandNameChange = bandChange;
    this.SetupDialog(title, bandName);
    this.SetupSelection();
  }

  protected void OnNodeChanged(FileTree.Node node)
  {
    if (node != null && node.Context is MidiFileInfo)
      node.Context = (object) new MidiFileInfo(node.FullPath);
    this.RefreshContent(refreshContent: true, refreshDetails: true);
  }

  private void SetupSelection()
  {
    List<FileTree.Node> destination = new List<FileTree.Node>();
    this._fileTree.GetNodes(destination, FileTree.Filter.Directories | FileTree.Filter.Files | FileTree.Filter.SelectedOnly);
    if (destination.Count > 0)
    {
      FileTree.Node node1 = (FileTree.Node) null;
      FileTree.Node node2 = (FileTree.Node) null;
      foreach (FileTree.Node node3 in destination)
      {
        if (node3.IsValid)
        {
          if (node3.IsDirectory)
            node2 = node3;
          else
            node1 = node3;
        }
      }
      this.SelectTreeNode(node2);
      this.SelectContentNode(node1);
    }
    else
      this.SelectTreeNode((FileTree.Node) this._fileTree.Root);
  }

  public virtual void OnGuiOpened()
  {
    if (_initializationFailed)
      return;
    base.OnGuiOpened();
    this.RefreshContent(refreshContent: true, refreshDetails: true);
    ((ICoreAPI) this.capi).Logger.Notification("[SongSelectGUI] GUI opened, refreshed file list");
  }

  public virtual void OnGuiClosed()
  {
    if (_initializationFailed)
      return;
    if (this._previewMusicPlayer != null)
    {
      if (this._previewMusicPlayer.IsPlaying || this._previewMusicPlayer.IsFinished)
        this._previewMusicPlayer.Stop();
      this._previewMusicPlayer.Dispose();
      this._previewMusicPlayer = (MidiPlayerBase) null;
    }
    this._fileTree.NodeChanged -= new FileTree.NodeChange(this.OnNodeChanged);
    base.OnGuiClosed();
  }

  private void SetupDialog(string title, string bandName)
  {
    double elementToDialogPadding = GuiStyle.ElementToDialogPadding;
    ElementBounds elementBounds1 = ElementStdBounds.AutosizedMainDialog.WithAlignment((EnumDialogArea) 6);
    ElementBounds elementBounds2 = ElementBounds.Fixed(0.5 * elementToDialogPadding, elementToDialogPadding + 8.0, 750.0 + 4.0 * elementToDialogPadding, 32.0);
    ElementBounds elementBounds3 = ElementBounds.Fixed(0.0, elementToDialogPadding + 4.0, 300.0, 32.0).FixedRightOf(elementBounds2, 0.0);
    ElementBounds elementBounds4 = ElementBounds.Fixed(elementToDialogPadding, elementToDialogPadding + 40.0 + 10.0, 500.0, 600.0);
    ElementBounds elementBounds5 = ElementBounds.Fixed(-0.5 * elementToDialogPadding, 0.0, 250.0, 600.0).WithFixedOffset(elementBounds4.fixedX, elementBounds4.fixedY);
    ElementBounds bounds1 = ElementBounds.Fixed(elementBounds5.fixedX + elementBounds5.fixedWidth, elementBounds5.fixedY, 20.0, elementBounds5.fixedHeight);
    ElementBounds contentBound1 = ElementBounds.Fixed(bounds1.fixedX + bounds1.fixedWidth + elementToDialogPadding, elementBounds4.fixedY, 500.0, 600.0);
    ElementBounds bounds2 = ElementBounds.Fixed(contentBound1.fixedX + contentBound1.fixedWidth, contentBound1.fixedY, 20.0, contentBound1.fixedHeight);
    ElementBounds contentBound2 = ElementBounds.Fixed(bounds2.fixedX + bounds2.fixedWidth + elementToDialogPadding, elementBounds4.fixedY, 300.0, 600.0);
    ElementBounds bounds3 = ElementBounds.Fixed(contentBound2.fixedX + contentBound2.fixedWidth + elementToDialogPadding, contentBound2.fixedY, 20.0, contentBound2.fixedHeight);
    ElementBounds elementBounds6 = ElementBounds.Fill.WithFixedPadding(elementToDialogPadding);
    elementBounds6.BothSizing = (ElementSizing) 2;
    elementBounds6.WithChildren(new ElementBounds[8]
    {
      elementBounds2,
      elementBounds3,
      elementBounds5,
      bounds1,
      contentBound1,
      bounds2,
      contentBound2,
      bounds3
    });
    var composer = this.capi.Gui.CreateCompo("FileExplorerDialog", elementBounds1)
        .AddShadedDialogBG(elementBounds6, true, 5.0, 0.75f)
        .AddDialogTitleBar(title, new Action(this.Close), (CairoFont) null, (ElementBounds) null, (string) null)
        .BeginChildElements(elementBounds6)
        .AddDynamicText(".", CairoFont.WhiteDetailText(), elementBounds2, "locationText")
        .AddTextInput(elementBounds3, new Action<string>(this.FilterContent), CairoFont.WhiteDetailText(), "searchTextInput")
        .BeginClip(elementBounds5.ForkBoundingParent(0.0, 0.0, 0.0, 0.0))
        .AddInset(elementBounds5, 3, 0.85f)
        .AddFlatListEx(elementBounds5, new Action<int>(this.OnTreeElementLeftClick), new Action<int>(this.OnTreeExpandLeftClick), Unsafe.As<List<IFlatListItem>>((object) this._treeNodes), "treeList")
        .EndClip()
        .AddVerticalScrollbarEx((Action<float>) (value => this.OnScrollBarValueChanged(value, this.TreeList)), bounds1, elementBounds5, "treeScrollBar")
        .BeginClip(contentBound1.ForkBoundingParent(0.0, 0.0, 0.0, 0.0))
        .AddInset(contentBound1, 3, 0.85f)
        .AddFlatListEx(contentBound1, new Action<int>(this.OnContentElementLeftClick), null, Unsafe.As<List<IFlatListItem>>((object) this._contentNodes), "contentList")
        .EndClip()
        .AddVerticalScrollbarEx((Action<float>) (value => this.OnScrollBarValueChanged(value, this.ContentList)), bounds2, contentBound1, "contentScrollBar")
        .BeginClip(contentBound2.ForkBoundingParent(0.0, 0.0, 0.0, 0.0))
        .AddRichtext(string.Empty, CairoFont.WhiteDetailText(), contentBound2.ForkChild(), "detailsText")
        .EndClip()
        .AddVerticalScrollbarEx((Action<float>) (value => this.OnScrollBarValueChanged(value, this.DetailsText)), bounds3, contentBound2, "detailsScrollBar")
        .EndChildElements();
    this.SingleComposer = composer.Compose(true);
    if (this.BandNameChange != null)
      this.UpdateBand(bandName);
    this.SelectTreeNode((FileTree.Node) this._fileTree.Root);
  }

  private void OnScrollBarValueChanged(float value, GuiElementFlatList list)
  {
    list.insideBounds.fixedY = -(double) value;
    list.insideBounds.CalcWorldBounds();
  }

  private void OnScrollBarValueChanged(float value, GuiElementRichtext list)
  {
    ((GuiElement) list).Bounds.fixedY = -(double) value;
    ((GuiElement) list).Bounds.CalcWorldBounds();
  }

  private void UpdateScrollBarSize(GuiElementFlatList list, GuiElementScrollbar scrollBar)
  {
    int count = list.Elements.Count;
    double num = GuiElement.scaled((double) (list.unscaledCellHeight + list.unscaledCellSpacing)) * (double) count;
    double fixedHeight = ((GuiElement) list).Bounds.fixedHeight;
    scrollBar.SetHeights((float) fixedHeight, (float) num);
  }

  private void UpdateScrollBarSize(GuiElementRichtext richText, GuiElementScrollbar scrollBar)
  {
    double fixedHeight = ((GuiElement) richText).Bounds.fixedHeight;
    scrollBar.SetHeights((float) ((GuiElement) richText).Bounds.ParentBounds.InnerHeight, (float) fixedHeight);
  }

  private void SelectTreeNode(FileTree.Node node)
  {
    if (this._treeSelection != null)
      this._treeSelection.IsSelected = false;
    if (node != null)
    {
      node.IsSelected = true;
      this._treeSelection = node;
    }
    this._textFilter = string.Empty;
    ((GuiElementEditableTextBase) this.SearchTextInput).SetValue(string.Empty, true);
    this.SearchTextInput.SetPlaceHolderText(node == null ? "Search..." : $"Search in {node.Name}...");
    this.SelectContentNode((FileTree.Node) null);
    this.RefreshContent(refreshContent: true, refreshDetails: true);
  }

  private void SelectContentNode(FileTree.Node node)
  {
    if (this._contentSelection != null)
    {
      this._contentSelection.IsSelected = false;
      this._contentSelection = (FileTree.Node) null;
    }
    this._contentSelection = node;
    if (this._contentSelection != null)
      this._contentSelection.IsSelected = true;
    this.RefreshContent(false, refreshDetails: true);
  }

  protected void RefreshContent(bool refreshTree = true, bool refreshContent = false, bool refreshDetails = false)
  {
    if (refreshTree)
    {
      this._treeNodes.Clear();
      this._fileTree.GetNodes(this._treeNodes, FileTree.Filter.Directories | FileTree.Filter.ExpandedOnly);
      this.LocationText.SetNewText(this._treeSelection != null ? this._treeSelection.FullPath : "-", false, false, false);
      this.UpdateScrollBarSize(this.TreeList, this.TreeScrollBar);
    }
    if (refreshContent)
    {
      bool flag = !string.IsNullOrEmpty(this._textFilter);
      this._contentNodes.Clear();
      FileTree.Node treeSelection = this._treeSelection;
      treeSelection?.GetNodes(this._contentNodes, FileTree.Filter.Files, flag ? 0 : 1);
      if (flag)
      {
        int index = 0;
        while (index < this._contentNodes.Count)
        {
          if (this._contentNodes[index].Name.Contains(this._textFilter, StringComparison.OrdinalIgnoreCase))
            ++index;
          else
            this._contentNodes.RemoveAt(index);
        }
      }
      int count = this._contentNodes.Count;
      this._contentNodes.RemoveAll((Predicate<FileTree.Node>) (node =>
      {
        string lowerInvariant = Path.GetExtension(node.Name).ToLowerInvariant();
        return lowerInvariant != ".mid" && lowerInvariant != ".midi";
      }));
      ((ICoreAPI) this.capi).Logger.Notification($"[SongSelectGUI] Found {this._contentNodes.Count} MIDI files (filtered from {count} total files) in: {treeSelection?.FullPath ?? "null"}");
      this.UpdateScrollBarSize(this.ContentList, this.ContentListScrollBar);
    }
    if (!refreshDetails)
      return;
    this.RefreshDetails();
    this.SetPreviewTrack((MidiFile) null);
  }

  protected void RefreshDetails()
  {
    FileTree.Node contentSelection = this._contentSelection;
    if (contentSelection == null)
    {
      this.DetailsText.SetNewText(Array.Empty<RichTextComponentBase>());
    }
    else
    {
      this.DetailsText.SetNewText(this.BuildDetails(contentSelection));
      this.UpdateScrollBarSize(this.DetailsText, this.DetailsScrollBar);
    }
  }

  private void OnTreeElementLeftClick(int index) => this.SelectTreeNode(this._treeNodes[index]);

  private void OnTreeExpandLeftClick(int index)
  {
    FileTree.Node treeNode = this._treeNodes[index];
    treeNode.IsExpanded = !treeNode.IsExpanded;
    this.RefreshContent();
  }

  private void OnContentElementLeftClick(int index)
  {
    this.SelectContentNode(this._contentNodes[index]);
  }

  private void FilterContent(string textFilter)
  {
    this._textFilter = textFilter;
    this.RefreshContent(false, true, true);
  }

  private RichTextComponentBase[] BuildDetails(FileTree.Node node)
  {
    MidiFileInfo midiFileInfo1;
    if (node.Context is MidiFileInfo context)
    {
      midiFileInfo1 = context;
    }
    else
    {
      MidiFileInfo midiFileInfo2 = new MidiFileInfo(node.FullPath);
      node.Context = (object) midiFileInfo2;
      midiFileInfo1 = midiFileInfo2;
    }
    CairoFont leftFont = CairoFont.WhiteDetailText().WithOrientation((EnumTextOrientation) 0);
    CairoFont rightFont = CairoFont.WhiteDetailText().WithOrientation((EnumTextOrientation) 1);
    List<RichTextComponentBase> components = new List<RichTextComponentBase>();
    addComponent("Name:", node.Name);
    addPathComponent("Path:", node.DirectoryPath);
    addComponent("Size:", $"{midiFileInfo1.SizeKB:0.00} kB");
    addComponent("Created:", $"{((FileSystemInfo) midiFileInfo1.FileInfo).CreationTime}");
    addComponent("Extension:", ((FileSystemInfo) midiFileInfo1.FileInfo).Extension ?? "");
    if (!midiFileInfo1.IsMidi)
      return components.ToArray();
    for (int index = 0; index < midiFileInfo1.TracksCount; ++index)
    {
      MidiTrackInfo track = midiFileInfo1.Tracks[index];
      addSingleComponent(string.Empty);
      addSingleComponent($"Track #{track.Index:00}:");
      addComponent("Duration:", durationToString(track.Duration) ?? "");
      addComponent("Notes:", $"{track.NoteCount}");
      if (track.NoteCount != 0)
        addPlaybackComponent(midiFileInfo1.GetMidiFile(), track.Index);
    }
    return components.ToArray();

    static string trimContent(string content, int maxLength = 33)
    {
      if (content.Length <= maxLength)
        return content;
      content = content.Substring(0, maxLength - 3);
      return content + "...";
    }

    static string durationToString(double seconds)
    {
      TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
      return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }

    void addComponent(string title, string content)
    {
      components.Add((RichTextComponentBase) new RichTextComponent(this.capi, title, leftFont));
      if (content.Length > 33)
      {
        content = content.Substring(0, 30);
        content += "...";
      }
      if (!content.EndsWith(Environment.NewLine))
        content += Environment.NewLine;
      components.Add((RichTextComponentBase) new RichTextComponent(this.capi, content, rightFont));
    }

    void addSingleComponent(string title)
    {
      if (!title.EndsWith(Environment.NewLine))
        title += Environment.NewLine;
      components.Add((RichTextComponentBase) new RichTextComponent(this.capi, title, leftFont));
    }

    void addPlaybackComponent(MidiFile midi, int trackIndex)
    {
      if (trackIndex == this._activeTrack)
        components.Add((RichTextComponentBase) new LinkTextComponent(this.capi, "Stop", leftFont, (Action<LinkTextComponent>) (txc =>
        {
          this.capi.Gui.PlaySound("menubutton_press", false, 1f);
          this.SetPreviewTrack((MidiFile) null);
        })));
      else
        components.Add((RichTextComponentBase) new LinkTextComponent(this.capi, "Preview", leftFont, (Action<LinkTextComponent>) (txc =>
        {
          this.capi.Gui.PlaySound("menubutton_press", false, 1f);
          ((ICoreAPI) this.capi).Logger.Notification($"[SongSelectGUI] Preview button clicked: {midi.Chunks.Count} chunks, track {trackIndex}");
          this.SetPreviewTrack(midi, trackIndex);
        })));
      if (this._fileSelectionCallback != null)
        components.Add((RichTextComponentBase) new LinkTextComponent(this.capi, "Select", rightFont, (Action<LinkTextComponent>) (txc =>
        {
          this.capi.Gui.PlaySound("menubutton_press", false, 1f);
          ((ICoreAPI) this.capi).Logger.Notification($"[SongSelectGUI] Select button clicked: {node.Name} at {node.RelativePath}");
          this._fileSelectionCallback(node.RelativePath, node.Name);
          this.TryClose();
        })));
      else
        components.Add((RichTextComponentBase) new LinkTextComponent(this.capi, "Play", rightFont, (Action<LinkTextComponent>) (txc =>
        {
          ((ICoreAPI) this.capi).Logger.Notification($"[SongSelectGUI] Play button clicked: {node.Name}, track {trackIndex}, instrument {this._instrumentType?.Name ?? "unknown"}");
          ((PlaybackManagerClient)this.capi.GetInstrumentMod().PlaybackManager).RequestStartPlayback(node.RelativePath, trackIndex, this._instrumentType);
          this.TryClose();
        })));
      components.Add((RichTextComponentBase) new RichTextComponent(this.capi, Environment.NewLine, leftFont));
    }

    void addPathComponent(string title, string path)
    {
      components.Add((RichTextComponentBase) new RichTextComponent(this.capi, title, leftFont));
      components.Add((RichTextComponentBase) new LinkTextComponent(this.capi, trimContent(path), rightFont, (Action<LinkTextComponent>) (txc =>
      {
        this.capi.Gui.PlaySound("menubutton_press", false, 1f);
        Process.Start(new ProcessStartInfo()
        {
          FileName = path,
          UseShellExecute = true,
          Verb = "open"
        });
      })));
      components.Add((RichTextComponentBase) new RichTextComponent(this.capi, Environment.NewLine, leftFont));
    }
  }

  private void SetPreviewTrack(MidiFile midi, int track = 0, bool seekToStart = true)
  {
    if (midi == null)
    {
      safeStop();
    }
    else
    {
      try
      {
        safeStop();
        this._previewMusicPlayer.Play(midi, track);
        if (seekToStart)
        {
          double time = midi.ReadFirstNoteInSeconds(track);
          if (time > 0.0)
            this._previewMusicPlayer.Seek(time);
        }
        this._activeTrack = track;
      }
      catch
      {
        safeStop();
      }
      this.RefreshDetails();
    }

    void safeStop()
    {
      if (this._previewMusicPlayer != null && this._previewMusicPlayer.IsPlaying)
        this._previewMusicPlayer.Stop();
      this._activeTrack = -1;
      this.RefreshDetails();
    }
  }

  public void UpdateBand(string bandName)
  {
    string str = !(bandName != "") ? "No Band" : $"Band Name: \n\"{bandName}\"";
    GuiElementDynamicTextHelper.GetDynamicText(this.SingleComposer, "Band name").SetNewText(str, false, false, false);
    this.BandNameChange(bandName);
  }

  public virtual void OnBeforeRenderFrame3D(float deltaTime)
  {
    if (_initializationFailed)
      return;
    if (this._previewMusicPlayer != null && this._previewMusicPlayer.IsPlaying)
    {
      this._previewMusicPlayer.Update(deltaTime);
      if (this._previewMusicPlayer.IsFinished)
        this.SetPreviewTrack((MidiFile) null);
    }
    base.OnBeforeRenderFrame3D(deltaTime);
  }

  private void Close() => this.TryClose();

  [StructLayout(LayoutKind.Sequential, Size = 1)]
  private struct Keys
  {
    public const string TreeList = "treeList";
    public const string TreeScrollBar = "treeScrollBar";
    public const string ContentList = "contentList";
    public const string ContentScrollBar = "contentScrollBar";
    public const string LocationText = "locationText";
    public const string SearchTextInput = "searchTextInput";
    public const string DetailsText = "detailsText";
    public const string DetailsScrollBar = "detailsScrollBar";
  }
}
