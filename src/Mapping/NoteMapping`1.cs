using VSInstrumentsBase.src.Midi;


namespace VSInstrumentsBase.src.Mapping;

public class NoteMapping<T> : NoteMappingBase<T>
{
  public float GetRelativePitch(Pitch target)
  {
    Pitch source = this.GetItem(target).Source;
    return NoteMappingUtility.ComputeRelativePitch(target, source);
  }
}
