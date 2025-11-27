using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using MidiParser;
using Instruments.Core;
using Instruments.Files;
using Instruments.Players;
using Instruments.Types;

namespace Instruments.GUI
{
	public class SongSelectGUI : GuiDialog
	{
		public override string ToggleKeyCombinationCode => null;
		event Action<string> bandNameChange;

		// TODO@exocs:
		//  This structure may be kept outside of the song select GUI and cached,
		//  e.g. for as long as an instrument is held. There is no need to rebuild
		//  the tree repeatedly, especially since it updates on changes.
		// TODO@exocs:
		//  Moving it outside will also prevent nodes from collapsing or expanding
		//  when the GUI is re-opened.
		private FileTree _fileTree;


		// Summary:
		//     List of all nodes to be shown in the tree view, only displaying directories.
		private List<FileTree.Node> _treeNodes = null;
		//
		// Summary:
		//     Currently selected node in the tree view, i.e. the node determining the content view.
		private FileTree.Node _treeSelection = null;
		//
		// Summary:
		//     List of all nodes to be shown in the tree view, only displaying directories.
		private List<FileTree.Node> _contentNodes = null;
		//
		// Summary:
		//     Currently selected node in the content view, likely a node representing a file.
		private FileTree.Node _contentSelection = null;
		//
		// Summary:
		//     Stores the currently selected text filter.
		private string _textFilter;
		//
		// Summary:
		//     The music player responsible for playing local preview.
		private MidiPlayerBase _previewMusicPlayer;
		//
		// Summary:
		//     The instrument type the songs should be played with (if any).
		private InstrumentType _instrumentType;
		//
		// Summary:
		//     Currently active track by its index.
		private int _activeTrack = -1;
		//
		// Summary:
		//     Convenience wrapper for all text constants of individual Gui elements for this menu.
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

		private GuiElementFlatList TreeList
		{
			get
			{
				return SingleComposer.GetFlatList(Keys.TreeList);
			}
		}
		private GuiElementScrollbar TreeScrollBar
		{
			get
			{
				return SingleComposer.GetScrollbar(Keys.TreeScrollBar);
			}
		}

		private GuiElementFlatList ContentList
		{
			get
			{
				return SingleComposer.GetFlatList(Keys.ContentList);
			}
		}
		private GuiElementScrollbar ContentListScrollBar
		{
			get
			{
				return SingleComposer.GetScrollbar(Keys.ContentScrollBar);
			}
		}

		private GuiElementDynamicText LocationText
		{
			get
			{
				return SingleComposer.GetDynamicText(Keys.LocationText);
			}
		}

		private GuiElementTextInput SearchTextInput
		{
			get
			{
				return SingleComposer.GetTextInput(Keys.SearchTextInput);
			}
		}
		private GuiElementRichtext DetailsText
		{
			get
			{
				return SingleComposer.GetRichtext(Keys.DetailsText);
			}
		}

