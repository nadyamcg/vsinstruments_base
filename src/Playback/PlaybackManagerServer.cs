using System.Diagnostics;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using MidiParser;
using Instruments.Files;
using Instruments.Network.Playback;
using Instruments.Players;

namespace Instruments.Playback
{
	public class PlaybackManagerServer : PlaybackManager
	{
		//
		// Summary:
		//     This class contains information about a single playback (and/or its state) for a given player, on the server.
		protected class PlaybackStateServer : PlaybackStateBase
		{
			//
			// Summary:
			//     Interface to the game.
			protected ICoreServerAPI ServerAPI { get; private set; }
			//
			// Summary:
			//     Time of the last playback request in elapsed miliseconds.
			private long _lastRequestTime;
			//
			// Summary:
			//     Whether the associated player is currently playing back.
			private bool _isPlaying;
			//
			// Summary:
			//     The time in elapsed milliseconds of the server at which the playback will complete.
			private long _finishTime;
			//
			// Summary:
			//     Creates new server playback info for the provided player.
			public PlaybackStateServer(ICoreServerAPI api, IServerPlayer player) : base(player)
			{
				ServerAPI = api;
				_lastRequestTime = 0;
				_isPlaying = false;
			}
			//
			// Summary:
			//     Sets the playback as active.
			public void StartPlayback(double durationSeconds)
			{
				_isPlaying = true;
				_finishTime = ServerAPI.World.ElapsedMilliseconds + (long)(durationSeconds * 1000.0);
			}
			//
			// Summary:
			//     Deactivates the playback.
			public void StopPlayback()
			{
				_isPlaying = false;
				_finishTime = 0;
			}
			//
			// Summary:
			//     Returns whether the player is actively playing.
			public override bool IsPlaying
			{
				get
				{
					return _isPlaying;
				}
			}
			//
			// Summary:
			//     Returns whether the player has finished playing.
			public override bool IsFinished
			{
				get
				{
					return _isPlaying && (ServerAPI.World.ElapsedMilliseconds >= _finishTime);
				}
			}
			//
			// Summary:
			//     Returns the time, in elapsed milliseconds at which last request was received.
			public long LastRequestTime
			{
				get
				{
					return _lastRequestTime;
				}
			}
			//
			// Summary:
			//     Updates last request time to the current elapsed time.
			public void BumpLastRequestTime()
			{
				_lastRequestTime = ServerAPI.World.ElapsedMilliseconds;
			}
		}
		//
		// Summary:
		//     Returns the interface to the game.
		protected ICoreServerAPI ServerAPI { get; }
		//	 
		// Summary:	 
		//     Returns the networking channel for file transactions.
		protected IServerNetworkChannel ServerChannel { get; private set; }
		//
		// Summary:	 
		//     Server-side file manager used to dispatch file requests.
		protected FileManagerServer ServerFileManager { get; private set; }
		//
		// Summary:
		//     Creates new server side playback manager.
		public PlaybackManagerServer(ICoreServerAPI api, FileManagerServer fileManager)
			: base(api, fileManager)
		{
			ServerAPI = api;
			ServerChannel = api.Network.RegisterChannel(Constants.Channel.Playback)
				.RegisterMessageType<StartPlaybackRequest>()
				.RegisterMessageType<StartPlaybackBroadcast>()
				.RegisterMessageType<StartPlaybackOwner>()
				.RegisterMessageType<StartPlaybackDenyOwner>()
				.RegisterMessageType<StopPlaybackRequest>()
				.RegisterMessageType<StopPlaybackBroadcast>()

				.SetMessageHandler<StartPlaybackRequest>(OnStartPlaybackRequest)
				.SetMessageHandler<StopPlaybackRequest>(OnStopPlaybackRequest);


			ServerFileManager = fileManager;

			// Create a container for the status of playback as known on the server for the joining player,
			ServerAPI.Event.PlayerJoin += (IServerPlayer player) =>
			{
				PlaybackStateServer state = new PlaybackStateServer(api, player);
				AddPlaybackState(state);

				// Whenever a player modifies their hotbar, terminate their playback if they were active.
				IInventory playerHotbar = player.InventoryManager.GetHotbarInventory();
				playerHotbar.SlotModified += (int slotID) =>
				{
					if (IsPlaying(player.ClientId))
					{
						StopPlayback(player.ClientId, StopPlaybackReason.Terminated);
					}
				};
			};
			
			// And dispose of it, making sure to terminate any outgoing playback for players that are leaving.
			ServerAPI.Event.PlayerLeave += (IServerPlayer player) =>
			{
				if (RemovePlaybackState(player.ClientId, out PlaybackStateServer state) && state.IsPlaying)
				{
					// Simply stop the playback locally. Other clients are already aware of the specified user
					// leaving, there is no need to do additional synchronization.
					state.StopPlayback();
				}
			};

			// Whenever a player dies, terminate their playback if they were active.
			ServerAPI.Event.PlayerDeath += (IServerPlayer player, DamageSource damage) =>
			{
				if (IsPlaying(player.ClientId))
				{
					StopPlayback(player.ClientId, StopPlaybackReason.Terminated);
				}
			};

			// Whenever a player changes their active hotbar slot, terminate their playback if they were active.
			ServerAPI.Event.AfterActiveSlotChanged += (IServerPlayer player, ActiveSlotChangeEventArgs args) =>
			{
				if (IsPlaying(player.ClientId))
				{
					StopPlayback(player.ClientId, StopPlaybackReason.Terminated);
				}
			};

			// Register the tick event for the playback updates.
			ServerAPI.Event.RegisterGameTickListener(Update, Constants.Playback.ManagerTickInterval);
		}
		//
		// Summary:
		//     Called when a client requests playback start.
		protected void OnStartPlaybackRequest(IServerPlayer source, StartPlaybackRequest packet)
		{
			// Validate whether the request was valid whatsoever, check for malicious attempts,
			// deny anything illicit, bad or broken:
			if (!ValidatePlaybackRequest(source, packet))
			{
				// TODO@exocs: Handle?
				return;
			}

			// Request the specified file:
			ServerFileManager.RequestFile(source, packet.File, (node, context) =>
			{
				// Upon receiving the request file, approve the request and
				// start the playback both locally and for all relevant clients:
				StartPlayback(source, packet.File, node, packet.Channel, packet.Instrument);
			});
		}
		//
		// Summary:
		//     Called when a client requests playback start.
		protected void StartPlayback(IServerPlayer source, string sourceFile, FileTree.Node serverFile, int channel, int instrumentType)
		{
			// Retrieve the server-side playback state.
			PlaybackStateServer state = GetPlaybackState(source.ClientId) as PlaybackStateServer;

			// Throttling rate in milliseconds; TODO@exocs: Move to constants
			const long ThrottleRequestsRate = 1000;

			// Determine the duration between last and current request.
			long elapsedTime = ServerAPI.World.ElapsedMilliseconds - state.LastRequestTime;
			if (elapsedTime < ThrottleRequestsRate) // TODO@exocs: Constant
			{
				StartPlaybackDenyOwner tooManyRequests = new StartPlaybackDenyOwner();
				tooManyRequests.Reason = DenyPlaybackReason.TooManyRequests;
				ServerChannel.SendPacket(tooManyRequests, source);
				return;
			}

			// Since a request was already received, simply bump the last request time
			// so we can limit the amount of requests processed.
			state.BumpLastRequestTime();

			// If there is an ongoing playback already, just drop it and inform the owner that
			// it has been denied, no broadcast was done yet, so no further actions are needed.
			if (state.IsPlaying)
			{
				StartPlaybackDenyOwner opInProgress = new StartPlaybackDenyOwner();
				opInProgress.Reason = DenyPlaybackReason.OperationInProgress;
				ServerChannel.SendPacket(opInProgress, source);
				return;
			}

			// Try parsing the file, at least the duration will need to be known in advance, plus
			// the additional verification that supposed file is a MIDI prior to dispatching it to
			// all the other clients is definitely much welcome.
			double fileDuration = 0;
			bool fileValidated = false;

			try
			{
				MidiFile midi = new MidiFile(serverFile.FullPath);
				fileDuration = midi.ReadTrackDuration(channel);
				fileValidated = true;
			}
			catch
			{
				fileValidated = false;
			}

			// TODO@exocs:
			//    Validate the actual file. If it's something illicit, or not a midi, just drop the request.
			if (!fileValidated)
			{
				StartPlaybackDenyOwner deny = new StartPlaybackDenyOwner();
				deny.Reason = DenyPlaybackReason.InvalidFile;
				ServerChannel.SendPacket(deny, source);
				return;
			}

			// Send a packet to all the clients except for the actual instigator, as all these players
			// will use the "shared" data path with the source player UID stamped in the path.
			StartPlaybackBroadcast startBroadcast = new StartPlaybackBroadcast();
			startBroadcast.ClientId = source.ClientId;
			startBroadcast.Channel = channel;
			startBroadcast.File = serverFile.RelativePath;
			startBroadcast.Instrument = instrumentType;
			ServerChannel.BroadcastPacket(startBroadcast, exceptPlayers: source);

			// Send a packet to the actual instigator, as they will be playing local file:
			StartPlaybackOwner startOwner = new StartPlaybackOwner();
			startOwner.Channel = channel;
			startOwner.File = sourceFile;
			startOwner.Instrument = instrumentType;
			ServerChannel.SendPacket(startOwner, source);

			// Set the local state of the playback.
			state.StartPlayback(fileDuration);
		}
		//
		// Summary:
		//     Returns whether specified player can start playback with provided data.
		protected bool ValidatePlaybackRequest(IServerPlayer source, StartPlaybackRequest packet)
		{
			return true;
		}
		//
		// Summary:
		//     Called when a client requests playback stop.
		protected void OnStopPlaybackRequest(IServerPlayer source, StopPlaybackRequest packet)
		{
			PlaybackStateServer state = GetPlaybackState(source.ClientId) as PlaybackStateServer;
			if (!state.IsPlaying)
			{
				return;
			}

			StopPlayback(source.ClientId, StopPlaybackReason.Cancelled);
		}
		//
		// Summary:
		//     Stops the playback for provided clientId and broadcasts the action to all clients.
		protected void StopPlayback(int clientId, StopPlaybackReason reason)
		{
			PlaybackStateServer state = GetPlaybackState(clientId) as PlaybackStateServer;
			Debug.Assert(state.IsPlaying);
			
			// Send a packet to all the clients, including the instigator, to stop
			// the current playback.
			StopPlaybackBroadcast stopBroadcast = new StopPlaybackBroadcast();
			stopBroadcast.ClientId = clientId;
			stopBroadcast.Reason = reason;
			ServerChannel.BroadcastPacket(stopBroadcast);

			// Set the local state of the playback.
			state.StopPlayback();
		}
		//
		// Summary:
		//     Updates the playback manager.
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
					PlaybackStateServer stateServer = (PlaybackStateServer)state;
					stateServer.StopPlayback();
				}
			}
		}
	}
}
