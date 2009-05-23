// file: SplitArray.cs
// brief: Data structure holding a 'gap' in it for efficient insert/delete operation.
// author: YAMAMOTO Suguru
// update: 2009-05-23
//=========================================================
//#define SPLIT_ARRAY_ENABLE_SANITY_CHECK
//#define SPLIT_ARRAY_ENABLE_TRACE
using System;
using System.Collections;
using System.Collections.Generic;
using Conditional = System.Diagnostics.ConditionalAttribute;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace Sgry.Azuki
{
	/// <summary>
	/// The array structure with 'gap' for efficient insertion/deletion.
	/// </summary>
	class SplitArray<T> : IEnumerable<T>
	{
		#region Fields
		protected T[] _Data = null;
		protected int _GrowSize;
		protected int _Count;
		protected int _GapPos;
		protected int _GapLen;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public SplitArray( int initBufferSize )
			: this( initBufferSize, 0 )
		{}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public SplitArray( int initBufferSize, int growSize )
		{
			_Data = new T[ initBufferSize ];
			_GrowSize = growSize;
			_GapLen = initBufferSize;
			_Count = 0;
			_GapPos = 0;
			
			__set_insanity_data__( 0, initBufferSize );
			__check_sanity__();
		}
		#endregion

		#region ToArray
		/// <summary>
		/// Creates a copy of the content as an array by using given converter.
		/// </summary>
		public S[] ToArray<S>( Converter<T, S> converter )
		{
			S[] array = new S[ _Count ];

			for( int i=0; i<_Count; i++ )
			{
				array[i] = converter( GetAt(i) );
			}

			return array;
		}

		/// <summary>
		/// Creates a copy of the content as an array.
		/// </summary>
		public T[] ToArray()
		{
			T[] array = new T[ _Count ];

			for( int i=0; i<_Count; i++ )
			{
				array[i] = GetAt( i );
			}

			return array;
		}
		#endregion

		#region Count and Capacity
		/// <summary>
		/// Gets count of the elements currently stored.
		/// </summary>
		public int Count
		{
			get{ return _Count; }
		}

		/// <summary>
		/// Gets or sets the size of the internal buffer.
		/// </summary>
		public virtual int Capacity
		{
			get{ return _Data.Length; }
			set{ this.EnsureSpaceForInsertion(value); }
		}
		#endregion

		#region Content Access
		/// <summary>
		/// Gets an element at specified index.
		/// </summary>
		public T GetAt( int index )
		{
			DebugUtl.Assert( 0 <= index && index < _Count, "argument out of range: requested index is "+index+" but _Count is "+_Count );

			if( index < _GapPos )
			{
				return _Data[ index ];
			}
			else
			{
				return _Data[ _GapLen + index ];
			}
		}

		/// <summary>
		/// Gets elements in range [begin, end).
		/// </summary>
		public void GetRange<S>( int begin, int end, ref S[] outBuffer, Converter<T, S> converter )
		{
			DebugUtl.Assert( 0 <= begin && begin <= _Count && begin < end, "argument out of range: requested data at invalid range ["+begin+", "+end+")." );

			int count = end - begin;
			for( int i=0; i<count; i++ )
				outBuffer[i] = converter( GetAt(begin + i) );
		}

		/// <summary>
		/// Gets elements in range [begin, end).
		/// </summary>
		public void GetRange( int begin, int end, ref T[] outBuffer )//, int bufferIndexTo )
		{
			const int bufferIndexTo = 0;
			DebugUtl.Assert( begin < _Count && end <= _Count, "argument out of range: requested range is ["+begin+", "+end+") but _Count is "+_Count+"." );
			DebugUtl.Assert( 0 <= begin && 0 <= end && begin < end, "invalid argument: invalid range ["+begin+", "+end+") was given." );

			int count = end - begin;
			for( int i=0; i<count; i++ )
				outBuffer[bufferIndexTo+i] = GetAt( begin + i );
		}

		/// <summary>
		/// Overwrites an element at specified index.
		/// </summary>
		public void SetAt( T value, int index )
		{
			DebugUtl.Assert( index < _Count );

			if( index < _GapPos )
			{
				_Data[index] = value;
			}
			else
			{
				_Data[ _GapLen + index ] = value;
			}
			__dump__( String.Format("SetAt({0}, {1})", value, index) );
			__check_sanity__();
		}

		/// <summary>
		/// Adds an element.
		/// </summary>
		public void Add( T value )
		{
			Insert( _Count, value );
		}

		/// <summary>
		/// Adds elements.
		/// </summary>
		public void Add( T[] values )
		{
			Insert( _Count, values );
		}

		/// <summary>
		/// Adds elements.
		/// </summary>
		public void Add<S>( S[] values, Converter<S, T> converter )
		{
			Insert( _Count, values, converter );
		}

		/// <summary>
		/// Inserts an element at specified index.
		/// </summary>
		/// <exception cref="ArgumentException">invalid index was given</exception>
		public virtual void Insert( int index, T value )
		{
			// [case 1: Insert(1, "#")]
			// ABCDE___FGHI     (gappos:5, gaplen:3)
			// ABCDEFGHI___     (gappos:9, gaplen:3)
			// ABCDEFGHI_______ (gappos:9, gaplen:7)
			// A_______BCDEFGHI (gappos:1, gaplen:7)
			// A#______BCDEFGHI (gappos:5, gaplen:3)
			DebugUtl.Assert( 0 <= index, "Invalid index was given (index:"+index+")." );
			DebugUtl.Assert( value != null, "Null was given to 'values'." );

			// make sufficient gap for insertion
			EnsureSpaceForInsertion( 1 );
			MoveGapTo( index );

			// insert
			_Data[index] = value;

			// update info
			_Count += 1;
			_GapPos += 1;
			_GapLen -= 1;
			__dump__( String.Format("Insert({0}, {1})", index, value) );
			__check_sanity__();
		}

		/// <summary>
		/// Inserts elements at specified index.
		/// </summary>
		/// <param name="insertIndex">target location of insertion</param>
		/// <param name="values">the elements to be inserted</param>
		/// <param name="converter">type converter to insert data of different type efficiently</param>
		/// <exception cref="ArgumentOutOfRangeException">invalid index was given</exception>
		public virtual void Insert<S>( int insertIndex, S[] values, Converter<S, T> converter )
		{
			// [case 1: Insert(1, "hoge")]
			// ABCDE___FGHI     (gappos:5, gaplen:3)
			// ABCDEFGHI___     (gappos:9, gaplen:3)
			// ABCDEFGHI_______ (gappos:9, gaplen:7)
			// A_______BCDEFGHI (gappos:1, gaplen:7)
			// Ahoge___BCDEFGHI (gappos:5, gaplen:3)
			DebugUtl.Assert( 0 <= insertIndex, "Invalid index was given (insertIndex:"+insertIndex+")." );
			DebugUtl.Assert( values != null, "Null was given to 'values'." );
			DebugUtl.Assert( converter != null, "Null was given to 'converter'." );
			
			// make sufficient gap for insertion
			EnsureSpaceForInsertion( values.Length );
			MoveGapTo( insertIndex );

			// insert
			//Array.Copy( values, 0, _Data, insertIndex, values.Length );
			for( int i=0; i<values.Length; i++ )
			{
				_Data[insertIndex + i] = converter( values[i] );
			}

			// update info
			_Count += values.Length;
			_GapPos += values.Length;
			_GapLen -= values.Length;
			__dump__( String.Format("Insert({0}, {1}...)", insertIndex, values[0]) );
			__check_sanity__();
		}

		/// <summary>
		/// Inserts elements at specified index.
		/// </summary>
		/// <param name="insertIndex">target location of insertion</param>
		/// <param name="values">the elements to be inserted</param>
		/// <exception cref="ArgumentOutOfRangeException">invalid index was given</exception>
		public void Insert( int insertIndex, T[] values )
		{
			DebugUtl.Assert( values != null, "Null was given to 'values'." );

			Insert( insertIndex, values, 0, values.Length );
		}

		/// <summary>
		/// Inserts elements at specified index.
		/// </summary>
		/// <param name="insertIndex">target location of insertion</param>
		/// <param name="values">elements which contains the elements to be inserted</param>
		/// <param name="valueBegin">index of the first elements to be inserted</param>
		/// <param name="valueEnd">index of the end position (one after last elements)</param>
		/// <exception cref="ArgumentOutOfRangeException">invalid index was given</exception>
		public virtual void Insert( int insertIndex, T[] values, int valueBegin, int valueEnd )
		{
			// [case 1: Insert(1, "foobar", 0, 4)]
			// ABCDE___FGHI     (gappos:5, gaplen:3)
			// ABCDEFGHI___     (gappos:9, gaplen:3)
			// ABCDEFGHI_______ (gappos:9, gaplen:7)
			// A_______BCDEFGHI (gappos:1, gaplen:7)
			// Afoob___BCDEFGHI (gappos:5, gaplen:3)
			DebugUtl.Assert( 0 <= insertIndex, "Invalid index was given (insertIndex:"+insertIndex+")." );
			DebugUtl.Assert( values != null, "Null was given to 'values'." );
			
			int insertLen = valueEnd - valueBegin;
			
			// make sufficient gap at insertion point
			EnsureSpaceForInsertion( insertLen );
			MoveGapTo( insertIndex );

			// insert
			Array.Copy( values, valueBegin, _Data, insertIndex, insertLen );

			// update
			_Count += insertLen;
			_GapPos += insertLen;
			_GapLen -= insertLen;
			__dump__( String.Format("Insert({0}, {1}..., {2}, {3})", insertIndex, values[0], valueBegin, valueEnd) );
			__check_sanity__();
		}

		/// <summary>
		/// Overwrites elements from "replaceIndex" with specified range [valueBegin, valueEnd) of values.
		/// </summary>
		public virtual void Replace( int replaceIndex, T[] values, int valueBegin, int valueEnd )
		{
			DebugUtl.Assert( 0 <= replaceIndex, "Invalid index was given (replaceIndex:"+replaceIndex+")." );
			DebugUtl.Assert( values != null );
			DebugUtl.Assert( 0 <= valueEnd && valueEnd <= values.Length, "Invalid index was given (valueEnd:"+valueEnd+")." );
			DebugUtl.Assert( 0 <= valueBegin && valueBegin <= valueEnd, "Invalid index was given (valueBegin:"+valueBegin+", valueEnd:"+valueEnd+")." );
			DebugUtl.Assert( replaceIndex + valueEnd - valueBegin <= _Count, "Invalid indexes were given (<"+replaceIndex+":replaceIndex> + <"+(valueEnd-valueBegin)+":valueEnd - valueBegin> <= <"+_Count+":_Count> ?)." );

			// [case 1: Replace(1, "foobar", 0, 4)]
			// ABC___DEFGHI (gappos:3, gaplen:3)
			// ABCDEF___GHI (gappos:6, gaplen:3)
			// Afooba___GHI (gappos:6, gaplen:3)
			int replaceLen = valueEnd - valueBegin;

			// move gap to the location just after replacing ends
			MoveGapTo( replaceIndex + replaceLen );

			// overwrite elements
			Array.Copy( values, valueBegin, _Data, replaceIndex, replaceLen );
			__dump__( String.Format("Replace({0}, {1}..., {2}, {3})", replaceIndex, values[0], valueBegin, valueEnd) );
			__check_sanity__();
		}

		/// <summary>
		/// Deletes elements at specified range [begin, end).
		/// </summary>
		public virtual void Delete( int begin, int end )
		{
			// [case 1: Delete(4, 5)]
			// A___BCDEFGHI (gappos:1, gaplen:3)
			// ABCD___EFGHI (gappos:4, gaplen:3)
			// ABCD_____GHI (gappos:4, gaplen:5)
			// [case 2: Delete(4, 5)]
			// ABCDEFG___HI (gappos:7, gaplen:3)
			// ABCDEF___GHI (gappos:6, gaplen:3)
			// ABCF_____GHI (gappos:4, gaplen:5)
			DebugUtl.Assert( 0 <= begin , "Invalid range was given ["+begin+", "+end+")" );
			DebugUtl.Assert( 0 <= end , "Invalid range was given ["+begin+", "+end+")" );
			DebugUtl.Assert( begin < end, "invalid range was given ["+begin+", "+end+")" );

			// delete
			int deleteLen = end - begin;
			if( _GapPos < begin )
			{
				// move gap's end to the delete location and expand gap
				MoveGapTo( begin );
				__set_insanity_data__( _GapPos+_GapLen, _GapPos+_GapLen+deleteLen );
				_GapLen += deleteLen;
			}
			else
			{
				// move gap's head next to the delete range and expand gap (backward)
				MoveGapTo( end );
				_GapPos -= deleteLen;
				_GapLen += deleteLen;
				__set_insanity_data__( _GapPos, _GapPos+deleteLen );
			}
			
			// update info
			_Count -= deleteLen;
			__dump__( String.Format("Delete({0}, {1})", begin, end) );
			__check_sanity__();
		}

		/// <summary>
		/// Deletes all elements.
		/// </summary>
		public virtual void Clear()
		{
			_Count = 0;
			_GapPos = 0;
			_GapLen = _Data.Length;

			__dump__( String.Format("Clear()") );
			__set_insanity_data__( 0, _Data.Length );
			__check_sanity__();
		}
		#endregion

		#region Gap Management
		/// <summary>
		/// Moves 'gap' to specified location.
		/// </summary>
		protected void MoveGapTo( int index )
		{
			// [case 1: MoveGapTo(1)]
			// ABCDE___FGHI (gappos:5, gaplen:3, part2pos:3)
			// A___BCDEFGHI (gappos:1, gaplen:3, part2pos:3)
			// [case 2: MoveGapTo(6)]
			// ABCD___EFGHI (gappos:4, gaplen:3)
			// ABCDEF___GHI (gappos:2, gaplen:3)
			DebugUtl.Assert( index <= _Data.Length - _GapLen, String.Format("condition: index({0}) <= _Data.Length({1}) - _GapLen({2})", index, _Data.Length, _GapLen) );

			if( index < _GapPos )
			{
				Array.Copy( _Data, index, _Data, index+_GapLen, Part1Len-index );
				__set_insanity_data__( index, index+_GapLen );
				_GapPos = index;
			}
			else if( _GapPos < index )
			{
				Array.Copy( _Data, _GapPos+_GapLen, _Data, _GapPos, index-_GapPos );
				_GapPos = index;
				__set_insanity_data__( _GapPos, _GapPos+_GapLen );
			}
		}

		/// <summary>
		/// Ensures the buffer is capable to insert data.
		/// </summary>
		void EnsureSpaceForInsertion( int insertLength )
		{
			DebugUtl.Assert( _Data != null );
			DebugUtl.Assert( 0 <= insertLength );

			// to avoid all gaps are filled by inserted data, expand buffer
			if( _GapLen <= insertLength )
			{
				// move gap to the end
				MoveGapTo( _Data.Length - _GapLen );

				// calculate buffer size to be expanded
				int newSize = _Data.Length;
				do
				{
					if( 0 < _GrowSize )
						newSize += _GrowSize;
					else
						newSize *= 2;
				}
				while( newSize < _Count+insertLength );

				// expand buffer
				ResizeArray( ref _Data, newSize );
				__set_insanity_data__( _GapPos, newSize );

				// update info
				_GapLen = newSize - _Count;
			}
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Retrieves an enumerator.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			return new SplitArrayEnumerator<T>( this );
		}

		/// <summary>
		/// Retrieves an enumerator.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new SplitArrayEnumerator<T>( this );
		}

		/// <summary>
		/// Gets an element at specified index.
		/// </summary>
		public T this[int index]
		{
			get{ return this.GetAt(index); }
			set{ this.SetAt(value, index); }
		}

		int Part1Len
		{
			get{ return _GapPos; }
		}

		void ResizeArray( ref T[] array, int newSize )
		{
#			if !PocketPC
			Array.Resize<T>( ref array, newSize );
#			else
			// because there is no Array.Resize<T> method in Compact Framework, resize manually.
			// note that this is not slower than Array.Resize<T>.
			T[] value = new T[ newSize ];
			int minSize = Math.Min( array.Length, newSize );
			
			if( 0 < minSize )
			{
				Array.Copy( array, value, minSize );
			}

			array = value;
#			endif
		}
		#endregion

		#region DebugUtl Utilities (only works when T is System.Char)
		const char INSANITY = '\x65e0'; // 'wu'; a Kanji meaning "nothing" 
		//const char INSANITY = '?';
		
		[Conditional("SPLIT_ARRAY_ENABLE_SANITY_CHECK")]
		void __check_sanity__()
		{
			if( 'a' is T )
			{
				char ch;
				for( int i=_GapPos; i<_GapPos+_GapLen; i++ )
				{
					ch = (char)_Data.GetValue( i );
					if( ch != INSANITY )
					{
						__dump__( "##SANITY CHECK##" );
						DebugUtl.Fail( "SplitArray lost sanity!! (_Data["+i+"] is "+(int)(char)_Data.GetValue(i)+")" );
					}
				}
			}
		}

		[Conditional("SPLIT_ARRAY_ENABLE_SANITY_CHECK")]
		void __set_insanity_data__( int begin, int end )
		{
			if( 'a' is T )
			{
				for( int i=begin; i<end; i++ )
					_Data.SetValue( INSANITY, i );
			}
		}

		[Conditional("SPLIT_ARRAY_ENABLE_TRACE")]
		internal void __dump__( string msgHeader )
		{
			if( 'a' is T )
			{
				int i=0;
				Console.Error.WriteLine( "[{3}] (gappos:{0}, gaplen:{1}, count:{2})", _GapPos, _GapLen, _Count, msgHeader );
				for( ; i<_GapPos; i++ )
					Console.Error.Write( "|{0}", _Data[i] );
				for( ; i<_GapPos+_GapLen; i++ )
					Console.Error.Write( "@{0}", _Data[i] );
				for( ; i<_Data.Length; i++ )
					Console.Error.Write( "|{0}", _Data[i] );
				Console.Error.WriteLine();
				Console.Error.Flush();
			}
		}

#		if SPLIT_ARRAY_ENABLE_TRACE
		/// <summary>
		/// ToString for debug.
		/// </summary>
		public override string ToString()
		{
			if( Count == 0 )
				return String.Empty;

			System.Text.StringBuilder buf = new System.Text.StringBuilder();
			buf.Append( this[0].ToString() );
			for( int i=1; i<Count; i++ )
				buf.Append( " " + this[i].ToString() );
			return buf.ToString();
		}
#		endif
		#endregion
	}

	#region Enumerator
	/// <summary>
	/// The enumerator class for the SplitArray.
	/// </summary>
	class SplitArrayEnumerator<T> : IEnumerator<T>
	{
		SplitArray<T> _Array;
		int _Index = -1;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public SplitArrayEnumerator( SplitArray<T> array )
		{
			_Array = array;
		}

		/// <summary>
		/// Disposes resources.
		/// </summary>
		public void Dispose()
		{}
		#endregion

		#region IEnumerator Interface
		/// <summary>
		/// Retrieves the element at where this enumerator points.
		/// </summary>
		public T Current
		{
			get{ return _Array.GetAt(_Index); }
		}

		/// <summary>
		/// Retrieves the element at where this enumerator points.
		/// </summary>
		object IEnumerator.Current
		{
			get{ return _Array.GetAt(_Index); }
		}

		/// <summary>
		/// Moves location to next.
		/// </summary>
		/// <returns>true if successfuly moved to next</returns>
		public bool MoveNext()
		{
			if( _Array.Count <= _Index+1 )
				return false;

			_Index++;
			return true;
		}

		/// <summary>
		/// Resets location of this enumerator
		/// </summary>
		public void Reset()
		{
			_Index = 0;
		}
		#endregion
	}
	#endregion
}
