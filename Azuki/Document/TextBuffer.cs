// file: TextBuffer.cs
// brief: Specialized SplitArray for char with text search feature without copying content.
// author: YAMAMOTO Suguru
// update: 2008-10-31
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
	/// <seealso cref="SplitArray&lt;T&gt;"/>
	public class TextBuffer : SplitArray<Char>
	{
		#region Fields
		SplitArray<CharClass> _Classes;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public TextBuffer()
			: this( 256, 128 )
		{}

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
		/// Find text pattern by regular expression.
		/// </summary>
		/// <param name="regex">Regex object to be used</param>
		/// <param name="begin">Index of where to start the search</param>
		/// <param name="end">Index of where the search must be terminated</param>
		/// <returns>Index of where the pattern was found or -1 if not found</returns>
		public int Find( Regex regex, int begin, int end )
		{
			int start, length;
			int foundIndex;

			if( end < begin )
				throw new ArgumentException( "endIndex must be greater than startIndex." );

			// move gap to safe position to execute regex search
			if( begin <= _GapPos )
			{
				MoveGapTo( begin );
			}

			// prepare indexes
			start = begin + _GapLen;
			length = end - begin;

			// find
			foundIndex = regex.Match( new String(_Data), start, length ).Index;
			if( foundIndex == -1 )
			{
				return -1;
			}

			return foundIndex - _GapLen;
		}

		/// <summary>
		/// Find text pattern.
		/// </summary>
		/// <param name="value">String to find</param>
		/// <param name="begin">Index of where to start the search</param>
		/// <param name="end">Index of where the search must be terminated</param>
		/// <returns>Index of where the pattern was found or -1 if not found</returns>
		public int Find( string value, int begin, int end )
		{
			return Find( value, begin, end, StringComparison.InvariantCulture );
		}

		/// <summary>
		/// Find text pattern.
		/// </summary>
		/// <param name="value">String to find</param>
		/// <param name="begin">Index of where to start the search</param>
		/// <param name="end">Index of where the search must be terminated</param>
		/// <param name="comparisonType">String comparison option to be used</param>
		/// <returns>Index of where the pattern was found or -1 if not found</returns>
		public int Find( string value, int begin, int end, StringComparison comparisonType )
		{
			int start, length;
			int foundIndex;

			if( end < begin )
				throw new ArgumentException( "endIndex must be greater than startIndex." );
			
			// move gap to safe position to execute regex search
			if( begin < _GapPos && _GapPos <= end )
			{
				MoveGapTo( begin );
			}
			
			// prepare indexes
			start = begin + _GapLen;
			length = end - begin;

			// find
			foundIndex = new String(_Data).IndexOf( value, start, length, comparisonType );
			if( foundIndex == -1 )
			{
				return -1;
			}

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
