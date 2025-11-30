using VSInstrumentsBase.src.Mapping.Mappers;
using VSInstrumentsBase.src.Midi;
using System;
using System.IO;


namespace VSInstrumentsBase.src.Mapping;

public class NoteMappingLegacy : NoteMapping<string>
{
  public NoteMappingLegacy(string soundDirectory)
  {
    if (soundDirectory.EndsWith(Path.DirectorySeparatorChar))
      soundDirectory = soundDirectory.Substring(0, soundDirectory.Length - 1);
    using (NoteMapperOctave<string> noteMapperOctave = new NoteMapperOctave<string>(Pitch.A0))
    {
      noteMapperOctave.Add(Pitch.A0, soundDirectory + "/a0.ogg");
      noteMapperOctave.Add(Pitch.A1, soundDirectory + "/a1.ogg");
      noteMapperOctave.Add(Pitch.A2, soundDirectory + "/a2.ogg");
      noteMapperOctave.Add(Pitch.A3, soundDirectory + "/a3.ogg");
      noteMapperOctave.Add(Pitch.A4, soundDirectory + "/a4.ogg");
      noteMapperOctave.Add(Pitch.A5, soundDirectory + "/a5.ogg");
      noteMapperOctave.Add(Pitch.A6, soundDirectory + "/a6.ogg");
      noteMapperOctave.Add(Pitch.A7, soundDirectory + "/a7.ogg");
      if (!noteMapperOctave.Map((NoteMappingBase<string>) this))
        throw new Exception("Failed to create legacy note map!");
    }
  }
}
