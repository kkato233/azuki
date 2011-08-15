// file: RleArray.cs
// brief: Array of items which compresses contents with RLE compression method.
// author: YAMAMOTO Suguru
// update: 2011-08-15
//=========================================================
using System;
using System.Collections;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;
using StringBuilder = System.Text.StringBuilder;

namespace Sgry.Azuki
{
	/// <summary>
	/// Array of items which compresses contents with RLE compression method.
	/// </summary>
	class RleArray<T> : IList<T>
	{
#		if DEBUG
		internal SplitArray<Node> _Nodes;
#		else
		SplitArray<Node> _Nodes;
#		endif
		int _TotalCount = 0;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public RleArray()
			: this( 32 )
		{}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public RleArray( int initCapacity )
		{
			_Nodes = new SplitArray<Node>( Math.Max(32, initCapacity) );
		}
		
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public RleArray( IEnumerable<T> values )
			: this( 32 )
		{
			foreach( T value in values )
			{
				Add( value );
			}
		}
		#endregion

		#region IList<T>
		/// <summary>
		/// Searches this array for the specified item and returns the index of the found one.
		/// </summary>
		/// <returns>
		/// The index of the specified item found firstly, othewise -1.
		/// </returns>
		public int IndexOf( T item )
		{
			int sum = 0;

			for( int i=0; i<_Nodes.Count; i++ )
			{
				Node node = _Nodes[i];
				if( node.Value.Equals(item) )
				{
					// target item is in this RLE node
					return sum;
				}
				sum += node.Length;
			}

			return -1;
		}

		/// <summary>
		/// Insert the specified values at specified index.
		/// </summary>
		/// <exception cref='ArgumentOutOfRangeException'>
		/// Parameter <paramref name="index"/> is out of valid range.
		/// </exception>
		public void Insert( int index, IEnumerable<T> values )
		{
			int i = 0;

			foreach( T value in values )
			{
				Insert( index + i, value );
			}
		}

		/// <summary>
		/// Insert the specified value at specified index.
		/// </summary>
		/// <exception cref='ArgumentOutOfRangeException'>
		/// Parameter <paramref name="index"/> is out of valid range.
		/// </exception>
		public void Insert( int index, T value )
		{
			if( index < 0 || _TotalCount < index )
				throw new ArgumentOutOfRangeException( "index" );

			int nodeIndex;
			int indexInNode;
			Node prevNode, theNode;

			// find the node which contains the insertion point
			FindNodeIndex( index, out nodeIndex, out indexInNode );
			if( nodeIndex < 0 || indexInNode < 0 )
			{
				// there is no nodes following.
				Debug.Assert( index == _TotalCount );
				if( 0 < _Nodes.Count )
				{
					// 
					prevNode = _Nodes[ _Nodes.Count - 1 ];
					if( value.Equals(prevNode.Value) )
					{
						prevNode.Length++;
						_TotalCount++;
					}
					else
					{
						_Nodes.Add( new Node(1, value) );
						_TotalCount++;
					}
				}
				else
				{
					// Simply add a new node since the array was empty.
					_Nodes.Add( new Node(1, value) );
					_TotalCount++;
				}

				return;
			}

			// get the node at one previous position
			theNode = _Nodes[ nodeIndex ];
			prevNode = null;
			if( 0 < nodeIndex )
			{
				prevNode = _Nodes[ nodeIndex-1 ];
			}

			// Insert the value
			if( indexInNode == 0 )
			{
				// Take care of both left and right boundaries
				if( value.Equals(theNode.Value) )
				{
					theNode.Length++;
					_TotalCount++;
				}
				else if( prevNode != null && value.Equals(prevNode.Value) )
				{
					prevNode.Length++;
					_TotalCount++;
				}
				else
				{
					_Nodes.Insert( nodeIndex, new Node(1, value) );
					_TotalCount++;
				}
			}
			else
			{
				// Take care of both right boundary
				if( value.Equals(theNode.Value) )
				{
					theNode.Length++;
					_TotalCount++;
				}
				else
				{
					int orgNodeLen = theNode.Length;

					theNode.Length = indexInNode;
					_Nodes.Insert( nodeIndex+1, new Node(orgNodeLen - indexInNode, theNode.Value) );
					_Nodes.Insert( nodeIndex+1, new Node(1, value) );
					_TotalCount++;
				}
			}
		}
		