		private GuiElementScrollbar DetailsScrollBar
		{
			get
			{
				return SingleComposer.GetScrollbar(Keys.DetailsScrollBar);
			}
		}
		//
		// Summary:
		//     Create new song selection GUI with root path at the provided directory path.
		public SongSelectGUI(ICoreClientAPI capi, InstrumentType instrumentType, Action<string> bandChange = null, string bandName = "", string title = "Song Selection")
			: base(capi)
		{
			// Retrieve the client file tree from the mod system.
			_fileTree = capi.GetInstrumentMod().FileManager.UserTree;

			_fileTree.NodeChanged += OnNodeChanged;

			_treeNodes = new List<FileTree.Node>();
			_contentNodes = new List<FileTree.Node>();
			_previewMusicPlayer = new MidiPlayer(capi, capi.World.Player, instrumentType); // If no instrument is provided, try to use anything.
			_instrumentType = instrumentType;

			bandNameChange = bandChange;
			SetupDialog(title, bandName);
			SetupSelection();
		}
		//
		// Summary:
		//     Callback for node changes that updates the state of the GUI.
		protected void OnNodeChanged(FileTree.Node node)
		{
			if (node != null && node.Context is MidiFileInfo)
			{
				// Make sure to overwrite the previous file info, as the viewer
				// will never re-create it on demand. It will only reuse the existing
				// or create a brand new info for nodes that don't have any yet.
				node.Context = new MidiFileInfo(node.FullPath);
			}
			RefreshContent(true, true, true);
		}
		//
		// Summary:
		//     Determine and reset selection to previous state, if possible.
		private void SetupSelection()
		{
			// First determine whether the tree may already have some nodes selected. If yes, try to use
			// the latest available node of each type (directory for tree, file for content) as default.
			List<FileTree.Node> activeSelection = new List<FileTree.Node>();
			_fileTree.GetNodes(activeSelection, FileTree.Filter.SelectedOnly | FileTree.Filter.Directories | FileTree.Filter.Files);

			// There is some selection, so try to restore it:
			if (activeSelection.Count > 0)
			{
				FileTree.Node selectContentNode = null;
				FileTree.Node selectTreeNode = null;
				foreach (FileTree.Node node in activeSelection)
				{
					if (!node.IsValid)
						continue;

					if (node.IsDirectory)
					{
						selectTreeNode = node;
						continue;
					}

					selectContentNode = node;
				}

				// TODO@exocs:
				//   This call may come after the tree has changed. If the node still lingers and returns !IsValid,
				//   try to traverse the hierarchy and select the first IsValid parent hit, to make UX better.

				SelectTreeNode(selectTreeNode);
				SelectContentNode(selectContentNode);
			}
			else
			{
				// No selection, initialize as if we have selected the root node.
				SelectTreeNode(_fileTree.Root);
			}
		}
		//
		// Summary:
		//     Dispose of allocated resources once menu is closed.
		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
		}
		//
		// Summary:
		//     Dispose of allocated resources once menu is closed.
		public override void OnGuiClosed()
		{
			if (_previewMusicPlayer != null)
			{
				if (_previewMusicPlayer.IsPlaying || _previewMusicPlayer.IsFinished)
					_previewMusicPlayer.Stop();

				_previewMusicPlayer.Dispose();
				_previewMusicPlayer = null;
			}

			_fileTree.NodeChanged -= OnNodeChanged;

			base.OnGuiClosed();
		}
		//
		// Summary:
		//     Prepares and composes the dialog.
		private void SetupDialog(string title, string bandName)
		{
			// Constants
			double padding = GuiStyle.ElementToDialogPadding;
			const double topBarHeight = 40;
			const double leftPaneWidth = 250;
			const double rightPaneWidth = 500;
			const double paneHeight = 600;
			const double detailsPaneWidth = 300.0f;

			// Base dialog background
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
				.WithAlignment(EnumDialogArea.CenterMiddle);

			// Address may be long and so it will span the entire left column and 50% of the right column
			// The magical (+8) is just an effort to center it better, otherwise it looks really poorly.
			ElementBounds addressBarBounds = ElementBounds.Fixed(0.5 * padding, padding + 8, leftPaneWidth + rightPaneWidth + 4 * padding, 32);

			// Search bar will (hopefully) in most cases suffice with few characters or words, make it only half of the remaining width
			ElementBounds searchBarBounds = ElementBounds.Fixed(0, padding + 4, detailsPaneWidth, 32)
				.FixedRightOf(addressBarBounds, 0);

			// Content area below the navigation bar
			ElementBounds contentBounds = ElementBounds.Fixed(
				padding,
				padding + topBarHeight + 10,
				rightPaneWidth,
				paneHeight
			);

			// Tree view left pane
			// The magical (-50% padding) offset is just an effort to center the entire layout somewhat
			ElementBounds treePaneBounds = ElementBounds.Fixed(-0.5 * padding, 0, leftPaneWidth, paneHeight)
				.WithFixedOffset(contentBounds.fixedX, contentBounds.fixedY);

			// Tree view scrollbar
			ElementBounds treeScrollbarBounds = ElementBounds.Fixed(
				treePaneBounds.fixedX + treePaneBounds.fixedWidth,
				treePaneBounds.fixedY,
				20,
				treePaneBounds.fixedHeight
			);

			// Content view pane
			ElementBounds contentPaneBounds = ElementBounds.Fixed(
				treeScrollbarBounds.fixedX + treeScrollbarBounds.fixedWidth + padding,
				contentBounds.fixedY,
				rightPaneWidth,
				paneHeight
			);

			// Content view scrollbar
			ElementBounds contentScrollbarBounds = ElementBounds.Fixed(
				contentPaneBounds.fixedX + contentPaneBounds.fixedWidth,
				contentPaneBounds.fixedY,
				20,
				contentPaneBounds.fixedHeight
			);

			// Details panel
			ElementBounds detailsPaneBounds = ElementBounds.Fixed(
				contentScrollbarBounds.fixedX + contentScrollbarBounds.fixedWidth + padding, // place right of content scrollbar
				contentBounds.fixedY,
				detailsPaneWidth,
				paneHeight
			);

			// Details scrollbar
			ElementBounds detailsScrollbarBounds = ElementBounds.Fixed(
				detailsPaneBounds.fixedX + detailsPaneBounds.fixedWidth + padding,
				detailsPaneBounds.fixedY,
				20,
				detailsPaneBounds.fixedHeight
			);

			// Background
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(padding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren(
				addressBarBounds, searchBarBounds,
				treePaneBounds, treeScrollbarBounds,
				contentPaneBounds, contentScrollbarBounds,
				detailsPaneBounds, detailsScrollbarBounds
			);

			// Begin composing
			SingleComposer = capi.Gui.CreateCompo("FileExplorerDialog", dialogBounds)
					.AddShadedDialogBG(bgBounds)
					.AddDialogTitleBar(title, Close)
					.BeginChildElements(bgBounds)

						// Top bar with address text and search bar
						.AddDynamicText(".", CairoFont.WhiteDetailText(), addressBarBounds, Keys.LocationText)
						.AddTextInput(searchBarBounds, FilterContent, CairoFont.WhiteDetailText(), Keys.SearchTextInput)

						// Left panel, i.e. the tree view
						.BeginClip(treePaneBounds.ForkBoundingParent())
							.AddInset(treePaneBounds, 3)
							.AddFlatListEx(treePaneBounds, OnTreeElementLeftClick, OnTreeExpandLeftClick, Unsafe.As<List<IFlatListItem>>(_treeNodes), Keys.TreeList)
						.EndClip()
						.AddVerticalScrollbarEx((float value) =>
						{
							OnScrollBarValueChanged(value, TreeList);

						}, treeScrollbarBounds, treePaneBounds, Keys.TreeScrollBar)

						// Right pane, i.e. the content view
						.BeginClip(contentPaneBounds.ForkBoundingParent())
							.AddInset(contentPaneBounds, 3)
							.AddFlatList(contentPaneBounds, OnContentElementLeftClick, Unsafe.As<List<IFlatListItem>>(_contentNodes), Keys.ContentList)
						.EndClip()
						.AddVerticalScrollbarEx((value) =>
						{
							OnScrollBarValueChanged(value, ContentList);

						}, contentScrollbarBounds, contentPaneBounds, Keys.ContentScrollBar)

						.BeginClip(detailsPaneBounds.ForkBoundingParent())
							.AddRichtext(string.Empty, CairoFont.WhiteDetailText(), detailsPaneBounds.ForkChild(), Keys.DetailsText)
						.EndClip()
						.AddVerticalScrollbarEx((float value) =>
						{
							OnScrollBarValueChanged(value, DetailsText);
						}, detailsScrollbarBounds, detailsPaneBounds, Keys.DetailsScrollBar)

					.EndChildElements()
					.Compose();


			if (bandNameChange != null)
				UpdateBand(bandName);

			// Initialize as if we selected the root node, if any
			SelectTreeNode(_fileTree.Root);
		}
		//
		// Summary:
		//     Callback raised when the scroll bar value for provided list has changed.
		private void OnScrollBarValueChanged(float value, GuiElementFlatList list)
		{
			list.insideBounds.fixedY = -value;
			list.insideBounds.CalcWorldBounds();
		}
		//
		// Summary:
		//     Callback raised when the scroll bar value for provided list has changed.
		private void OnScrollBarValueChanged(float value, GuiElementRichtext list)
		{
			list.Bounds.fixedY = -value;
			list.Bounds.CalcWorldBounds();
		}
		//
		// Summary:
		//     Updates the scroll bar dimensions based on content in the provided list.
		private void UpdateScrollBarSize(GuiElementFlatList list, GuiElementScrollbar scrollBar)
		{
			int rowCount = list.Elements.Count;

			double rowHeight = GuiElement.scaled(list.unscaledCellHeight + list.unscaledCellSpacing);
			double scrollTotalHeight = rowHeight * rowCount;
			double scrollVisibleHeight = list.Bounds.fixedHeight;

			scrollBar.SetHeights((float)scrollVisibleHeight, (float)scrollTotalHeight);
		}
		//
		// Summary:
		//     Updates the scroll bar dimensions based on content in the provided richtext.
		private void UpdateScrollBarSize(GuiElementRichtext richText, GuiElementScrollbar scrollBar)
		{
			double totalHeight = richText.Bounds.fixedHeight;
			scrollBar.SetHeights((float)richText.Bounds.ParentBounds.InnerHeight, (float)totalHeight);
		}
		//
		// Summary:
		//     Selects the provided tree node in the tree view.
		//     Propagates changes to content selection as well!
		private void SelectTreeNode(FileTree.Node node)
		{
			// Clear the previous selection, if there is one, as we only want
			// to allow opening a single node from within the tree.
			if (_treeSelection != null)
			{
				_treeSelection.IsSelected = false;
			}

			// Update state of the new node and make sure to update
			// the location text, so the user is aware of what they are doing.
			if (node != null)
			{
				node.IsSelected = true;
				_treeSelection = node;
			}

			// When the user selects a tree node, their content is about
			// to be changed. Drop the.
			_textFilter = string.Empty;
			SearchTextInput.SetValue(string.Empty);
			SearchTextInput.SetPlaceHolderText(node == null ? "Search..." : $"Search in {node.Name}...");

			// On tree change the content needs to be cleared out
			SelectContentNode(null);

			// And make sure to apply refresh the content of the tree if necessary 
			RefreshContent(true, true, true);
		}
		//
		// Summary:
		//     Selects the provided tree node in the tree view.
		//     Propagates changes to content selection as well!
		private void SelectContentNode(FileTree.Node node)
		{
			// Make sure to clear and unselect the previous entry!
			if (_contentSelection != null)
			{
				_contentSelection.IsSelected = false;
				_contentSelection = null;
			}

			// Store the new selection and mark it as selected
			// before refreshing the details pane with information.
			_contentSelection = node;
			if (_contentSelection != null)
			{
				_contentSelection.IsSelected = true;
			}

			RefreshContent(false, false, true);
		}
		//
		// Summary:
		//     Refreshes all content state, e.g. the lists and the dimensions of their scroll bars.
		protected void RefreshContent(bool refreshTree = true, bool refreshContent = false, bool refreshDetails = false)
		{
			if (refreshTree)
			{
				_treeNodes.Clear();
				_fileTree.GetNodes(_treeNodes, FileTree.Filter.Directories | FileTree.Filter.ExpandedOnly);
				LocationText.SetNewText(_treeSelection != null ? _treeSelection.FullPath : "-");
				UpdateScrollBarSize(TreeList, TreeScrollBar);
			}

			// Refresh content view
			if (refreshContent)
			{
				// First determine if filter is applied - with filter applied we loosen the bound
				// of direct depth search only and search recursively instead.
				bool isFilterEnabled = !string.IsNullOrEmpty(_textFilter);

				// Clear the content and fetch all nodes from the selected tree item again:
				_contentNodes.Clear();
				FileTree.Node treeNode = _treeSelection;
				if (treeNode != null)
				{
					treeNode.GetNodes(
						_contentNodes,
						FileTree.Filter.Files,
						isFilterEnabled ? 0 : 1
						);
				}

				// After fetching the content, filter the actual nodes if the filter is enabled:
				if (isFilterEnabled)
				{
					for (int i = 0; i < _contentNodes.Count;)
					{
						FileTree.Node predicate = _contentNodes[i];
						if (predicate.Name.Contains(_textFilter, StringComparison.OrdinalIgnoreCase))
						{
							++i;
							continue;
						}

						// The predicate did not match, withdraw it from the node view.
						_contentNodes.RemoveAt(i);
					}
				}

				UpdateScrollBarSize(ContentList, ContentListScrollBar);
			}

			if (refreshDetails)
			{
				RefreshDetails();
				SetPreviewTrack(null);
			}
		}
		//
		// Summary:
		//     Refreshes only rich text details without applying any additional logic.
		protected void RefreshDetails()
		{
			FileTree.Node content = _contentSelection;
			if (content == null)
			{
				DetailsText.SetNewText(Array.Empty<RichTextComponentBase>());
				return;
			}
			else
			{
				RichTextComponentBase[] components = BuildDetails(content);
				DetailsText.SetNewText(components);
			}
			UpdateScrollBarSize(DetailsText, DetailsScrollBar);
		}
		//
		// Summary:
		//     Raised when an element in the tree view is clicked.
		private void OnTreeElementLeftClick(int index)
		{
			// TODO@exocs: This can be improved,
			// also check the Visible property and its impact on the flat list??
			FileTree.Node node = _treeNodes[index];
			SelectTreeNode(node);
		}
		//
		// Summary:
		//     Raised when an expand button is clicked for an element in the tree view.
		private void OnTreeExpandLeftClick(int index)
		{
			FileTree.Node node = _treeNodes[index];
			node.IsExpanded = !node.IsExpanded;

			RefreshContent(true, false);
		}
		//
		// Summary:
		//     Raised when an item is clicked on in the content view.
		private void OnContentElementLeftClick(int index)
		{
			SelectContentNode(_contentNodes[index]);
		}
		//
		// Summary:
		//     Applies the provided string as the text filter.
		private void FilterContent(string textFilter)
		{
			_textFilter = textFilter;
			RefreshContent(false, true, true);
		}
		//
		// Summary:
		//     Builds details string for the provided node.
		private RichTextComponentBase[] BuildDetails(FileTree.Node node)
		{
			// If an info is provided, use the existing one. The tree watcher
			// notifies us of changes done and the file info is always refreshed.
			// If it doesn't exist yet, new one can safely be created and stored
			// in the node, as done here:
			MidiFileInfo midiInfo;
			if (node.Context is MidiFileInfo existingMidiFileInfo)
			{
				midiInfo = existingMidiFileInfo;
			}
			else
			{
				MidiFileInfo newFileInfo = new MidiFileInfo(node.FullPath);
				node.Context = newFileInfo;
				midiInfo = newFileInfo;
			}

			// Define the fonts with their orientation either used on the left (title) or the right (content)
			// in the rich text widget.
			CairoFont leftFont = CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Left);
			CairoFont rightFont = CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Right);

