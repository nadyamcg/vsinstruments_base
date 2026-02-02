
namespace VSInstrumentsBase.src.Midi;

public static class PitchExtensions
{
  private static readonly Note[] SharpsTable =
  [
    new('C', 0),
    new('C', 1),
    new('D', 0),
    new('D', 1),
    new('E', 0),
    new('F', 0),
    new('F', 1),
    new('G', 0),
    new('G', 1),
    new('A', 0),
    new('A', 1),
    new('B', 0)
  ];
  private static readonly Note[] FlatsTable =
  [
    new('C', 0),
    new('D', -1),
    new('D', 0),
    new('E', -1),
    new('E', 0),
    new('F', 0),
    new('G', -1),
    new('G', 0),
    new('A', -1),
    new('A', 0),
    new('B', -1),
    new('B', 0)
  ];

  public static Note NotePreferringSharps(this Pitch pitch)
  {
    return PitchExtensions.SharpsTable[(int) pitch % 12];
  }

  public static Note NotePreferringFlats(this Pitch pitch)
  {
    return PitchExtensions.FlatsTable[(int) pitch % 12];
  }

  public static int RelativePitch(this Pitch pitch, Pitch reference) => pitch - reference;

  public static int ToMidiNote(this Pitch pitch) => (int) pitch;

  public static int PositionInOctave(this Pitch pitch) => (int) pitch % 12;

  public static int PitchInOctave(this Note note, int octave)
  {
    char letter = note.Letter;
    var num = letter switch
    {
      'A' => 9,
      'B' => 11,
      'C' => 0,
      'D' => 2,
      'E' => 4,
      'F' => 5,
      'G' => 7,
      _ => 0,
    };
    return num + note.Accidental + (octave + 1) * 12;
  }
}
