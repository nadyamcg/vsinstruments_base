using System;
using Midi;

namespace Instruments.Mapping.Mappers
{
	//
	// Summary:
	//     This object creates mapping that will replace empty items with
	//     nearest non-empty items based on their distance.
	public sealed class NoteMapperDistance<T> : NoteMappingBase<T>.NoteMapperBase
	{
#nullable enable
		private readonly T?[] values;

		public NoteMapperDistance()
		{
			// Pre-allocate space for all items, null entries
			// represent missing items that will be remapped
			// to existing entries with modulated pitch
			values = new T?[Constants.Note.NoteCount];
		}

		public override bool Add(Pitch pitch, T value)
		{
			int index = (int)pitch;
			if (values[index] != null)
				return false;

			values[index] = value;
			return true;
		}
		public override bool Map(NoteMappingBase<T> destination)
		{
			// At least a single value is required to create a note map, albeit it
			// will likely be of low quality due to large pitch modulation values.
			int numValues = countValues();
			if (numValues <= 0)
			{
				return false;
			}

			int countValues()
			{
				int numValues = 0;
				for (int i = 0; i < values.Length; ++i)
					if (values[i] != null)
						++numValues;

				return numValues;
			}
			int? findUpperBound(int? start)
			{
				if (start == null) return null;

				for (int i = start.Value + 1; i < Constants.Note.NoteCount; ++i)
				{
					if (values[i] != null)
						return i;
				}
				return null;
			}
			void fill(int? lo, int? hi, int length = Constants.Note.NoteCount)
			{
				if (!lo.HasValue && hi.HasValue)
				{
					T? item = values[hi.Value];
					for (int i = 0; i <= hi.Value; ++i)
						Set(destination, i, hi.Value, item);
				}
				else if (!hi.HasValue && lo.HasValue)
				{
					T? item = values[lo.Value];
					for (int i = lo.Value; i < length; ++i)
						Set(destination, i, lo.Value, item);
				}
				else if (hi.HasValue && lo.HasValue)
				{
					for (int i = lo.Value; i <= hi.Value; ++i)
					{
						int best;
						int loDist = i - lo.Value;
						int hiDist = hi.Value - i;
						if (hiDist < loDist) // TODO: Figure out whether higher or lower sample actually sounds better?
						{
							best = hi.Value;
						}
						else
						{
							best = lo.Value;
						}

						Set(destination, i, best, values[best]);
					}
				}
			}

			// Find the initial upper bound (first non-null item),
			// there is no lower bound in this situation (yet)
			int? lo = null;
			int? hi = findUpperBound(0);

			// With no lower boundary set, the fill function
			// fills the map starting at 0th position with
			// sample from hi up to its position.
			fill(lo, hi);

			// Until all values are processed, promote the upper
			// bound to the lower bound and find new upper bound.
			do
			{
				lo = hi;
				hi = findUpperBound(lo);

				// Fill the map with values between lo and hi,
				// using whichever sample is closer.
				fill(lo, hi);
				// With no upper boundary set, the fill function
				// fills the map starting at lo-th position with
				// sample from lo up to the map's capacity.

			} while (hi.HasValue);

			// All entries are assigned, map is complete.
			return true;

		}
		public override void Dispose()
		{
			Array.Clear(values);
		}
	}
#nullable restore
}