			// Maximum length of characters of the content (right side).
			const int maximumContentLength = 33;

			// Trims content above the length and replaces the last symbols by '...'
			string trimContent(string content, int maxLength = maximumContentLength)
			{
				if (content.Length > maxLength)
				{
					content = content.Substring(0, maxLength - 3);
					return string.Concat(content, "...");
				}
				return content;
			}
			// Returns consistently formatted string for provided duration.
			string durationToString(double seconds)
			{
				// Round the duration to hh:mm:ss, the miliseconds are just chore for the eyes.
				TimeSpan duration = TimeSpan.FromSeconds(seconds);
				return string.Format(
					"{0:00}:{1:00}:{2:00}",//:{3:000}",
					duration.Hours, duration.Minutes,
					duration.Seconds//, MathF.Round(duration.Milliseconds)
					);
			}


			List<RichTextComponentBase> components = new List<RichTextComponentBase>();

			// Add new entry, title is the title on the left, content is the item on the right.
			void addComponent(string title, string content)
			{
				//var newcomponents = VtmlUtil.Richtextify(capi, left, leftFont);
				components.Add(new RichTextComponent(capi, title, leftFont));

				// Clamp the length to sensible values
				if (content.Length > maximumContentLength)
				{
					content = content.Substring(0, maximumContentLength - 3);
					content = string.Concat(content, "...");
				}

				// Ensure the line is terminated properly
				if (!content.EndsWith(Environment.NewLine))
				{
					content += Environment.NewLine;
				}

				components.Add(new RichTextComponent(capi, content, rightFont));
			}
			// Add new entry, no content, just a title on the left. 
			void addSingleComponent(string title)
			{
				if (!title.EndsWith(Environment.NewLine))
				{
					title += Environment.NewLine;
				}

				components.Add(new RichTextComponent(capi, title, leftFont));
			}
			// Add playback component for the provided file at specified track,
			// this crates the 'Preview' and 'Play' buttons.
			void addPlaybackComponent(MidiFile midi, int trackIndex)
			{
				// Is it playing?
				bool isPreviewing = trackIndex == _activeTrack;

				if (isPreviewing)
				{
					components.Add(new LinkTextComponent(capi, "Stop", leftFont, (txc) =>
					{
						capi.Gui.PlaySound("menubutton_press");
						SetPreviewTrack(null);
					}));
				}
				else
				{
					components.Add(new LinkTextComponent(capi, "Preview", leftFont, (txc) =>
					{
						capi.Gui.PlaySound("menubutton_press");
						SetPreviewTrack(midi, trackIndex);
					}));
				}

				components.Add(new LinkTextComponent(capi, "Play", rightFont, (txc) =>
				{
					// TODO@exocs: Move this out
					// Play callback
					capi.GetInstrumentMod().PlaybackManager.RequestStartPlayback(
						node.RelativePath,
						trackIndex,
						_instrumentType
						);
					TryClose();

				}));
				components.Add(new RichTextComponent(capi, Environment.NewLine, leftFont));
			}
			// Add new entry for file paths that are clickable (to be opened in explorer)
			void addPathComponent(string title, string path)
			{
				components.Add(new RichTextComponent(capi, title, leftFont));
				string displayText = trimContent(path);
				components.Add(new LinkTextComponent(capi, displayText, rightFont, (txc) =>
				{
					capi.Gui.PlaySound("menubutton_press");
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
					{
						FileName = path,
						UseShellExecute = true,
						Verb = "open"
					});
				}));

