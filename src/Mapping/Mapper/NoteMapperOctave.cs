using System;
using Midi;

namespace Instruments.Mapping.Mappers
{
	//
	// Summary:
	//     This object creates mapping that will use a single pitch sample
	//     from each provided octave to fill the rest of the octave.
	//     Octaves with no samples will remain unassigned.
	public sealed class NoteMapperOctave<T> : NoteMappingBase<T>.NoteMapperBase
	{
#nullable enable
		private readonly T?[] _values;
		private readonly Pitch _pitch;

		public NoteMapperOctave(Pitch pitch)
		{
			// Pre-allocate space for all items, null entries
			// represent missing items that will be remapped
			// to existing entries with modulated pitch
			_values = new T?[Constants.Note.NoteCount];
			_pitch = pitch;
		}

		public override bool Add(Pitch pitch, T value)
		{
			if (pitch.PositionInOctave() != _pitch.PositionInOctave())
				return false;

			int index = (int)pitch;
			if (_values[index] != null)
				return false;

			_values[index] = value;
			return true;
		}
		public override bool Map(NoteMappingBase<T> destination)
		{
			int note = _pitch.PositionInOctave();
			// This mapping relies on a single note being selected as
			// reference. Individual samples are provided only for the
			// same note in different octave. Start by iterating through
			// octaves and searching for the given sample pitch.
			for (int i = 0; i < Constants.Note.OctaveCount; ++i)
			{
				int lo = i * Constants.Note.OctaveLength;
				int sample = lo + note;
				T? value = _values[sample];
				// When no sample is found in this octave, leave the
				// entire octave empty and proceed to next one.
				if (value == null)
					continue;

				int hi = lo + Constants.Note.OctaveLength;
				// With the sample present and lower and upper bounds
				// determined, proceed to fill the entire octave
				// with the provided sample.
				for (int j = lo; j < hi; ++j)
				{
					Set(destination, j, sample, value);
				}
			}

			return true;
		}
		public override void Dispose()
		{
			Array.Clear(_values);
		}
	}
#nullable restore
}