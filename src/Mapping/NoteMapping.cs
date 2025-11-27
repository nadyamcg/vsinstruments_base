using Midi;

namespace Instruments.Mapping
{
	//
	// Summary:
	//     Container that associates values and relative sound pitch value 
	//     Base of a container that associates value to their note pitch keys.
	public class NoteMapping<T> : NoteMappingBase<T>
	{
		public NoteMapping()
			: base()
		{
		}

		//
		// Summary:
		//     Computes the pitch modulation necessary for the provided note pitch,
		//     considering its source pitch as reference.
		public float GetRelativePitch(Pitch target)
		{
			Pitch source = GetItem(target).Source;
			return NoteMappingUtility.ComputeRelativePitch(target, source);
		}
	}
}