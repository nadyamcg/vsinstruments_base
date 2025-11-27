using System;               // Array.Find()
using System.Collections.Generic; // List
using System.Diagnostics;  // Debug todo remove
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Instruments.Blocks;
using Instruments.Network.Packets;
using Instruments.Types;
using Instruments.Files;
using Instruments.Playback;

namespace Instruments.Core
{
    public class InstrumentModClient : InstrumentModBase
    {
        public override bool ShouldLoad(EnumAppSide side) // Enabling this will kill the sounds, cos the sounds are made server side smh. Might need to separate the ui stuff with sounds.
        {
            return side == EnumAppSide.Client;
        }

        #region CLIENT
        IClientNetworkChannel clientChannelNote;
        IClientNetworkChannel clientChannelABC;
        ICoreClientAPI clientApi;
        string playerHeldItem; // The item the player is holding. If this changes, stop playback.
        bool thisClientPlaying; // Is this client currently playing, or is some other client playing?
        List<Sound> soundList = new List<Sound>(); // For playing single notes sent by players, non-abc style
        List<SoundManager> soundManagers;
        bool clientSideEnable;
        bool clientSideReady = false;


        long listenerIDClient = -1;


		//
		// Summary:
		//     The object responsible for managing files on the client.
		private FileManagerClient _fileManager;
		//
		// Summary:
		//     Returns the object responsible for managing files on the client.
		public override FileManagerClient FileManager
		{
			get
			{
				return _fileManager;

			}
		}

		//
		// Summary:
		//     The object responsible for managing music playback on the client.
		private PlaybackManagerClient _playbackManager;
		//
		// Summary:
		//     Returns the object responsible for managing music playback on the client.
		public override PlaybackManagerClient PlaybackManager
		{
			get
			{
				return _playbackManager;
			}
		}

		public override void StartClientSide(ICoreClientAPI api)
        {
            clientApi = api;
            //base.StartClientSide(api);
            clientChannelNote =
                api.Network.RegisterChannel(Constants.Channel.Note)
                .RegisterMessageType(typeof(NoteStart))
                .RegisterMessageType(typeof(NoteUpdate))
                .RegisterMessageType(typeof(NoteStop))
                .SetMessageHandler<NoteStart>(MakeNote)
                .SetMessageHandler<NoteUpdate>(UpdateNote)
                .SetMessageHandler<NoteStop>(StopNote)
                ;
            clientChannelABC =
                api.Network.RegisterChannel(Constants.Channel.Abc)
                .RegisterMessageType(typeof(ABCStartFromClient))    // This needs to be here, even if there's no Message Handler
                .RegisterMessageType(typeof(ABCStopFromClient))     // I guess it's in order for the client to send stuff up to server, and below stuff is for receiving
                .RegisterMessageType(typeof(ABCUpdateFromServer))
                .RegisterMessageType(typeof(ABCStopFromServer))
                .RegisterMessageType(typeof(ABCSendSongFromServer))
                .SetMessageHandler<ABCUpdateFromServer>(ParseServerPacket)
                .SetMessageHandler<ABCStopFromServer>(StopSounds)
                .SetMessageHandler<ABCSendSongFromServer>(SongFromServer)
                ;

            soundManagers = new List<SoundManager>();

            thisClientPlaying = false;
            MusicBlockManager.Instance.Reset(); // I think there's a manager for both Server and Client, so reset it I guess
            Definitions.Instance.Reset();

            _fileManager = new FileManagerClient(api, InstrumentModSettings.Instance);
            _playbackManager = new PlaybackManagerClient(api, _fileManager);


			clientApi.RegisterCommand("instruments", "instrument playback commands", "[enable|disable]", ParseClientCommand);
            clientSideEnable = true;
            clientSideReady = true;

            //clientApi.ShowChatMessage("Cats!");
        }

        public override void Dispose()
        {
            // We MIGHT need this when resetting worlds without restarting the game
            base.Dispose();
            if (listenerIDClient != -1)
            {
                clientApi.Event.UnregisterGameTickListener(listenerIDClient);
                listenerIDClient = 0;
            }
            //soundManagers.Clear(); //Already null!
            soundList.Clear();
            clientSideReady = false;
        }
        private void MakeNote(NoteStart note)
        {
            if (!clientSideReady) return;

            string noteString = "/a3";
            if (note.instrument == "drum")
            {
                float div = note.pitch * 2 - 1;
                const float step = 0.046875f;  // 3/64
                float currentStep = 0f;
                for (int i = 0; i <= 64; i++)
                {
                    if (div < currentStep + step)
                    {
                        noteString = "/" + (i + 24);
                        break;
                    }
                    currentStep += step;
                }
                note.pitch = 1; // Reset the pitch, we don't want any pitch bend for drum
            }
            else if (note.instrument == "mic")
            {
                Random rnd = new Random();
                int rNum = rnd.Next(0, 5); // A number between 0 and 4
                switch (rNum)
                {
                    case 0:
                        noteString += "ba";
                        break;
                    case 1:
                        noteString += "bo";
                        break;
                    case 2:
                        noteString += "da";
                        break;
                    case 3:
                        noteString += "do";
                        break;
                    case 4:
                        noteString += "la";
                        break;
                }
            }
            IClientWorldAccessor clientWorldAccessor = clientApi.World;
            Sound sound = new Sound(clientWorldAccessor, note.positon, note.pitch, string.Empty, note.ID, InstrumentModSettings.Instance.playerVolume);
            if (sound.sound == null)
                Debug.WriteLine("Sound creation failed!");
            else
                soundList.Add(sound);
        }
        private void UpdateNote(NoteUpdate note)
        {
            if (!clientSideReady) return;
            //Sound sound = soundList.Find(x => x.ID.Contains(note.ID));
            Sound sound = soundList.Find(x => x.ID == note.ID);
            if (sound == null)
                return;
            sound.UpdateSound(note.positon, note.pitch);
        }
        private void StopNote(NoteStop note)
        {
            if (!clientSideReady) return;
            //Sound sound = soundList.Find(x => x.ID.Contains(note.ID));
            Sound sound = soundList.Find(x => x.ID == note.ID);
            if (sound == null)
                return;
            sound.StopSound();
            soundList.Remove(sound);
        }