		/// <summary>
		/// Removes an item at specified index.
		/// </summary>
		/// <param name='index'>Index of the item to be removed.</param>
		/// <exception cref='ArgumentOutOfRangeException'>
		/// <paramref name="index"/> is out of valid range.
		/// </exception>
		public void RemoveAt( int index )
		{
			if( index < 0 || _TotalCount <= index )
				throw new ArgumentOutOfRangeException( "index", "Specified index is out of valid range. (index:"+index+", this.Count:"+Count+")" );
			
			int nodeIndex;
			int indexInNode;
			
			// find the node which contains the item at specified index
			FindNodeIndex( index, out nodeIndex, out indexInNode );
			
			// if the node contains only the item to be removed,
			// remove the node itself.
			if( indexInNode <= 0 && _Nodes[indexInNode].Length <= 1 )
			{
				Debug.Assert( indexInNode == 0 );
				Debug.Assert( _Nodes[indexInNode].Length == 1 );
				_Nodes.RemoveAt( nodeIndex );
				if( 0 <= nodeIndex-1 && nodeIndex < _Nodes.Count
					&& _Nodes[nodeIndex-1].Value.Equals(_Nodes[nodeIndex].Value) )
				{
					// combine this node, the previous node, and the next node
					_Nodes[nodeIndex-1].Length += _Nodes[nodeIndex].Length;
					_Nodes.RemoveAt( nodeIndex );
				}
			}
			else
			{
				_Nodes[ nodeIndex ].Length--;
			}
			_TotalCount--;
		}

		/// <summary>
		/// Gets or sets an item at the specified index.
		/// </summary>
		/// <exception cref="System.ArgumentOutOfRangeException"/>
		public T this[ int index ]
		{
			get
			{
				if( index < 0 || _TotalCount <= index )
					throw new ArgumentOutOfRangeException();
				
				int nodeIndex = FindNodeIndex( index );
				Debug.Assert( 0 <= nodeIndex );

				return _Nodes[ nodeIndex ].Value;
			}
			set
			{
				if( index < 0 || _TotalCount <= index )
					throw new ArgumentOutOfRangeException();

				int nodeIndex;
				int indexInNode;
				Node thisNode, prevNode, nextNode;

				// find the node at the index
				FindNodeIndex( index, out nodeIndex, out indexInNode );
				Debug.Assert( 0 <= nodeIndex );

				// if the value at the index was already same as
				// the specified new value, do nothing
				thisNode = _Nodes[nodeIndex];
				if( thisNode.Value.Equals(value) )
				{
					return;
				}

				if( thisNode.Length == 1 )
				{
					Debug.Assert( indexInNode == 0 );
					prevNode = (0 <= nodeIndex-1) ? _Nodes[nodeIndex-1] : null;
					nextNode = (nodeIndex+1 < _Nodes.Count) ? _Nodes[nodeIndex+1] : null;

					if( prevNode != null
						&& prevNode.Value.Equals(value) )
					{
						// combine this node and the previous node if they have same value
						prevNode.Length++; // 1 == thisNode.Length
						_Nodes.RemoveAt( nodeIndex );

						// combine this node and the next node if they have same value
						if( nextNode != null
							&& nextNode.Value.Equals(value) )
						{
							prevNode.Length += nextNode.Length;
							_Nodes.RemoveAt( nodeIndex );
						}
					}
					else if( nextNode != null
						&& nextNode.Value.Equals(value) )
					{
						// combine this node and the next node
						thisNode.Value = value;
						thisNode.Length += nextNode.Length;
						_Nodes.RemoveAt( nodeIndex+1 );
					}
					else
					{
						// change the value of this node
						thisNode.Value = value;
					}
				}
				else if( indexInNode == 0 ) // at left boundary
				{
					prevNode = (0 <= nodeIndex-1) ? _Nodes[nodeIndex-1] : null;
					if( prevNode != null
						&& prevNode.Value.Equals(value) )
					{
						// combine this node and the previous node
						prevNode.Length++;
					}
					else
					{
						// insert a new node which holds the new value
						_Nodes.Insert( nodeIndex, new Node(1, value) );
					}

					// decrement the length of this node,
					// or delete this node if it no longer holds data
					thisNode.Length--;
					Debug.Assert( 0 < thisNode.Length );
				}
				else if( indexInNode == thisNode.Length - 1 ) // at right boundary
				{
					nextNode = (nodeIndex+1 < _Nodes.Count) ? _Nodes[nodeIndex+1] : null;
					if( nextNode != null
						&& nextNode.Value.Equals(value) )
					{
						// combine this node and the next node
						nextNode.Length++;
					}
					else
					{
						// insert a new node which holds the new value
						_Nodes.Insert( nodeIndex+1, new Node(1, value) );
					}

					// decrement the length of this node,
					// or delete this node if it no longer holds data
					thisNode.Length--;
					Debug.Assert( 0 < thisNode.Length );
				}
				else
				{
					//-- split this node and insert a new node holding the new value --
					int followingNodeLength = thisNode.Length - indexInNode - 1;
					Debug.Assert( 0 < indexInNode );
					Debug.Assert( 0 < followingNodeLength );

					// chop this node off at the specified index
					thisNode.Length = indexInNode;

					// insert a new node which holds the new value
					_Nodes.Insert( nodeIndex+1, new Node(1, value) );

					// insert a new node which holds the same value as this node
					_Nodes.Insert( nodeIndex+2, new Node(followingNodeLength, thisNode.Value) );
				}
			}
		}
		#endregion

