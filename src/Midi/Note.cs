using System;
using System.Diagnostics;


namespace VSInstrumentsBase.src.Midi;

public struct Note(char letter, int accidental)
{
  public const int Natural = 0;
  public const int Sharp = 1;
  public const int Flat = -1;

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public char Letter { get; } = char.ToUpper(letter);

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  public int Accidental { get; } = accidental;

  public override string ToString()
  {
    int accidental = this.Accidental;
    if (true)
      ;
    string str;
    switch (accidental)
    {
      case -1:
        str = "b";
        break;
      case 0:
        str = "";
        break;
      case 1:
        str = "#";
        break;
      default:
        str = "";
        break;
    }
    if (true)
      ;
    return $"{this.Letter}{str}";
  }

  public static Note ParseNote(string noteStr, ref int position)
  {
    if (string.IsNullOrEmpty(noteStr))
      throw new ArgumentException("Note string cannot be null or empty");
    if (position >= noteStr.Length)
      throw new ArgumentException("Position is beyond string length");
    char upper = char.ToUpper(noteStr[position]);
    ++position;
    int accidental = 0;
    if (position < noteStr.Length)
    {
      char ch = noteStr[position];
      if (true)
        ;
      int num;
      switch (ch)
      {
        case '#':
          num = 1;
          break;
        case 'b':
          num = -1;
          break;
        default:
          num = 0;
          break;
      }
      if (true)
        ;
      accidental = num;
      if (accidental != 0)
        ++position;
    }
    return new Note(upper, accidental);
  }
}
