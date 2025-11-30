// Decompiled with JetBrains decompiler
// Type: Midi.PitchExtensions
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

#nullable disable
namespace VSInstrumentsBase.src.Midi;

public static class PitchExtensions
{
  private static readonly Note[] SharpsTable = new Note[12]
  {
    new Note('C', 0),
    new Note('C', 1),
    new Note('D', 0),
    new Note('D', 1),
    new Note('E', 0),
    new Note('F', 0),
    new Note('F', 1),
    new Note('G', 0),
    new Note('G', 1),
    new Note('A', 0),
    new Note('A', 1),
    new Note('B', 0)
  };
  private static readonly Note[] FlatsTable = new Note[12]
  {
    new Note('C', 0),
    new Note('D', -1),
    new Note('D', 0),
    new Note('E', -1),
    new Note('E', 0),
    new Note('F', 0),
    new Note('G', -1),
    new Note('G', 0),
    new Note('A', -1),
    new Note('A', 0),
    new Note('B', -1),
    new Note('B', 0)
  };

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
    if (true)
      ;
    int num;
    switch (letter)
    {
      case 'A':
        num = 9;
        break;
      case 'B':
        num = 11;
        break;
      case 'C':
        num = 0;
        break;
      case 'D':
        num = 2;
        break;
      case 'E':
        num = 4;
        break;
      case 'F':
        num = 5;
        break;
      case 'G':
        num = 7;
        break;
      default:
        num = 0;
        break;
    }
    if (true)
      ;
    return num + note.Accidental + (octave + 1) * 12;
  }
}
