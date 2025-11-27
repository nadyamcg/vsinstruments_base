using System;
using Midi;

namespace Instruments.Mapping
{
	//
	// Summary:
	//     Base of a container that associates value to their note pitch keys.
	public abstract class NoteMappingBase<T>
	{
		//
		// Summary:
		//     Contains single value associated to a pitch and stores its source pitch,
		//     i.e. the location it was mapped from if not mapped directly.
		protected struct Item
		{
			//
			// Summary:
			//     The actual value this entry represent on its position in the map.
			public T Value;
			//
			// Summary:
			//     The source defines the origin of this item as such that if its source
			//     and actual position in the map does not equal, it was remapped from
			//     the source and should take it into acount when e.g. determining pitch.
			public Pitch Source;

			public Item(T value, Pitch source)
			{
				Value = value;
				Source = source;
			}
		}
		//
		// Summary:
		//     Individual mapped objects contained in this map.
		private readonly Item[] _entries;
		//
		// Summary:
		//     Creates new empty note mapping. Use NoteMapper implementations to assing values to the map.
		public NoteMappingBase()
		{
			_entries = new Item[Constants.Note.NoteCount];
		}

		protected void Set(Pitch pitch, Pitch samplePitch, T value)
		{
			_entries[(int)pitch] = new Item(value, samplePitch);
		}

		protected void Clear()
		{
			Array.Clear(_entries);
		}

		protected Item GetItem(Pitch pitch)
		{
			return _entries[(int)pitch];
		}
		//
		// Summary:
		//     Returns the value mapped to the provided pitch.
		public T GetValue(Pitch pitch)
		{
			return _entries[(int)pitch].Value;
		}

		//
		// Summary:
		//     Base for object responsible for building the map from provided cache of items.
		public abstract class NoteMapperBase : IDisposable
		{
			//
			// Summary:
			//     Assign a mapping. Multiple mappings can be assigned prior to the map generation.
			public abstract bool Add(Pitch pitch, T value);
			//
			// Summary:
			//     Finalizes the mapping process by converting and applying the stored mappings to the destination map.
			public abstract bool Map(NoteMappingBase<T> destination);
			//
			// Summary:
			//     Releases all resources and disposes of this mapper.
			public abstract void Dispose();

			protected static void Set(NoteMappingBase<T> destination, int valueIndex, int sampleIndex, T value)
			{
				Pitch current = (Pitch)valueIndex;
				Pitch sample = (Pitch)sampleIndex;
				destination.Set(current, sample, value);
			}

			protected static void Clear(NoteMappingBase<T> destination)
			{
				destination.Clear();
			}
		}
	}

	//
	// Summary:
	//     Base interface for objects that implement a note-object mapping relation.
	public static class NoteMappingUtility
	{
		//
		// Summary:
		//     Computes the pitch modulation necessary for the provided note pitch,
		//     considering its source pitch as reference.
		public static float ComputeRelativePitch(Pitch target, Pitch source)
		{
			int semitoneDiff = (int)target - (int)source;
			return (float)Math.Pow(2, semitoneDiff / 12.0);
		}

		//
		// Summary:
		//     Computes the pitch modulation necessary for the provided note pitch,
		//     considering its source pitch as reference. Pitch extension method.
		public static float RelativePitch(this Pitch source, Pitch target)
		{
			return ComputeRelativePitch(target, source);
		}
	}
}