// file: TextBuffer.cs
// brief: Specialized SplitArray for char with text search feature without copying content.
// author: YAMAMOTO Suguru
// update: 2009-01-12
//=========================================================
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public TextBuffer( int initGapSize, int growSize )
			: base( initGapSize, growSize )
		{
			_Classes = new SplitArray<CharClass>( initGapSize, growSize );
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

		#region Content Access
		/// <summary>
		/// Inserts an element at specified index.
		/// </summary>
		/// <exception cref="ArgumentException">invalid index was given</exception>
		public override void Insert( int index, char value )
		{
			base.Insert( index, value );
			_Classes.Insert( index, CharClass.Normal );
		}
		
		/// <summary>
		/// Inserts elements at specified index.
		/// </summary>
		/// <param name="insertIndex">target location of insertion</param>
		/// <param name="values">the elements to be inserted</param>
		/// <param name="converter">type converter to insert data of different type efficiently</param>
		/// <exception cref="ArgumentOutOfRangeException">invalid index was given</exception>
		public override void Insert<S>( int insertIndex, S[] values, Converter<S, char> converter )
		{
			base.Insert( insertIndex, values, converter );
			_Classes.Insert( insertIndex, new CharClass[values.Length] );
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
			_Classes.Insert( insertIndex, new CharClass[ valueEnd - valueBegin ] );
		}

		/// <summary>
		/// Overwrites elements from "replaceIndex" with specified range [valueBegin, valueEnd) of values.
		/// </summary>
		public override void Replace( int replaceIndex, char[] values, int valueBegin, int valueEnd )
		{
			base.Replace( replaceIndex, values, valueBegin, valueEnd );
			_Classes.Replace( replaceIndex, new CharClass[values.Length], valueBegin, valueEnd );
		}

		/// <summary>
		/// Deletes elements at specified range [begin, end).
		/// </summary>
		public override void Delete( int begin, int end )
		{
			base.Delete( begin, end );
			_Classes.Delete( begin, end );
		}

		/// <summary>
		/// Deletes all elements.
		/// </summary>
		public override void Clear()
		{
			base.Clear();
			_Classes.Clear();
		}
		#endregion

		#region Text Search
		/// <summary>
		/// Find a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="begin">The search starting position.</param>
		/// <param name="end">The search terminating position.</param>
		/// <param name="comparisonType">Options for string comparison.</param>
		/// <returns>Index of the first occurrence of the pattern if found, or -1 if not found.</returns>
		public int Find( string value, int begin, int end, StringComparison comparisonType )
		{
			// [example]
			// Document.Find( "abc", 1, 10, ? );
			// # |  buffer | value | begin | end |(*)g.m.b.| start
			//---+---------+-------+-------+-----+---------+-------
			// 1 |aab...abc|  ab   |   0   |  3  |aab...abc|   0
			// 2 |aab...abc|  ab   |   0   |  6  |aababc...|   0
			// 3 |aab...abc|  abc  |   2   |  6  |aa...babc|   5
			// 4 |aab...abc|  abc  |   3   |  6  |aab...abc|   6
			// (* g.m.b = gap moved buffer)
			int start, length;
			int foundIndex;
			
			DebugUtl.Assert( value != null );
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
			foundIndex = new String(_Data).IndexOf( value, start, length, comparisonType );
			if( foundIndex == -1 )
			{
				return -1;
			}

			// return found index
			if( start == begin )
				return foundIndex;
			else
				return foundIndex - _GapLen;
		}

		/// <summary>
		/// Find a text pattern by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="begin">The search starting position.</param>
		/// <param name="end">Index of where the search must be terminated</param>
		/// <returns>Index of where the pattern was found or -1 if not found</returns>
		/// <remarks>
		/// This method find a text pattern
		/// expressed by a regular expression in the current content.
		/// The text matching process continues for the index
		/// specified with the <paramref name="end"/> parameter
		/// and does not stop at line ends nor null-characters.
		/// </remarks>
		public int Find( Regex regex, int begin, int end )
		{
			int start, length;
			Match match;
			int foundIndex = -1;

			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( end < begin )
				throw new ArgumentException( "parameter end must be greater than parameter begin." );
			if( _Count < end )
				throw new ArgumentOutOfRangeException( "end must not greater than character count. (end:"+end+", Count:"+_Count+")" );

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
				return -1;
			}
			foundIndex = match.Index;

			// return found index
			if( start == begin )
				return foundIndex;
			else
				return foundIndex - _GapLen;
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
