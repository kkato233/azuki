using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sgry.Azuki.Test
{
	[TestFixture]
	public class RleArrayTest
	{
		[Test]
		public void InitDispose()
		{
			RleArray<char> chars;

			chars = new RleArray<char>();
			Assert.AreEqual( 0, chars.Count );

			chars = new RleArray<char>();
			Set( chars, "abc" );
			Assert.AreEqual( 3, chars.Count );
			Assert.AreEqual( 'a', chars[0] );
			Assert.AreEqual( 'b', chars[1] );
			Assert.AreEqual( 'c', chars[2] );
		}

		[Test]
		public void GetSet()
		{
			RleArray<char> chars;

			// get
			chars = new RleArray<char>();
			Set( chars, "abc" );
			Assert.AreEqual( 3, chars.Count );
			Assert.AreEqual( 'a', chars[0] );
			Assert.AreEqual( 'b', chars[1] );
			Assert.AreEqual( 'c', chars[2] );
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				chars[-1].ToString();
			} );
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				chars[5].ToString();
			} );

			// set - out of bounds
			chars = new RleArray<char>();
			Set( chars, "abc" );
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				chars[-1] = 'z';
			} );
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				chars[4] = 'd';
			} );

			{
				// set - same value
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[2] = 'b';
				Assert.AreEqual( "a a b b b c c", chars.ToString() );
				Assert.AreEqual( "[2|a] [3|b] [2|c]", chars._Nodes.ToString() );

				// set - new value on left boundary
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[2] = 'X';
				Assert.AreEqual( "a a X b b c c", chars.ToString() );
				Assert.AreEqual( "[2|a] [1|X] [2|b] [2|c]", chars._Nodes.ToString() );

				// set - same value as the previous node on left boundary
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[2] = 'a';
				Assert.AreEqual( "a a a b b c c", chars.ToString() );
				Assert.AreEqual( "[3|a] [2|b] [2|c]", chars._Nodes.ToString() );

				// set - new value in the middle of a node
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[3] = 'a';
				Assert.AreEqual( "a a b a b c c", chars.ToString() );
				Assert.AreEqual( "[2|a] [1|b] [1|a] [1|b] [2|c]", chars._Nodes.ToString() );

				// set - new value on right boundary
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[4] = 'X';
				Assert.AreEqual( "a a b b X c c", chars.ToString() );
				Assert.AreEqual( "[2|a] [2|b] [1|X] [2|c]", chars._Nodes.ToString() );

				// set - same value as the next node on right boundary
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[4] = 'c';
				Assert.AreEqual( "a a b b c c c", chars.ToString() );
				Assert.AreEqual( "[2|a] [2|b] [3|c]", chars._Nodes.ToString() );
			}

			{
				// set - combines with previous node and the node disappears
				chars = new RleArray<char>();
				Set( chars, "aabcc" );
				chars[2] = 'a';
				Assert.AreEqual( "a a a c c", chars.ToString() );
				Assert.AreEqual( "[3|a] [2|c]", chars._Nodes.ToString() );

				// set - combines with next node and the node disappears
				chars = new RleArray<char>();
				Set( chars, "aabcc" );
				chars[2] = 'c';
				Assert.AreEqual( "a a c c c", chars.ToString() );
				Assert.AreEqual( "[2|a] [3|c]", chars._Nodes.ToString() );

				// set - combines with next and previous node, and the node disappears
				chars = new RleArray<char>();
				Set( chars, "aabaa" );
				chars[2] = 'a';
				Assert.AreEqual( "a a a a a", chars.ToString() );
				Assert.AreEqual( "[5|a]", chars._Nodes.ToString() );

				// set - combines with previous node and the node disappears
				chars = new RleArray<char>();
				Set( chars, "aabcc" );
				chars[2] = 'B';
				Assert.AreEqual( "a a B c c", chars.ToString() );
				Assert.AreEqual( "[2|a] [1|B] [2|c]", chars._Nodes.ToString() );
			}
		}

		[Test]
		public void Insert()
		{
			RleArray<char> chars = new RleArray<char>();

			Set( chars, "aabb" );
			chars.Insert( 2, 'a' );
			Assert.AreEqual( 5, chars.Count );
			Assert.AreEqual( 'a', chars[0] );
			Assert.AreEqual( 'a', chars[1] );
			Assert.AreEqual( 'a', chars[2] );
			Assert.AreEqual( 'b', chars[3] );
			Assert.AreEqual( 'b', chars[4] );
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				chars[-1].ToString();
			} );
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				chars[5].ToString();
			} );

			Set( chars, "aabb" );
			chars.Insert( 2, 'b' );
			Assert.AreEqual( 5, chars.Count );
			Assert.AreEqual( 'a', chars[0] );
			Assert.AreEqual( 'a', chars[1] );
			Assert.AreEqual( 'b', chars[2] );
			Assert.AreEqual( 'b', chars[3] );
			Assert.AreEqual( 'b', chars[4] );

			Set( chars, "aabb" );
			chars.Insert( 2, 'c' );
			Assert.AreEqual( 5, chars.Count );
			Assert.AreEqual( "a a c b b", chars.ToString() );

			Set( chars, "aabb" );
			chars.Insert( 1, 'a' );
			Assert.AreEqual( 5, chars.Count );
			Assert.AreEqual( 'a', chars[0] );
			Assert.AreEqual( 'a', chars[1] );
			Assert.AreEqual( 'a', chars[2] );
			Assert.AreEqual( 'b', chars[3] );
			Assert.AreEqual( 'b', chars[4] );

			Set( chars, "aabb" );
			chars.Insert( 1, 'c' );
			Assert.AreEqual( 5, chars.Count );
			Assert.AreEqual( 'a', chars[0] );
			Assert.AreEqual( 'c', chars[1] );
			Assert.AreEqual( 'a', chars[2] );
			Assert.AreEqual( 'b', chars[3] );
			Assert.AreEqual( 'b', chars[4] );
		}

		[Test]
		public void Remove()
		{
			RleArray<char> chars = new RleArray<char>();

			{
				// In a node
				Set( chars, "aa" );
				chars.RemoveAt( 0 );
				Assert.AreEqual( 1, chars.Count );
				Assert.AreEqual( "[1|a]", chars._Nodes.ToString() );
				chars.RemoveAt( 0 );
				Assert.AreEqual( 0, chars.Count );
				Assert.AreEqual( "", chars._Nodes.ToString() );
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					chars.RemoveAt(0);
				} );

				// Removing a node
				Set( chars, "aaabc" );
				chars.RemoveAt( 3 );
				Assert.AreEqual( 4, chars.Count );
				Assert.AreEqual( "[3|a] [1|c]", chars._Nodes.ToString() );

				// Combining nodes
				Set( chars, "aabaa" );
				chars.RemoveAt( 2 );
				Assert.AreEqual( 4, chars.Count );
				Assert.AreEqual( "[4|a]", chars._Nodes.ToString() );
			}
		}
		
		[Test]
		public void IndexOf()
		{
			RleArray<char> chars = new RleArray<char>();
			
			Set( chars, "aabbcca" );
			Assert.AreEqual( 0, chars.IndexOf('a') );
			Assert.AreEqual( 2, chars.IndexOf('b') );
			Assert.AreEqual( 4, chars.IndexOf('c') );
			Assert.AreEqual( -1, chars.IndexOf('d') );
		}

		[Test]
		public void Contains()
		{
			RleArray<char> chars = new RleArray<char>();
			
			Set( chars, "aabbcca" );
			Assert.AreEqual( true, chars.Contains('a') );
			Assert.AreEqual( true, chars.Contains('b') );
			Assert.AreEqual( true, chars.Contains('c') );
			Assert.AreEqual( false, chars.Contains('d') );
		}

#		region Utilities
		static void Set( RleArray<char> ary, IEnumerable<char> values )
		{
			ary.Clear();
			foreach( char value in values )
			{
				ary.Add( value );
			}
		}
#		endregion
	}
}
