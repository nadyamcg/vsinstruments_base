using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using VSInstrumentsBase.src.Files;


namespace VSInstrumentsBase.src.Playback;

public abstract class PlaybackManager
{
  protected PlaybackManager(ICoreAPI api, FileManager fileManager)
  {
  }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected Dictionary<int, PlaybackManager.PlaybackStateBase> PlaybackStates { get; private set; } = new Dictionary<int, PlaybackManager.PlaybackStateBase>(64 );

  protected void AddPlaybackState<T>(T playbackInfo) where T : PlaybackManager.PlaybackStateBase
  {
    this.PlaybackStates.Add(playbackInfo.ClientId, (PlaybackManager.PlaybackStateBase) playbackInfo);
  }

  protected bool HasPlaybackState(int clientId) => this.PlaybackStates.ContainsKey(clientId);

  protected bool RemovePlaybackState<T>(int clientId, out T state) where T : PlaybackManager.PlaybackStateBase
  {
    if (this.PlaybackStates.Remove(clientId, out PlaybackManager.PlaybackStateBase playbackStateBase))
    {
      state = playbackStateBase as T;
      return true;
    }
    state = default (T);
    return false;
  }

  protected PlaybackManager.PlaybackStateBase GetPlaybackState(int clientId)
  {
    PlaybackManager.PlaybackStateBase playbackStateBase;
    return this.PlaybackStates.TryGetValue(clientId, out playbackStateBase) ? playbackStateBase : (PlaybackManager.PlaybackStateBase) null;
  }

  public virtual void Update(float deltaTime)
  {
  }

  public bool IsPlaying(int clientId)
  {
    PlaybackManager.PlaybackStateBase playbackState = this.GetPlaybackState(clientId);
    return playbackState != null && playbackState.IsPlaying;
  }

  protected abstract class PlaybackStateBase(IPlayer player)
  {
    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IPlayer Player { get; private set; } = player;

    public int ClientId => this.Player.ClientId;

    public abstract bool IsPlaying { get; }

    public abstract bool IsFinished { get; }

    public virtual void Update(float deltaTime)
    {
    }
  }
}
