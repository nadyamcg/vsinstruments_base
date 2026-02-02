
using vsinstruments_base;

namespace VSInstrumentsBase.src.GUI;

public interface IFlatListExpandable
{
  bool IsExpanded { get; }

  int Depth { get; }
}
