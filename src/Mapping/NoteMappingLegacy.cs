using System;
using System.IO;
using Midi;
using Instruments.Mapping.Mappers;

namespace Instruments.Mapping
{
	//
	// Summary:
	//     Backwards compatible legacy mapping that uses previously defined standard
	//     of providing A0, A1, A2, A3, A4, A5, A6, A7 sound samples in the instrument
	//     asset location as a0.ogg, a1.ogg, ... a7.ogg files.
	public class NoteMappingLegacy : NoteMapping<string>
	{
		public NoteMappingLegacy(string soundDirectory)
			: base()
		{
			if (soundDirectory.EndsWith(Path.DirectorySeparatorChar))
				soundDirectory = soundDirectory.Substring(0, soundDirectory.Length - 1);

			using (var mapper = new NoteMapperOctave<string>(Pitch.A0))
			{
				mapper.Add(Pitch.A0, string.Concat(soundDirectory, "/a0.ogg"));
				mapper.Add(Pitch.A1, string.Concat(soundDirectory, "/a1.ogg"));
				mapper.Add(Pitch.A2, string.Concat(soundDirectory, "/a2.ogg"));
				mapper.Add(Pitch.A3, string.Concat(soundDirectory, "/a3.ogg"));
				mapper.Add(Pitch.A4, string.Concat(soundDirectory, "/a4.ogg"));
				mapper.Add(Pitch.A5, string.Concat(soundDirectory, "/a5.ogg"));
				mapper.Add(Pitch.A6, string.Concat(soundDirectory, "/a6.ogg"));
				mapper.Add(Pitch.A7, string.Concat(soundDirectory, "/a7.ogg"));

				if (!mapper.Map(this))
				{
					throw new Exception("Failed to create legacy note map!");
				}
			}
		}
	}
}