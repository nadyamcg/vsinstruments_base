using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;


namespace vsinstruments_base.src;

public class RecursiveFileProcessor
{
  public static bool DirectoryExists(string path)
  {
    Debug.WriteLine(Directory.GetCurrentDirectory());
    return Directory.Exists(path);
  }

  public static void ProcessDirectory(
    string targetDirectory,
    string baseDirectory,
    ref List<string> abcFiles)
  {
    foreach (string file in Directory.GetFiles(targetDirectory))
      RecursiveFileProcessor.ProcessFile(file, baseDirectory, ref abcFiles);
    foreach (string directory in Directory.GetDirectories(targetDirectory))
      RecursiveFileProcessor.ProcessDirectory(directory, baseDirectory, ref abcFiles);
  }

  public static void ProcessFile(string path, string baseDirectory, ref List<string> abcFiles)
  {
    string str = path;
    int length = str.Length;
    int startIndex = length - 4;
    if (!(str.Substring(startIndex, length - startIndex) == ".abc"))
      return;
    abcFiles.Add(path.Replace(baseDirectory, ""));
  }

  public static bool ReadFile(string path, ref string fileData)
  {
    if (!File.Exists(path))
      return false;
    FileStream fileStream = File.OpenRead(path);
    byte[] numArray = new byte[((Stream) fileStream).Length];
    UTF8Encoding utF8Encoding = new UTF8Encoding(true);
    while (((Stream) fileStream).Read(numArray, 0, numArray.Length) > 0)
      fileData = utF8Encoding.GetString(numArray);
    return true;
  }
}
