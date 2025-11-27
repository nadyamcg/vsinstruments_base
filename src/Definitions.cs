using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Midi;
using Instruments.Core;
using Instruments.Mapping;

namespace Instruments
{
	public class Definitions
	{
		private string bandName = "";
		private PlayMode mode = PlayMode.abc;
		private static Definitions _instance;
		private Dictionary<string, string> animMap = new Dictionary<string, string>();
		private List<string> abcFiles = new List<string>();
		private List<string> serverAbcFiles = new List<string>();
		private bool messageDone = false;
		private bool abcPlaying = false;
		
		private Definitions()
		{
		}

		[Obsolete("Use Instance instead!")]
		public static Definitions GetInstance()
		{
			if (_instance != null)
				return _instance;
			return _instance = new Definitions();
		}

		public static Definitions Instance
		{
			get
			{
				return _instance != null ? _instance : _instance = new Definitions();
			}
		}

		public void SetBandName(string bn)
		{
			bandName = bn;
		}
		public string GetBandName()
		{
			return bandName;
		}
		[Obsolete("Use toolMode attribute instead!", false)]
		public void SetPlayMode(PlayMode newMode)
		{
			mode = newMode;
		}
		[Obsolete("Use toolMode attribute instead!", false)]
		public PlayMode GetPlayMode()
		{
			return mode;
		}
		[Obsolete("Use Pitch and its extension API instead!", false)]
		public NoteFrequency GetFrequency(int index)
		{
			Pitch pitch = Pitch.A3 + index;
			Midi.Note note = pitch.NotePreferringSharps();
			return new NoteFrequency(note.ToString(), pitch.RelativePitch(Pitch.A3));
		}
		public List<string> GetSongList()
		{
			return abcFiles;
		}
		[Obsolete("Use InstrumentItemType API instead!")]
		public string GetAnimation(string type)
		{
			return "holdbothhands";
		}
		public bool UpdateSongList(ICoreClientAPI capi)
		{
			abcFiles.Clear();
			// First, check the client's dir exists
			string localDir = InstrumentModSettings.Instance.abcLocalLocation;
			if (RecursiveFileProcessor.DirectoryExists(localDir))
			{
				// It exists! Now find the files in it
				RecursiveFileProcessor.ProcessDirectory(localDir, localDir + Path.DirectorySeparatorChar, ref abcFiles);
			}
			else
			{
				if (!messageDone)
				{
					// Client ABC folder not found, log a message to tell the player where it should be. But still search the server folder
					capi.ShowChatMessage("ABC warning: Could not find folder at \"" + localDir + "\". Displaying server files instead.");
					messageDone = true;
				}
			}
			foreach (string song in serverAbcFiles)
				abcFiles.Add(song);

			if (abcFiles.Count == 0)
			{
				capi.ShowChatMessage("ABC error: No abc files found!");
				return false;
			}
			else
			{
				return true;
			}
		}
		public void AddToServerSongList(string songFileName)
		{
			serverAbcFiles.Add(songFileName);
		}
		[Obsolete("Abc playback is obsolete!")]
		public string ABCBasePath()
		{
			return InstrumentModSettings.Instance.abcLocalLocation;
		}
		public void SetIsPlaying(bool toggle)
		{
			abcPlaying = toggle;
		}
		public bool IsPlaying() { return abcPlaying; }
		public void Reset()
		{
			abcFiles.Clear();
			serverAbcFiles.Clear();
		}
	}
}