				components.Add(new RichTextComponent(capi, Environment.NewLine, leftFont));
			}

			// Start populating the list with individual components to display:
			addComponent(			"Name:",		node.Name);
			addPathComponent(		"Path:",		node.DirectoryPath);
			addComponent(			"Size:",		$"{midiInfo.SizeKB:0.00} kB");
			addComponent(			"Created:",		$"{midiInfo.FileInfo.CreationTime}");
			addComponent(			"Extension:",	$"{midiInfo.FileInfo.Extension}");

			// When the file is not a valid MIDI file, there is nothing more to populate:
			if (!midiInfo.IsMidi)
			{
				return components.ToArray();
			}


			// Now that the global properties are added populated, start adding individual
			// information for each track available in the file.
			for (int i = 0; i < midiInfo.TracksCount; ++i)
			{
				MidiTrackInfo trackInfo = midiInfo.Tracks[i];
				// Add single line spacing between each track, it makes it much easier
				// to comprehend visually.
				addSingleComponent(	string.Empty);
				addSingleComponent(	$"Track #{trackInfo.Index:00}:");
				addComponent(		"Duration:",	$"{durationToString(trackInfo.Duration)}");
				addComponent(		"Notes:",		$"{trackInfo.NoteCount}");
				// There is nothing more to add if the track has no notes.
				if (trackInfo.NoteCount == 0)
					continue;

				addPlaybackComponent(midiInfo.GetMidiFile(), trackInfo.Index);
			}

			return components.ToArray();
		}
		//
		// Summary:
		//     Sets the preview track to play.
		// Parameters:
		//   midi: The file to play or null to stop previewing.
		//   track: Index of the track within the file.
		private void SetPreviewTrack(MidiFile midi, int track = 0, bool seekToStart = true)
		{
			void safeStop()
			{
				if (_previewMusicPlayer != null && _previewMusicPlayer.IsPlaying)
					_previewMusicPlayer.Stop();

				_activeTrack = -1;
				RefreshDetails();
			}

			if (midi == null)
			{
				safeStop();
				return;
			}

			try
			{
				safeStop();
				_previewMusicPlayer.Play(midi, track);
				if (seekToStart)
				{
					double start = midi.ReadFirstNoteInSeconds(track);
					if (start > 0)
						_previewMusicPlayer.Seek(start);
				}

				_activeTrack = track;
			}
			catch
			{
				safeStop();
			}

			RefreshDetails();
		}

		public void UpdateBand(string bandName)
		{
			// Called when the text needs to change. Update the SingleComposer's Dynamic text field.
			string newText;
			if (bandName != "")
				newText = "Band Name: \n\"" + bandName + "\"";
			else
				newText = "No Band";
			SingleComposer.GetDynamicText("Band name").SetNewText(newText);
			bandNameChange(bandName);
		}
		//
		// Summary:
		//     Polls for periodic changes.
		public override void OnBeforeRenderFrame3D(float deltaTime)
		{
			// Update the playback of the preview player, if there is any.
			// This is also responsible for finishing the playback and
			//  resetting its playback state, if applicable.
			if (_previewMusicPlayer != null)
			{
				if (_previewMusicPlayer.IsPlaying)
				{
					_previewMusicPlayer.Update(deltaTime);

					if (_previewMusicPlayer.IsFinished)
						SetPreviewTrack(null);
				}
			}

			base.OnBeforeRenderFrame3D(deltaTime);
		}

		private void Close()
		{
			TryClose();
		}
	}
}
