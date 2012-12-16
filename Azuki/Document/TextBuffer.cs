// file: TextBuffer.cs
// brief: Specialized SplitArray for char with text search feature without copying content.
// author: YAMAMOTO Suguru
// update: 2011-09-23
//=========================================================
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// Specialized SplitArray for char with text search feature without copying content.
	/// This is the core data structure of Azuki.
	/// </summary>
	class TextBuffer : SplitArray<Char>
	{
		#region Fields
		SplitArray<CharClass> _Classes;
		RleArray<uint> _MarkingBitMasks;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public TextBuffer( int initGapSize, int growSize )
			: base( initGapSize, growSize )
		{
			_Classes = new SplitArray<CharClass>( initGapSize, growSize );
			_MarkingBitMasks = new RleArray<uint>();
		}
		#endregion

		#region Character Classes
		/// <summary>
		/// Clears class information from all characters.
		/// </summary>
		public void ClearCharClasses()
		{
			for( int i=0; i<_Classes.Count; i++ )
			{
				_Classes[i] = CharClass.Normal;
			}
		}

		/// <summary>
		/// Gets class of the character at specified index.
		/// </summary>
		public CharClass GetCharClassAt( int index )
		{
			return _Classes[ index ];
		}

		/// <summary>
		/// Sets class of the character at specified index.
		/// </summary>
		public void SetCharClassAt( int index, CharClass klass )
		{
			_Classes[ index ] = klass;
		}
		#endregion

		#region Marking
		public RleArray<uint> Marks
		{
			get{ return _MarkingBitMasks; }
		}
		#endregion

		#region Content Access
		/// <summary>
		/// Gets or sets the size of the internal buffer.
		/// </summary>
		/// <exception cref="System.OutOfMemoryException">There is no enough memory to expand buffer.</exception>
		public override int Capacity
		{
			get{ return base.Capacity; }
			set
			{
				base.Capacity = value;
				_Classes.Capacity = value;
				//NO_NEED//_MarkingBitMasks.Xxx = value;
			}
		}

		/// <summary>
		/// Inserts an element at specified index.
		/// </summary>
		/// <exception cref="ArgumentException">invalid index was given</exception>
		public override void Insert( int index, char value )
		{
			base.Insert( index, value );
			_Classes.Insert( index, CharClass.Normal );
			_MarkingBitMasks.Insert( index, 0 );
		}

		/// <summary>
		/// Inserts elements at specified index.
		/// </summary>
		/// <param name="insertIndex">target location of insertion</param>
		/// <param name="values">elements which contains the elements to be inserted</param>
		/// <param name="valueBegin">index of the first elements to be inserted</param>
		/// <param name="valueEnd">index of the end position (one after last elements)</param>
		/// <exception cref="ArgumentOutOfRangeException">invalid index was given</exception>
		public override void Insert( int insertIndex, char[] values, int valueBegin, int valueEnd )
		{
			base.Insert( insertIndex, values, valueBegin, valueEnd );
			_Classes.Insert( insertIndex, new CharClass[valueEnd - valueBegin] );
			_MarkingBitMasks.Insert( insertIndex, 0, valueEnd - valueBegin );
		}

		/// <summary>
		/// Overwrites elements from "replaceIndex" with specified range [valueBegin, valueEnd) of values.
		/// </summary>
		public override void Replace( int replaceIndex, char[] values, int valueBegin, int valueEnd )
		{
			int replaceLen = valueEnd - valueBegin;

			base.Replace( replaceIndex, values, valueBegin, valueEnd );

			_Classes.Replace( replaceIndex, new CharClass[replaceLen], valueBegin, valueEnd );

			for( int i=0; i<replaceLen; i++ )
				_MarkingBitMasks.RemoveAt( replaceIndex + i );
			for( int i=0; i<replaceLen; i++ )
				_MarkingBitMasks.Insert( replaceIndex + i, values[valueBegin+i] );
		}

		/// <summary>
		/// Deletes elements at specified range [begin, end).
		/// </summary>
		public override void RemoveRange( int begin, int end )
		{
			base.RemoveRange( begin, end );
			_Classes.RemoveRange( begin, end );
			for( int i=begin; i<end; i++ )
			{
				_MarkingBitMasks.RemoveAt( begin );
			}
			Debug.Assert( this.Count == _Classes.Count );
			Debug.Assert( this.Count == _MarkingBitMasks.Count );
		}

		/// <summary>
		/// Deletes all elements.
		/// </summary>
		public override void Clear()
		{
			base.Clear();
			_Classes.Clear();
			_MarkingBitMasks.Clear();
		}
		#endregion

		#region Text Search
		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="begin">Begin index of the search range.</param>
		/// <param name="end">End index of the search range.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		public SearchResult FindNext( string value, int begin, int end, bool matchCase )
		{
			// If the gap exists after the search starting position,
			// it must be moved to before the starting position.
			int start, length;
			int foundIndex;
			StringComparison compType;
			
			DebugUtl.Assert( value != null );
			DebugUtl.Assert( 0 <= begin );
			DebugUtl.Assert( begin <= end );
			DebugUtl.Assert( end <= _Count );

			// convert begin/end indexes to start/length indexes
			start = begin;
			length = end - begin;
			if( length <= 0 )
			{
				return null;
			}

			// move the gap if necessary
			if( _GapPos <= begin )
			{
				// the gap exists before search range so the gap is not needed to be moved
				//DO_NOT//MoveGapTo( somewhere );
				start += _GapLen;
			}
			else if( _GapPos < end )
			{
				// the gap exists IN the search range so the gap must be moved
				MoveGapTo( begin );
				start += _GapLen;
			}
			//NO_NEED//else if( end <= _GapPos ) {} // nothing to do in this case

			// find
			compType = (matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
			foundIndex = new String(_Data).IndexOf( value, start, length, compType );
			if( foundIndex == -1 )
			{
				return null;
			}

			// calculate found index not in gapped buffer but in content
			if( _GapPos < end )
			{
				foundIndex -= _GapLen;
			}

			// return found index
			return new SearchResult( foundIndex, foundIndex + value.Length );
		}

		/// <summary>
		/// Finds previous occurrence of a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		public SearchResult FindPrev( string value, int begin, int end, bool matchCase )
		{
			// If the gap exists before the search starting position,
			// it must be moved to after the starting position.
			int start, length;
			int foundIndex;
			StringComparison compType;
			
			DebugUtl.Assert( value != null );
			DebugUtl.Assert( begin <= end );
			DebugUtl.Assert( end <= _Count );

			// if empty string is the value to search, just return search start index
			if( value.Length == 0 )
			{
				return new SearchResult( end, end );
			}

			// convert begin/end indexes to start/length indexes
			start = end - 1;
			length = end - begin;
			if( start < 0 || length <= 0 )
			{
				return null;
			}

			// calculate start index in the gapped buffer
			if( _GapPos < begin )
			{
				// the gap exists before search range so the gap is not needed to be moved
				start += _GapLen;
			}
			else if( _GapPos < end )
			{
				// the gap exists in the search range so the gap must be moved
				MoveGapTo( end );
			}
			//NO_NEED//else if( end <= _GapPos ) {} // nothing to do in this case

			// find
			compType = (matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
			foundIndex = new String(_Data).LastIndexOf( value, start, length, compType );
			if( foundIndex == -1 )
			{
				return null;
			}

			// calculate found index not in gapped buffer but in content
			if( _GapPos < end )
			{
				foundIndex -= _GapLen;
			}

			// return found index
			return new SearchResult( foundIndex, foundIndex + value.Length );
		}

		/// <summary>
		/// Find a text pattern by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="begin">The search starting position.</param>
		/// <param name="end">Index of where the search must be terminated</param>
		/// <returns></returns>
		/// <remarks>
		/// This method find a text pattern
		/// expressed by a regular expression in the current content.
		/// The text matching process continues for the index
		/// specified with the <paramref name="end"/> parameter
		/// and does not stop at line ends nor null-characters.
		/// </remarks>
		public SearchResult FindNext( Regex regex, int begin, int end )
		{
			int start, length;
			Match match;

			DebugUtl.Assert( regex != null );
			DebugUtl.Assert( begin <= end );
			DebugUtl.Assert( end <= _Count );

			// in any cases, search length is "end - begin".
			length = end - begin;

			// determine where the gap should be moved to
			if( end <= _GapPos )
			{
				// search must stop before reaching the gap so there is no need to move gap
				start = begin;
			}
			else
			{
				// search may not stop before reaching to the gap
				// so move gap to ensure there is no gap in the search range
				start = begin + _GapLen;
				MoveGapTo( begin );
			}

			// find
			match = regex.Match( new String(_Data), start, length );
			if( match.Success == false )
			{
				return null;
			}

			// return found index
			if( start == begin )
				return new SearchResult( match.Index, match.Index + match.Length );
			else
				return new SearchResult( match.Index - _GapLen, match.Index - _GapLen + match.Length );
		}

		public SearchResult FindPrev( Regex regex, int begin, int end )
		{
			int start, length;
			Match match;

			DebugUtl.Assert( regex != null );
			DebugUtl.Assert( begin <= end );
			DebugUtl.Assert( end <= _Count );
			DebugUtl.Assert( (regex.Options & RegexOptions.RightToLeft) != 0 );

			// convert begin/end indexes to start/length
			length = end - begin;
			if( end <= _GapPos )
			{
				// search must stop before reaching the gap so there is no need to move gap
				start = begin;
			}
			else
			{
				// search may not stop before reaching to the gap
				// so move gap to ensure there is no gap in the search range
				start = begin + _GapLen;
				MoveGapTo( begin );
			}

			// find
			match = regex.Match( new String(_Data), start, length );
			if( match.Success == false )
			{
				return null;
			}

			// return found index
			if( start == begin )
				return new SearchResult( match.Index, match.Index + match.Length );
			else
				return new SearchResult( match.Index - _GapLen, match.Index - _GapLen + match.Length );
		}
		#endregion

		#region Utilities
#		if DEBUG
		/// <summary>
		/// ToString for Debug.
		/// </summary>
		public override string ToString()
		{
			System.Text.StringBuilder buf = new System.Text.StringBuilder( this.Count );
			for( int i=0; i<Count; i++ )
			{
				buf.Append( this[i] );
			}
			return buf.ToString();
		}
#		endif
		#endregion
	}
}
