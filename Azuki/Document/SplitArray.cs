// file: SplitArray.cs
// brief: Data structure holding a 'gap' in it for efficient insert/delete operation.
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
	class SplitArray<T> : IEnumerable<T>, IList<T>
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
		/// Creates a copy of the content as an array.
		/// </summary>
		public T[] ToArray()
		{
			T[] array = new T[ _Count ];
			CopyTo( 0, _Count, array, 0 );
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
		/// <exception cref="System.OutOfMemoryException">There is no enough memory to expand buffer.</exception>
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
		/// Copies items to other array object.
		/// </summary>
		public void CopyTo( T[] array )
		{
			CopyTo( array, 0 );
		}

		/// <summary>
		/// Copies items to other array object.
		/// </summary>
		public void CopyTo( T[] array, int arrayIndex )
		{
			CopyTo( 0, Count, array, arrayIndex );
		}

		/// <summary>
		/// Copies items to other array object.
		/// </summary>
		public void CopyTo( int begin, int end, T[] array )
		{
			CopyTo( begin, end, array, 0 );
		}

		/// <summary>
		/// Copies items to other array object.
		/// </summary>
		public void CopyTo( int begin, int end, T[] array, int arrayIndex )
		{
			DebugUtl.Assert( 0 <= begin && begin < end && end <= Count, "invalid range ["+begin+", "+end+") was given. (Count:"+Count+")" );
			DebugUtl.Assert( array != null, "parameter 'array' must not be null." );
			DebugUtl.Assert( 0 <= arrayIndex && arrayIndex < array.Length, "parameter 'arrayIndex' is out of range. (arrayIndex:"+arrayIndex+", array.Length:"+array.Length+")" );
			DebugUtl.Assert( end - begin <= array.Length - arrayIndex, "size of the given array is not sufficient. (begin:"+begin+", end:"+end+", arrayIndex:"+arrayIndex+", array.Length:"+array.Length+")" );

			int length = end - begin;
			if( length < _GapPos )
			{
				Array.Copy( _Data, begin, array, arrayIndex, length );
			}
			else
			{
				Array.Copy( _Data, begin, array, arrayIndex, _GapPos );
				Array.Copy( _Data, _GapPos + _GapLen, array, arrayIndex + _GapPos,
							length - _GapPos );
			}
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
		public void Add( params T[] values )
		{
			Insert( _Count, values );
		}

		/// <summary>
		/// Inserts an element at specified index.
		/// </summary>
		/// <exception cref="ArgumentException">invalid index was given</exception>
		public virtual void Insert( int insertIndex, T value )
		{
			// [case 1: Insert(1, "#")]
			// ABCDE___FGHI     (gappos:5, gaplen:3)
			// ABCDEFGHI___     (gappos:9, gaplen:3)
			// ABCDEFGHI_______ (gappos:9, gaplen:7)
			// A_______BCDEFGHI (gappos:1, gaplen:7)
			// A#______BCDEFGHI (gappos:5, gaplen:3)
			DebugUtl.Assert( 0 <= insertIndex, "Invalid index was given (insertIndex:"+insertIndex+")." );
			DebugUtl.Assert( value != null, "Null was given to 'values'." );

			// make sufficient gap for insertion
			EnsureSpaceForInsertion( 1 );
			MoveGapTo( insertIndex );

			// insert
			_Data[insertIndex] = value;

			// update info
			_Count += 1;
			_GapPos += 1;
			_GapLen -= 1;
			__dump__( String.Format("Insert({0}, {1})", insertIndex, value) );
			__check_sanity__();
		}

		/// <summary>
		/// Inserts elements at specified index.
		/// </summary>
		/// <param name="insertIndex">target location of insertion</param>
		/// <param name="values">the elements to be inserted</param>
		/// <exception cref="ArgumentOutOfRangeException">invalid index was given</exception>
		public void Insert( int insertIndex, params T[] values )
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
		/// Removes an element.
		/// </summary>
		public bool Remove( T item )
		{
			DebugUtl.Assert( item != null, "parameter 'item' must not be null." );

			// find the item
			int index = IndexOf( item );
			if( index < 0 )
			{
				return false;
			}

			// remove the item
			RemoveAt( index );
			return true;
		}

		/// <summary>
		/// Removes an element at specified range [index, index+1).
		/// </summary>
		public void RemoveAt( int index )
		{
			DebugUtl.Assert( 0 <= index && index < Count, "parameter 'index' is out of range. (index:"+index+", Count:"+Count+")" );
			RemoveRange( index, index+1 );
		}

		/// <summary>
		/// Removes elements at specified range [begin, end).
		/// </summary>
		public virtual void RemoveRange( int begin, int end )
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
			__dump__( String.Format("RemoveRange({0}, {1})", begin, end) );
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

		/// <summary>
		/// Finds the specified item and returns found index, or -1 if not found.
		/// </summary>
		public int IndexOf( T item )
		{
			for( int i=0; i<Count; i++ )
			{
				if( this[i].Equals(item) )
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Search for an item using binary-search algorithm
		/// (using Comparer&lt;T&gt;.Default as a comparer.)
		/// </summary>
		/// <returns>
		/// The index of the 'item' if found, otherwise bit-reversed value of
		/// the index of the first element which was greater than the 'item.'
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Comparer&lt;T&gt;.Default does not know how to compare elements of
		/// type T.
		/// </exception>
		public int BinarySearch( T item )
		{
			return BinarySearch( item, Comparer<T>.Default.Compare );
		}

		/// <summary>
		/// Search for an item using binary-search algorithm.
		/// </summary>
		/// <returns>
		/// The index of the 'item' if found, otherwise bit-reversed value of
		/// the index of the first element which was greater than the 'item.'
		/// </returns>
		public int BinarySearch( T item, Comparison<T> compare )
		{
			DebugUtl.Assert( compare != null );

			if( Count == 0 )
				return ~(0);

			int left = 0;
			int right = Count;
			int middle;
			for(;;)
			{
				middle = left + ( (right - left) >> 1 );
				int result = compare( GetAt(middle), item );
				if( 0 < result )
				{
					if( right == middle )
						return ~(middle);
					right = middle;
				}
				else if( result < 0 )
				{
					if( left == middle )
						return ~(middle + 1);
					left = middle;
				}
				else
				{
					return middle;
				}
			}
		}

		/// <summary>
		/// Distinguishes whether specified item exists in this collection or not.
		/// </summary>
		public bool Contains( T item )
		{
			return ( 0 <= IndexOf(item) );
		}
		#endregion

		#region Others
		/// <summary>
		/// (Returns false always.)
		/// </summary>
		public bool IsReadOnly
		{
			get{ return false; }
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
		/// <exception cref="System.OutOfMemoryException">There is no enough memory to expand buffer.</exception>
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

		/// <exception cref="System.OutOfMemoryException">There is no enough memory to expand buffer.</exception>
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

		/// <summary>
		/// ToString for debug.
		/// </summary>
		public override string ToString()
		{
			System.Text.StringBuilder buf;
			int count;
			
			if( Count == 0 )
				return String.Empty;

			buf = new System.Text.StringBuilder();
			count = Math.Min( 16, Count );
			buf.Append( this[0].ToString() );
			for( int i=1; i<count; i++ )
				buf.Append( " " + this[i].ToString() );
			return buf.ToString();
		}
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
