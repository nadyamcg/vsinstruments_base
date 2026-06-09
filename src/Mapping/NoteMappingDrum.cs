using VSInstrumentsBase.src.Mapping.Mappers;
using VSInstrumentsBase.src.Midi;
using System;
using System.IO;


namespace VSInstrumentsBase.src.Mapping;

// per-note mapping for the drum instrument and any other non-pitched
// instrument whose samples are named <midi-note>.ogg. each pitch in the
// 26..88 range loads its own sample at native pitch.
public class NoteMappingDrum : NoteMapping<string>
{
  public NoteMappingDrum(string soundDirectory)
  {
    if (soundDirectory.EndsWith(Path.DirectorySeparatorChar))
      soundDirectory = soundDirectory[..^1];
    using NoteMapperDistance<string> mapper = new();
    for (int note = 26; note <= 88; ++note)
      mapper.Add((Pitch) note, $"{soundDirectory}/{note}.ogg");
    if (!mapper.Map((NoteMappingBase<string>) this))
      throw new Exception("Failed to create drum note map!");
  }
}
