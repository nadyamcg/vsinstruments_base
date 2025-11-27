using System;

namespace Instruments
{
    public enum PlayMode
    {
        lockedTone = 0, // Player y angle floored to nearest tone
        lockedSemiTone, // Player y angle floored to nearest semi-tone
        fluid,      // Player y angle directly correlates to pitch
        abc         // Playing an abc file
    }

	[Obsolete("Do not pass notes as string, use Midi.Note or Midi.Pitch instead!")]
	public struct NoteFrequency
    {
        public string ID;
        public float pitch;
        public NoteFrequency(string id, float p)
        {
            ID = id;
            pitch = p;
        }
    }
}