        private void ParseServerPacket(ABCUpdateFromServer serverPacket)
        {
            IClientPlayer player = clientApi.World.Player; // If the client is still starting up, this will be null!
            if (player == null)
                return;

			InstrumentType instrumentType = InstrumentType.Find(serverPacket.InstrumentTypeID);
            if (instrumentType == null)
                return;
            //if (serverPacket.instrument == "" || serverPacket.instrument == "none")  // An invalid instrument was used, was the instrument pack removed?
            //    return;

            if (!clientSideEnable)
                return;

            if (!clientSideReady) return;

            SoundManager sm = soundManagers.Find(x => x.sourceID == serverPacket.fromClientID);
            if (sm == null)
            {
                // This was the first packet from the server with data from this client. Need to register a new SoundManager.
                float startTime = serverPacket.newChord.startTime;
                sm = new SoundManager(clientApi.World, serverPacket.fromClientID, instrumentType, startTime);
                soundManagers.Add(sm);
            }
            if (listenerIDClient == -1)
            {
                // This was the first abc packet from the server ever - need to register the tick listener.
                listenerIDClient = clientApi.Event.RegisterGameTickListener(OnClientGameTick, 1);
                if (serverPacket.fromClientID == player.ClientId)
                {
                    thisClientPlaying = true;
                    playerHeldItem = clientApi.World.Player.Entity.RightHandItemSlot.GetStackName();
                }
            }
            if (otherPlayerSync)
            {
                // Set the animation
                IPlayer otherPlayer = Array.Find(clientApi.World.AllOnlinePlayers, x => x.ClientId == sm.sourceID);				
				otherPlayer?.Entity?.StartAnimation(instrumentType.Animation);
            }
            sm.AddChord(serverPacket.positon, serverPacket.newChord);
        }
        private void StopSounds(ABCStopFromServer serverPacket)
        {
            if (!clientSideReady) return;
            IClientPlayer player = clientApi.World.Player; // If the client is still starting up, this will be null!
            if (player == null)
                return;

            SoundManager sm = soundManagers.Find(x => x.sourceID == serverPacket.fromClientID);
            if (sm != null)
            {
                if (sm.sourceID == player.ClientId)
                {
                    thisClientPlaying = false;
                    Definitions.Instance.SetIsPlaying(false);
                    //player?.Entity?.StopAnimation(Definitions.Instance.GetAnimation(sm.instrument));
                }
                if (otherPlayerSync)
                {
                    IPlayer otherPlayer = Array.Find(clientApi.World.AllOnlinePlayers, x => x.ClientId == sm.sourceID);
                    otherPlayer?.Entity?.StopAnimation(sm.InstrumentType.Animation);
                }
                sm.Kill();
                soundManagers.Remove(sm);
                CheckSoundManagersEmpty();
            }
        }
        private void SongFromServer(ABCSendSongFromServer serverPacket)
        {
            Definitions.Instance.AddToServerSongList(serverPacket.abcFilename);
        }

        private void OnClientGameTick(float dt)
        {
            int smCount = soundManagers.Count;
            for (int i = 0; i < smCount; i++)
            {
                if (soundManagers[i].Update(dt))
                    ;
                else
                {
                    if (soundManagers[i].sourceID == clientApi.World.Player.ClientId)
                        thisClientPlaying = false;
                    soundManagers.RemoveAt(i);
                    smCount--;
                    i--;
                }
            }
            CheckSoundManagersEmpty();
            if (thisClientPlaying)
            {
                string currentPlayerItem = clientApi.World.Player.Entity.RightHandItemSlot.GetStackName();
                if (currentPlayerItem != playerHeldItem) // Check that the player is still holding an instrument
                {
                    // TODO copied from in instrument. Make into a single function pls
                    ABCStopFromClient newABC = new ABCStopFromClient();
                    IClientNetworkChannel ch = clientApi.Network.GetChannel(Constants.Channel.Abc);
                    ch.SendPacket(newABC);
                    thisClientPlaying = false;
                }
            }
        }
        private void CheckSoundManagersEmpty()
        {
            if (soundManagers.Count == 0)
            {
                clientApi.Event.UnregisterGameTickListener(listenerIDClient);
                listenerIDClient = -1;
                thisClientPlaying = false;
            }
        }

        private void ParseClientCommand(int groupId, CmdArgs args)
        {
            string command = args.PopWord();
            switch (command)
            {
                case "enable":
                    clientSideEnable = true;
                    clientApi.ShowChatMessage("ABC playback enabled!");
                    break;
                case "disable":
                    clientSideEnable = false;
                    clientApi.ShowChatMessage("ABC playback disabled!");
                    {
                        ABCStopFromServer dummy = new ABCStopFromServer();
                        dummy.fromClientID = clientApi.World.Player.ClientId;
                        StopSounds(dummy);
                    }
                    break;
                default:
                    clientApi.ShowChatMessage("Syntax: .instruments [enable|disable]");
                    break;
            }
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
		public static InstrumentModClient GetInstrumentMod(this ICoreClientAPI clientAPI)
		{
			return GetInstrumentMod<InstrumentModClient>(clientAPI);
		}
	}
}