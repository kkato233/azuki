using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents a segment to describe where a portion of text begins from and where it ends.
	/// </summary>
	/// <remarks>
	///   <para>
	///   A segment is a pair of positions one of which indicates a beginning point of the segment
	///   and the other indicates an ending point. A segment is denoted as
	///   <code>&quot;[X, Y)&quot;</code> where X is index of the starting position of the segment,
	///   and Y is index of the ending. A segment includes the character at the beginning position
	///   and DOES NOT include the character at the ending position. For example, let a document's
	///   content is &quot;foobar&quot; and there is a segment <code>[3, 5)</code>, the segment
	///   includes <code>ba</code>.
	/// </para>
	/// </remarks>
	public struct TextSegment
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public TextSegment( int begin, int end )
			: this()
		{
			Begin = begin;
			End = end;
		}

		/// <summary>
		/// Gets or sets beginning position of the segment.
		/// </summary>
		public int Begin
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets ending position of the segment.
		/// </summary>
		public int End
		{
			get; set;
		}

		/// <summary>
		/// Gets the number of UTF-16 characters in the segment.
		/// </summary>
		public int Length
		{
			get{ return End - Begin; }
		}

		/// <summary>
		/// Gets whether the length of the segment is zero or not.
		/// </summary>
		public bool IsEmpty
		{
			get{ return (Begin == End); }
		}

		/// <summary>
		/// Gets a string that represents the found range by a search.
		/// </summary>
		public override string ToString()
		{
			return String.Format( "[{0}, {1})", Begin, End );
		}
	}
}
