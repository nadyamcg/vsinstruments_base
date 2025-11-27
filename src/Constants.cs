using System;
using Midi;

namespace Instruments
{
	//
	// Summary:
	//     This class contains variety of constants divided by their category.
	public static class Constants
	{
		//
		// Summary:
		//     This structure contains all mathematical constants.
		public struct Math
		{
			public const float PI = MathF.PI;
		}
		//
		// Summary:
		//     This structure contains all constants related to midi notes.
		public struct Note
		{
			//
			// Summary:
			//     The amount of notes including all octaves and accidentals.
			public const int NoteCount = (int)Pitch.G9;
			//
			// Summary:
			//     The length of a single octave in notes.
			public const int OctaveLength = (int)Pitch.C0 - (int)Pitch.CNeg1;
			//
			// Summary:
			//     The total number of octaves available.
			public const int OctaveCount = NoteCount / OctaveLength;
		}
		//
		// Summary:
		//     This structure contains constants related to networking packets.
		public struct Packet
		{
			public const int NameChangeID = 1004;
			public const int BandChangeID = 1005;
			public const int SongSelectID = 1006;

			//
			// Summary:
			//     Packet sent when a player tries to 'open' a music block. (TODO@exocs: Verify this assertion.)
			public const int MusicBlockOpenID = 69;
		}
		//
		// Summary:
		//     This structure contains constants related to networking channels.
		public struct Channel
		{
			public const string Note = "noteTest";

			public const string Abc = "abc";

			//
			// Summary:
			//     Defines the name of the networking channel used by file manager for transfering files.
			public const string FileManager = "FileTransferChannel";

			//
			// Summary:
			//     Defines the name of the networking channel used by actual song playback.
			public const string Playback = "PlaybackChannel";
		}
		//
		// Summary:
		//     This structure contains constants related to item and block attributes.
		public struct Attributes
		{
			//
			// Summary:
			//     Attribute of this type will contain the current mode the tool is set to.
			public const string ToolMode = "toolMode";
		}
		//
		// Summary:
		//     This structure contains constants related to item and block attributes.
		public struct Midi
		{
			//
			// Summary:
			//     Minimum value of the MIDI velocity event.
			public const byte VelocityMin = 0;
			//
			// Summary:
			//     Maximum value of the MIDI velocity event.
			public const byte VelocityMax = 127;
			//
			// Summary:
			//     Converts the provided velocity value into a normalized 0-1 value.
			public static float NormalizeVelocity(byte velocity)
			{
				// Clamp the value within bounds, there is no reason to go beyond the bounds in this case.
				if (velocity < VelocityMin) velocity = VelocityMin;
				else if (velocity > VelocityMax) velocity = VelocityMax;

				return velocity / (float)VelocityMax;
			}
		}
		//
		// Summary:
		//     This structure contains constants related to music players and their playback.
		public struct Playback
		{
			//
			// Summary:
			//     Default fade out time for all notes, if not specified differently.
			//     This is an arbitrary value provided by the mod, the abrupt cutoff of the sound does not
			//     sound good nor is it very physically accurate. Adjust per liking.
			public const float FadeOutDuration = 1.00f;
			//
			// Summary:
			//     Minimum fade out time for all notes, if not specified differently.
			public const float MinFadeOutDuration = 0.25f;
			//
			// Summary:
			//     Default minimum voluem for all notes, if not specified differently.
			//     This is an arbitrary value provided by the mod, used to determine the volume at minimum velocity.
			public const float MinVelocityVolume = 0.25f;
			//
			// Summary:
			//     Default minimum voluem for all notes, if not specified differently.
			//     This is an arbitrary value provided by the mod, used to determine the volume at minimum velocity.
			public const float MaxVelocityVolume = 1.0f;
			//
			// Summary:
			//     Returns the desired volume based on provided velocity.
			public static float GetVolumeFromVelocity(float velocity01)
			{
				return Single.Lerp(MinVelocityVolume, MaxVelocityVolume, velocity01);
			}
			//
			// Summary:
			//     Returns the desired fadeout duration based on provided velocity.
			public static float GetFadeDurationFromVelocity(float velocity01)
			{
				return Single.Lerp(FadeOutDuration, MinFadeOutDuration, velocity01);
			}

			//
			// Summary:
			//     Determines the tick rate at which the playback manager should update, in milliseconds.
			//     Default value is approximately equal to 30 Hz.
			public const int ManagerTickInterval = (int)((1.0 / 30.0) * 1000.0);
		}
		//
		// Summary:
		//     This structure contains constants related to music players and their playback.
		public struct Files
		{
			//
			// Summary:
			//     Determines the tick rate at which the file manager should update, in milliseconds.
			public const int ManagerTickInterval = 10;
		}
	}
}