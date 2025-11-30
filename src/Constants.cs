using System.Runtime.InteropServices;

namespace VSInstrumentsBase.src;

public static class Constants
{
  [StructLayout(LayoutKind.Sequential, Size = 1)]
  public struct Math
  {
    public const float PI = 3.14159274f;
  }

  [StructLayout(LayoutKind.Sequential, Size = 1)]
  public struct Note
  {
    public const int NoteCount = 127 ;
    public const int OctaveLength = 12;
    public const int OctaveCount = 10;
  }

  [StructLayout(LayoutKind.Sequential, Size = 1)]
  public struct Packet
  {
    public const int NameChangeID = 1004;
    public const int BandChangeID = 1005;
    public const int SongSelectID = 1006;
    public const int MusicBlockOpenID = 69;
  }

  [StructLayout(LayoutKind.Sequential, Size = 1)]
  public struct Channel
  {
    public const string Note = "noteTest";
    public const string Abc = "abc";
    public const string FileManager = "FileTransferChannel";
    public const string Playback = "PlaybackChannel";
    public const string MusicBlock = "instrumentsMusicBlock";
  }

  [StructLayout(LayoutKind.Sequential, Size = 1)]
  public struct Attributes
  {
    public const string ToolMode = "toolMode";
  }

  [StructLayout(LayoutKind.Sequential, Size = 1)]
  public struct Midi
  {
    public const byte VelocityMin = 0;
    public const byte VelocityMax = 127 ;

    public static float NormalizeVelocity(byte velocity)
    {
      if (velocity < (byte) 0)
        velocity = (byte) 0;
      else if (velocity > (byte) 127 )
        velocity = (byte) 127 ;
      return (float) velocity / (float) sbyte.MaxValue;
    }
  }

  [StructLayout(LayoutKind.Sequential, Size = 1)]
  public struct Playback
  {
    public const float FadeOutDuration = 1f;
    public const float MinFadeOutDuration = 0.25f;
    public const float MinVelocityVolume = 0.25f;
    public const float MaxVelocityVolume = 1f;
    public const int ManagerTickInterval = 33;

    public static float GetVolumeFromVelocity(float velocity01)
    {
      return float.Lerp(0.25f, 1f, velocity01);
    }

    public static float GetFadeDurationFromVelocity(float velocity01)
    {
      return float.Lerp(1f, 0.25f, velocity01);
    }
  }

  [StructLayout(LayoutKind.Sequential, Size = 1)]
  public struct Files
  {
    public const int ManagerTickInterval = 10;
  }
}
