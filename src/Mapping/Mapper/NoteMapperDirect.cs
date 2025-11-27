using System;
using Midi;

namespace Instruments.Mapping.Mappers
{

	//
	// Summary:
	//     This object creates mapping that will only map direct entries.
	//     Unassigned entries remain uninitialized (assigned to default value)
	public sealed class NoteMapperDirect<T> : NoteMappingBase<T>.NoteMapperBase
	{
#nullable enable
		private readonly T?[] values;


		public NoteMapperDirect()
		{
			values = new T?[Constants.Note.NoteCount];
		}

		public override bool Add(Pitch pitch, T value)
		{
			int index = (int)pitch;
			values[index] = value;
			return true;
		}
		public override bool Map(NoteMappingBase<T> destination)
		{
			for (int i = 0; i < Constants.Note.NoteCount; ++i)
				Set(destination, i, i, values[i]);

			return true;
		}
		public override void Dispose()
		{
			Array.Clear(values);
		}

	}
}