		#region ICollection<T> + alpha
		/// <summary>
		/// Add the specified values.
		/// </summary>
		/// <param name='values'>The values to add.</param>
		public void Add( IEnumerable<T> values )
		{
			foreach( T value in values )
			{
				Add( value );
			}
		}

		/// <summary>
		/// Add the specified value.
		/// </summary>
		/// <param name='value'>The value to add.</param>
		public void Add( T value )
		{
			Insert( _TotalCount, value );
		}

		/// <summary>
		/// Clears this instance.
		/// </summary>
		public void Clear()
		{
			_Nodes.Clear();
			_TotalCount = 0;
		}

		/// <summary>
		/// Gets whether this array contains the specified value or not.
		/// </summary>
		public bool Contains( T value )
		{
			return ( 0 <= IndexOf(value) );
		}
		
		/// <summary>
		/// Copies the items of this object to an array, starting at the specified index.
		/// </summary>
		/// <param name="array">An array the items will be copied to.</param>
		/// <param name="arrayIndex">The index in <param name="array"> at which copying begins.</param>
		/// <exception cref="ArgumentNullException">
		/// <param name="array"> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <param name="arrayIndex"> is less than zero.
		/// </exception>
		public void CopyTo( T[] array, int arrayIndex )
		{
			if( array == null )
				throw new ArgumentNullException( "array" );
			if( arrayIndex < 0 )
				throw new ArgumentOutOfRangeException( "arrayIndex" );

			for( int i=0; i<Count; i++ )
			{
				array[arrayIndex+i] = this[i];
			}
		}

		/// <summary>
		/// Gets total number of items stored in this array.
		/// </summary>
		public int Count
		{
			get
			{
#				if DEBUG
				int count = 0;
				foreach( Node item in _Nodes )
				{
					count += item.Length;
				}
				Debug.Assert( count == _TotalCount );
#				endif
				return _TotalCount;
			}
		}
		
		/// <summary>
		/// Gets a value indicating whether this instance is read only.
		/// </summary>
		/// <value>
		/// <c>false</c> always since this class can not be read-only.
		/// </value>
		public bool IsReadOnly
		{
			get { return false; }
		}
		
		/// <summary>
		/// Removes the specified item which was firstly found in the array.
		/// </summary>
		public bool Remove( T item )
		{
			int index;
			
			index = IndexOf( item );
			if( index < 0 )
			{
				return false;
			}
			RemoveAt( index );
			
			return true;
		}

		#endregion

		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a string that represents this object.
		/// </summary>
		public override string ToString()
		{
			StringBuilder str = new StringBuilder();
			if( 0 < Count )
			{
				str.Append( this[0].ToString() );
				for( int i=1; i<Count; i++ )
				{
					str.Append( " " + this[i].ToString() );
				}
			}
			return str.ToString();
		}
		
		#region Utilities
		int FindNodeIndex( int index )
		{
			int nodeIndex, indexInNode;

			FindNodeIndex( index, out nodeIndex, out indexInNode );

			return nodeIndex;
		}

		void FindNodeIndex( int index, out int nodeIndex, out int indexInNode )
		{
			Debug.Assert( 0 <= index );
			
			int sum = 0;
						
			for( int i=0; i<_Nodes.Count; i++ )
			{
				Node node = _Nodes[i];
				if( index < sum + node.Length )
				{
					// target item is in this RLE node
					nodeIndex = i;
					indexInNode = index - sum;
					return;
				}
				sum += node.Length;
			}

			nodeIndex = -1;
			indexInNode = -1;
		}
		
#		if DEBUG
		internal
#		endif
		class Node
		{
			public Node( int length, T value )
			{
				Length = length;
				Value = value;
			}

			public int Length;
			public T Value;

			public override string ToString()
			{
				return String.Format( "[{0}|{1}]", Length, Value );
			}
		}
		#endregion
	}
}
