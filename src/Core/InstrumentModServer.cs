using System.Collections.Generic; // List
using System.IO; // Open files
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Instruments.Network.Packets;
using Instruments.Blocks;
using Instruments.Files;
using Instruments.Playback;

namespace Instruments.Core
{
    public class InstrumentModServer : InstrumentModBase
    {
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        #region SERVER
        private ICoreServerAPI serverAPI;
        IServerNetworkChannel serverChannelNote;
        IServerNetworkChannel serverChannelABC;

        long listenerID = -1;
        string abcBaseDir;

        private struct PlaybackData
        {
            public int ClientID;
            public string abcData;
            public ABCParser parser;
            public int index;
        }

		//
		// Summary:
		//     The object responsible for managing files on the server.
		private FileManagerServer _fileManager;
		//
		// Summary:
		//     Returns the object responsible for managing files on the server.
		public override FileManagerServer FileManager
		{
			get
			{
				return _fileManager;
			}
		}

		//
		// Summary:
		//     The object responsible for managing music playback on the server.
		private PlaybackManagerServer _playbackManager;
		//
		// Summary:
		//     Returns the object responsible for managing music playback on the server.
		public override PlaybackManagerServer PlaybackManager
		{
			get
			{
				return _playbackManager;
			}
		}

		public override void StartServerSide(ICoreServerAPI api)
        {
            serverAPI = api;
            base.StartServerSide(api);
            serverChannelNote =
                api.Network.RegisterChannel(Constants.Channel.Note)
                .RegisterMessageType(typeof(NoteStart))
                .RegisterMessageType(typeof(NoteUpdate))
                .RegisterMessageType(typeof(NoteStop))
                .SetMessageHandler<NoteStart>(RelayMakeNote)
                .SetMessageHandler<NoteUpdate>(RelayUpdateNote)
                .SetMessageHandler<NoteStop>(RelayStopNote)
                ;
            serverChannelABC =
                api.Network.RegisterChannel(Constants.Channel.Abc)
                .RegisterMessageType(typeof(ABCStartFromClient))
                .RegisterMessageType(typeof(ABCStopFromClient))
                .RegisterMessageType(typeof(ABCUpdateFromServer))
                .RegisterMessageType(typeof(ABCStopFromServer))
                .RegisterMessageType(typeof(ABCSendSongFromServer))
                .SetMessageHandler<ABCStartFromClient>(StartABC)
                .SetMessageHandler<ABCStopFromClient>(StopABC)
                .SetMessageHandler<ABCStopFromServer>(null)
                .SetMessageHandler<ABCUpdateFromServer>(null)
                ;

            serverAPI.Event.RegisterGameTickListener(OnServerGameTick, 1); // arg1 is millisecond Interval
            MusicBlockManager.Instance.Reset();
            ABCParsers.Instance.SetAPI(serverAPI);

            _fileManager = new FileManagerServer(api, InstrumentModSettings.Instance);
            _playbackManager = new PlaybackManagerServer(api, _fileManager);

            serverAPI.Event.PlayerJoin += SendSongs;
        }
        public override void Dispose()
        {
            // We MIGHT need this when resetting worlds without restarting the game
            base.Dispose();
            if (listenerID != -1)
            {
                serverAPI.Event.UnregisterGameTickListener(listenerID);
                listenerID = 0;
            }
            ABCParsers.Instance.Reset();
        }
        public void SendSongs(IServerPlayer byPlayer)
        {
            string serverDir = InstrumentModSettings.Instance.abcServerLocation;
            if (!RecursiveFileProcessor.DirectoryExists(serverDir))
                return; // Server has no abcs, do nothing

            List<string> abcFiles = new List<string>();
            RecursiveFileProcessor.ProcessDirectory(serverDir, serverDir + Path.DirectorySeparatorChar, ref abcFiles);
            if (abcFiles.Count == 0)
            {
                return; // No files in the folder
            }
            foreach (string song in abcFiles)
            {
                ABCSendSongFromServer packet = new ABCSendSongFromServer();
                packet.abcFilename = song;
                serverChannelABC.SendPacket(packet, byPlayer);
            }
        }
        private void RelayMakeNote(IPlayer fromPlayer, NoteStart note)
        {
            // Send A packet to all clients (or clients within the area?) to start a note
            note.ID = fromPlayer.ClientId;
            serverChannelNote.BroadcastPacket(note);
        }
        private void RelayUpdateNote(IPlayer fromPlayer, NoteUpdate note)
        {
            // Send A packet to all clients (or clients within the area?) to start a note
            note.ID = fromPlayer.ClientId;
            serverChannelNote.BroadcastPacket(note);
        }
        private void RelayStopNote(IPlayer fromPlayer, NoteStop note)
        {
            // Send A packet to all clients (or clients within the area?) to start a note
            note.ID = fromPlayer.ClientId;
            serverChannelNote.BroadcastPacket(note);
        }
        private void StartABC(IPlayer fromPlayer, ABCStartFromClient abcData)
        {
            ABCParser abcp = ABCParsers.Instance.FindByID(fromPlayer.ClientId);
            if (abcp == null)
            {
                string abcSong = "";
                if (abcData.isServerFile)
                {
                    // The contained string is NOT a full song, but a link to it on the server.
                    // Find this file, load it, and make the abcParser in the same way
                    string fileLocation = InstrumentModSettings.Instance.abcServerLocation;
                    RecursiveFileProcessor.ReadFile(fileLocation + Path.DirectorySeparatorChar + abcData.abcData, ref abcSong);
                }
                else
                {
                    abcSong = abcData.abcData;
                }

                ABCParsers.Instance.MakeNewParser(serverAPI, fromPlayer, abcSong, abcData.bandName, abcData.instrument);
                if (serversideAnimSync)
                    fromPlayer?.Entity?.StartAnimation(Definitions.Instance.GetAnimation(abcData.instrument));
            }
            else
            {
                ABCParsers.Instance.Remove(serverAPI, fromPlayer, abcp);
            }
            /*
            if (listenerID == -1)
            {
                listenerID = serverAPI.Event.RegisterGameTickListener(OnServerGameTick, 1); // arg1 is millisecond Interval
            }
            */
        }
        private void StopABC(IPlayer fromPlayer, ABCStopFromClient abcData)
        {
            int clientID = fromPlayer.ClientId;
            ABCParser abcp = ABCParsers.Instance.FindByID(clientID);
            if (abcp != null)
            {
                ABCParsers.Instance.Remove(serverAPI, fromPlayer, abcp);
                ABCStopFromServer packet = new ABCStopFromServer();
                packet.fromClientID = clientID;
                IServerNetworkChannel ch = serverAPI.Network.GetChannel(Constants.Channel.Abc);
                ch.BroadcastPacket(packet);

                if(serversideAnimSync)
                    fromPlayer?.Entity?.StopAnimation(Definitions.Instance.GetAnimation(abcp.instrument));
            }

            return;
        }
        private void OnServerGameTick(float dt)
        {
            ABCParsers.Instance.Update(serverAPI, dt);
        }
        #endregion
    }

	//
	// Summary:
	//     This class provides convenience extension wrappers for retrieving the mod system instance.
	public static partial class InstrumentModExtensions
	{
		//
		// Summary:
		//     Convenience wrapper for retrieving the instruments mod instance.
		public static InstrumentModServer GetInstrumentMod(this ICoreServerAPI serverAPI)
		{
			return GetInstrumentMod<InstrumentModServer>(serverAPI);
		}
	}
}