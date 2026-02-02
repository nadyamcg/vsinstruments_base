using System;
using VSInstrumentsBase.src.Midi;

namespace VSInstrumentsBase.src;

public static class Constants
{
  public struct Math
  {
    public const float PI = MathF.PI;
  }

  public struct Note
  {
    public const int NoteCount = (int)Pitch.G9;
    public const int OctaveLength = (int)Pitch.C0 - (int)Pitch.CNeg1;
    public const int OctaveCount = NoteCount / OctaveLength;
  }

  public struct Packet
  {
    public const int NameChangeID = 1004;
    public const int BandChangeID = 1005;
    public const int SongSelectID = 1006;
    public const int MusicBlockOpenID = 69;
  }

  public struct Channel
  {
    public const string Note = "noteTest";
    public const string Abc = "abc";
    public const string FileManager = "FileTransferChannel";
    public const string Playback = "PlaybackChannel";
    public const string MusicBlock = "instrumentsMusicBlock";
  }

  public struct Attributes
  {
    public const string ToolMode = "toolMode";
  }

  public struct Midi
  {
    public const byte VelocityMin = 0;
    public const byte VelocityMax = 127;

    public static float NormalizeVelocity(byte velocity)
    {
      if (velocity < VelocityMin) velocity = VelocityMin;
      else if (velocity > VelocityMax) velocity = VelocityMax;

      return velocity / (float)VelocityMax;
    }
  }

  public struct Playback
  {
    public const float FadeOutDuration = 1.00f;
    public const float MinFadeOutDuration = 0.25f;
    public const float MinVelocityVolume = 0.25f;
    public const float MaxVelocityVolume = 1.0f;
    public const int ManagerTickInterval = (int)((1.0 / 30.0) * 1000.0);

    public static float GetVolumeFromVelocity(float velocity01)
    {
      return Single.Lerp(MinVelocityVolume, MaxVelocityVolume, velocity01);
    }

    public static float GetFadeDurationFromVelocity(float velocity01)
    {
      return Single.Lerp(FadeOutDuration, MinFadeOutDuration, velocity01);
    }
  }

  public struct Files
  {
    public const int ManagerTickInterval = 10;
  }
}
