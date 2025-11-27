using Vintagestory.API.Client;
using Vintagestory.API.Common;
using MidiParser;
using Instruments.Files;
using Instruments.Network.Playback;
using Instruments.Players;
using Instruments.Types;

namespace Instruments.Playback
{
	public class PlaybackManagerClient : PlaybackManager
	{
		//
		// Summary:
		//     This class contains information about a single playback (and/or its state) for a given player, on the client.
		protected class PlaybackStateClient : PlaybackStateBase
		{
			//
			// Summary:
			//     Interface to the game.
			protected ICoreClientAPI ClientAPI { get; private set; }
			//
			// Summary:
			//     Midi player for this player, if any is present.
			protected MidiPlayerBase MidiPlayer { get; private set; }
			//
			// Summary:
			//     Creates new server playback info for the provided player.
			public PlaybackStateClient(ICoreClientAPI api, IClientPlayer player) : base(player)
			{
				ClientAPI = api;
			}
			//
			// Summary:
			//     Starts playback.
			public void StartPlayback(MidiFile midi, InstrumentType instrumentType, int channel, double startTime)
			{
				MidiPlayer = new MidiPlayer(ClientAPI, Player, instrumentType);
				MidiPlayer.Play(midi, channel);
				MidiPlayer.TrySeek(startTime);
			}
			//
			// Summary:
			//     Starts playback.
			public void StopPlayback()
			{
				if (MidiPlayer != null)
				{
					MidiPlayer.TryStop();
					MidiPlayer.Dispose();
					MidiPlayer = null;
				}
			}
			//
			// Summary:
			//     Returns whether the player is actively playing.
			public override bool IsPlaying
			{
				get
				{
					return MidiPlayer != null;
				}
			}
			//
			// Summary:
			//     Returns whether the player is was playing, but is now finished.
			public override bool IsFinished
			{
				get
				{
					return MidiPlayer != null && MidiPlayer.IsFinished;
				}
			}
			//
			// Summary:
			//     Returns whether the player is actively playing.
			public override void Update(float deltaTime)
			{
				if (!MidiPlayer.IsFinished)
				{
					MidiPlayer.Update(deltaTime);
				}
			}
		}
		//	 
		// Summary:	 
		//     Returns the interface to the game.
		protected ICoreClientAPI ClientAPI { get; }
		//	 
		// Summary:	 
		//     Returns the networking channel for file transactions.
		protected IClientNetworkChannel ClientChannel { get; private set; }
		//
		// Summary:	 
		//     Client-side file manager used to fetch files.
		protected FileManagerClient ClientFileManager { get; private set; }
		//
		// Summary:
		//     Creates new client side playback manager.
		public PlaybackManagerClient(ICoreClientAPI api, FileManagerClient fileManager)
			: base(api, fileManager)
		{
			ClientAPI = api;
			ClientChannel = api.Network.RegisterChannel(Constants.Channel.Playback)
				.RegisterMessageType<StartPlaybackRequest>()
				.RegisterMessageType<StartPlaybackBroadcast>()
				.RegisterMessageType<StartPlaybackOwner>()
				.RegisterMessageType<StartPlaybackDenyOwner>()
				.RegisterMessageType<StopPlaybackRequest>()
				.RegisterMessageType<StopPlaybackBroadcast>()

				.SetMessageHandler<StartPlaybackBroadcast>(OnStartPlaybackBroadcast)
				.SetMessageHandler<StartPlaybackOwner>(OnStartPlaybackOwner)
				.SetMessageHandler<StartPlaybackDenyOwner>(OnStartPlaybackDenyOwner)
				.SetMessageHandler<StopPlaybackBroadcast>(OnStopPlaybackBroadcast);

			ClientFileManager = fileManager;

			// Create a container for the status of playback as known on the server for the joining player,
			ClientAPI.Event.PlayerJoin += (IClientPlayer player) =>
			{
				PlaybackStateClient state = new PlaybackStateClient(api, player);
				AddPlaybackState(state);
			};

			// Additionally create a state for every client that was already connected:
			foreach (IPlayer player in ClientAPI.World.AllOnlinePlayers)
			{
				if (!HasPlaybackState(player.ClientId))
				{
					PlaybackStateClient state = new PlaybackStateClient(api, player as IClientPlayer);
					AddPlaybackState(state);
				}
			}

			// And dispose of it, making sure to terminate any outgoing playback for players that are leaving.
			ClientAPI.Event.PlayerLeave += (IClientPlayer player) =>
			{
				if (RemovePlaybackState(player.ClientId, out PlaybackStateClient state) && state.IsPlaying)
				{
					// Simply stop the playback locally. The server and other clients are also aware of this fact,
					// so there is no need for additional synchronization logic.
					state.StopPlayback();
				}
			};

			// Register the tick event for the playback updates.
			ClientAPI.Event.RegisterGameTickListener(Update, Constants.Playback.ManagerTickInterval);
		}
		//
		// Summary:
		//     Asks the server to start the playback with provided data.
		public void RequestStartPlayback(string file, int channel, InstrumentType instrumentType)
		{
			// TODO@exocs:
			//   Drop the request locally, if we are playing already.

			StartPlaybackRequest startRequest = new StartPlaybackRequest();
			startRequest.File = file;
			startRequest.Channel = channel;
			startRequest.Instrument = instrumentType.ID;
			ClientChannel.SendPacket(startRequest);
		}
		//
		// Summary:
		//     Asks the server to stop the current playback.
		public void RequestStopPlayback()
		{
			StopPlaybackRequest stopRequest = new StopPlaybackRequest();
			ClientChannel.SendPacket(stopRequest);
		}
		//
		// Summary:
		//     Callback raised when playback starts.
		//     This callback is called for all players except the actual instigator (the instrument player).
		protected void OnStartPlaybackBroadcast(StartPlaybackBroadcast packet)
		{
			// Store the time at which the start occured, as the request may take some time
			// and the client will need to seek the player to the offset from now.
			long elapsedMilliseconds = ClientAPI.World.ElapsedMilliseconds;

			// Retrieve the state for the player that instigated the playback, before
			// processing with the file request.
			PlaybackStateClient state = GetPlaybackState(packet.ClientId) as PlaybackStateClient;

			ClientFileManager.RequestFile(state.Player, packet.File, (node, context) =>
			{
				double delaySec = (ClientAPI.World.ElapsedMilliseconds - (long)context) / 1000.0f;
				StartPlayback(state.Player.ClientId, node, packet.Channel, packet.Instrument, delaySec);

			}, elapsedMilliseconds);
		}
		//
		// Summary:
		//     Callback raised when playback ends.
		//     This callback is called for all players except the actual instigator (the instrument player).
		protected void OnStopPlaybackBroadcast(StopPlaybackBroadcast packet)
		{
			StopPlayback(packet.ClientId, packet.Reason);
		}
		//
		// Summary:
		//     Callback raised when playback starts.
		//     This callback is called for the actual instigator only.
		protected void OnStartPlaybackOwner(StartPlaybackOwner packet)
		{
			FileTree.Node node = ClientFileManager.UserTree.Find(packet.File);
			StartPlayback(ClientAPI.World.Player.ClientId, node, packet.Channel, packet.Instrument, 0);
			ShowPlaybackNotification($"Playing track #{packet.Channel:00} of {System.IO.Path.GetFileNameWithoutExtension(packet.File)}.");
		}
		//
		// Summary:
		//     Callback raised when a playback request is denied.
		//     This callback is called for the actual instigator only.
		protected void OnStartPlaybackDenyOwner(StartPlaybackDenyOwner packet)
		{
			ShowPlaybackErrorMessage(packet.Reason.GetText());
		}
		//
		// Summary:
		//     Starts the actual playback for the provided client, locally.
		// Parameters:
		//   clientId: Unique identifier of the client that started playing.
		//   node: The file node pointing to the file that should be played.
		//   channel: The index of the midi track to play back.
		//   instrumentTypeId: Unique identifier of the instrument type to play with.
		//   startTimeSec: Time in seconds that should be skipped in the playback.
		protected void StartPlayback(int clientId, FileTree.Node node, int channel, int instrumentTypeId, double startTimeSec = 0)
		{
			try
			{
				PlaybackStateClient state = GetPlaybackState(clientId) as PlaybackStateClient;

				MidiFile midi = new MidiFile(node.FullPath);
				InstrumentType instrumentType = InstrumentType.Find(instrumentTypeId);

				state.StartPlayback(midi, instrumentType, channel, startTimeSec);
			}
			catch
			{
				ShowPlaybackErrorMessage("An internal error occured.");
			}
		}
		//
		// Summary:
		//     Stops the actual playback for the provided client, locally.
		protected void StopPlayback(int clientId, StopPlaybackReason reason)
		{
			PlaybackStateClient state = GetPlaybackState(clientId) as PlaybackStateClient;
			state.StopPlayback();

			// TODO@exocs: For now stopping is handled as a broadcast, if there is additional information needed
			// for the owner/instigator specifically, this can be changed.
			if (clientId == ClientAPI.World.Player.ClientId)
			{
				ShowPlaybackNotification($"Playback stopped: {reason.GetText()}");
			}
		}
		//
		// Summary:
		//     Updates this managed and all its music players.
		public override void Update(float deltaTime)
		{
			var states = PlaybackStates.Values;
			foreach (PlaybackStateBase state in states)
			{
				if (!state.IsPlaying)
					continue;

				state.Update(deltaTime);

				if (state.IsFinished)
				{
					StopPlayback(state.ClientId, StopPlaybackReason.Finished);
				}
			}
		}
		//
		// Summary:
		//     Notifies the client with a playback message.
		protected void ShowPlaybackErrorMessage(string reason)
		{
			ClientAPI.ShowChatMessage($"Instruments playback failed: {reason}");
		}
		//
		// Summary:
		//     Notifies the client with a playback message.
		protected void ShowPlaybackNotification(string message)
		{
			ClientAPI.ShowChatMessage($"Instruments: {message}");
		}
	}
}
