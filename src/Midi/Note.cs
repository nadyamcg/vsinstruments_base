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

  public override readonly string ToString()
  {
    int accidental = this.Accidental;
    string str = accidental switch
    {
      -1 => "b",
      0 => "",
      1 => "#",
      _ => "",
    };
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
      var num = ch switch
      {
        '#' => 1,
        'b' => -1,
        _ => 0,
      };
      accidental = num;
      if (accidental != 0)
        ++position;
    }
    return new Note(upper, accidental);
  }
}
