using System.Collections.Generic;
using Vintagestory.API.Common;
using Instruments.Files;

namespace Instruments.Playback
{
	//
	// Summary:
	//     Base class for managing songs playback.
	public abstract class PlaybackManager
	{
		//
		// Summary:
		//     This class contains information about a single playback (and/or its state) for a given player.
		protected abstract class PlaybackStateBase
		{
			//
			// Summary:
			//     The player this info represents ("belongs to").
			public IPlayer Player { get; private set; }
			//
			// Summary:
			//     Creates new playback info for the provided player.
			public PlaybackStateBase(IPlayer player)
			{
				Player = player;
			}
			//
			// Summary:
			//     Returns the associated player's client ID.
			public int ClientId
			{
				get
				{
					return Player.ClientId;
				}
			}
			//
			// Summary:
			//     Returns whether the associated player is currently playing.
			public abstract bool IsPlaying { get; }
			//
			// Summary:
			//     Returns whether the associated player was playing, but is finished now.
			public abstract bool IsFinished { get; }
			//
			// Summary:
			//     Updates the state periodically.
			public virtual void Update(float deltaTime) { }
		}
		//
		// Summary:
		//     Set containing information about player playback state per client id.
		protected Dictionary<int, PlaybackStateBase> PlaybackStates { get; private set; }
		//
		// Summary:
		//     Assigns the provided playback state for its associated client.
		protected void AddPlaybackState<T>(T playbackInfo) where T : PlaybackStateBase
		{
			PlaybackStates.Add(playbackInfo.ClientId, playbackInfo);
		}
		//
		// Summary:
		//     Returns whether a playback state was already assigned to the provided client.
		protected bool HasPlaybackState(int clientId)
		{
			return PlaybackStates.ContainsKey(clientId);
		}
		//
		// Summary:
		//     Removes and outputs the playback state for the provided client.
		protected bool RemovePlaybackState<T>(int clientId, out T state) where T : PlaybackStateBase
		{
			if (PlaybackStates.Remove(clientId, out PlaybackStateBase baseState))
			{
				state = baseState as T;
				return true;
			}
			state = default;
			return false;
		}
		//
		// Summary:
		//     Finds the playback state for the provided client.
		protected PlaybackStateBase GetPlaybackState(int clientId)
		{
			if (PlaybackStates.TryGetValue(clientId, out PlaybackStateBase state))
			{
				return state;
			}
			return null;
		}
		//
		// Summary:
		//     Creates new playback manager.
		public PlaybackManager(ICoreAPI api, FileManager fileManager)
		{
			PlaybackStates = new Dictionary<int, PlaybackStateBase>(64);
		}
		//
		// Summary:
		//     Updates the playback manager. This method should be called periodically, on each game tick.
		public virtual void Update(float deltaTime) { }
		//
		// Summary:
		//     Returns whether the provided client is actively playing back.
		public bool IsPlaying(int clientId)
		{
			PlaybackStateBase state = GetPlaybackState(clientId);
			return state != null ? state.IsPlaying : false;
		}
	}
}
