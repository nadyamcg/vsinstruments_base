namespace VSInstrumentsBase.src;

public class Definitions
{
  private string bandName = "";
  private static Definitions _instance;
  private bool isPlaying = false;

  private Definitions()
  {
  }

  public static Definitions Instance
  {
    get
    {
      return Definitions._instance != null ? Definitions._instance : (Definitions._instance = new Definitions());
    }
  }

  public void SetBandName(string bn) => this.bandName = bn;

  public string GetBandName() => this.bandName;

  public void SetIsPlaying(bool toggle) => this.isPlaying = toggle;

  public bool IsPlaying() => this.isPlaying;

  public static void Reset()
  {
  }
}
