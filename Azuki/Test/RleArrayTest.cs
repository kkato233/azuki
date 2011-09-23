// 2011-09-23
#if TEST
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.Test
{
	static class RleArrayTest
	{
		public static void Test()
		{
			int test_num = 0;
			Console.WriteLine( "[Test for Azuki.RleArray]" );

			// init / dispose
			Console.WriteLine( "test {0} - init / dispose()", ++test_num );
			TestUtl.Do( Test_InitDispose );

			// get/set
			Console.WriteLine( "test {0} - GetSet()", ++test_num );
			TestUtl.Do( Test_GetSet );

			// insert
			Console.WriteLine( "test {0} - Insert()", ++test_num );
			TestUtl.Do( Test_Insert );

			// remove
			Console.WriteLine( "test {0} - Remove()", ++test_num );
			TestUtl.Do( Test_Remove );

			// IndexOf
			Console.WriteLine( "test {0} - IndexOf()", ++test_num );
			TestUtl.Do( Test_IndexOf );

			// Contains
			Console.WriteLine( "test {0} - Contains()", ++test_num );
			TestUtl.Do( Test_Contains );

			Console.WriteLine( "done." );
			Console.WriteLine();
		}

		static void Test_InitDispose()
		{
			RleArray<char> chars;

			chars = new RleArray<char>();
			TestUtl.AssertEquals( 0, chars.Count );

			chars = new RleArray<char>();
			Set( chars, "abc" );
			TestUtl.AssertEquals( 3, chars.Count );
			TestUtl.AssertEquals( 'a', chars[0] );
			TestUtl.AssertEquals( 'b', chars[1] );
			TestUtl.AssertEquals( 'c', chars[2] );
		}

		static void Test_GetSet()
		{
			RleArray<char> chars;

			// get
			chars = new RleArray<char>();
			Set( chars, "abc" );
			TestUtl.AssertEquals( 3, chars.Count );
			TestUtl.AssertEquals( 'a', chars[0] );
			TestUtl.AssertEquals( 'b', chars[1] );
			TestUtl.AssertEquals( 'c', chars[2] );
			try{ chars[-1].ToString(); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertExceptionType<ArgumentOutOfRangeException>(ex); }
			try{ chars[5].ToString(); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertExceptionType<ArgumentOutOfRangeException>(ex); }

			// set - out of bounds
			chars = new RleArray<char>();
			Set( chars, "abc" );
			try{ chars[-1] = 'z'; throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertExceptionType<ArgumentOutOfRangeException>(ex); }
			try{ chars[4] = 'd'; throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertExceptionType<ArgumentOutOfRangeException>(ex); }

			{
				// set - same value
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[2] = 'b';
				TestUtl.AssertEquals( "a a b b b c c", chars.ToString() );
				TestUtl.AssertEquals( "[2|a] [3|b] [2|c]", chars._Nodes.ToString() );

				// set - new value on left boundary
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[2] = 'X';
				TestUtl.AssertEquals( "a a X b b c c", chars.ToString() );
				TestUtl.AssertEquals( "[2|a] [1|X] [2|b] [2|c]", chars._Nodes.ToString() );

				// set - same value as the previous node on left boundary
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[2] = 'a';
				TestUtl.AssertEquals( "a a a b b c c", chars.ToString() );
				TestUtl.AssertEquals( "[3|a] [2|b] [2|c]", chars._Nodes.ToString() );

				// set - new value in the middle of a node
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[3] = 'a';
				TestUtl.AssertEquals( "a a b a b c c", chars.ToString() );
				TestUtl.AssertEquals( "[2|a] [1|b] [1|a] [1|b] [2|c]", chars._Nodes.ToString() );

				// set - new value on right boundary
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[4] = 'X';
				TestUtl.AssertEquals( "a a b b X c c", chars.ToString() );
				TestUtl.AssertEquals( "[2|a] [2|b] [1|X] [2|c]", chars._Nodes.ToString() );

				// set - same value as the next node on right boundary
				chars = new RleArray<char>();
				Set( chars, "aabbbcc" );
				chars[4] = 'c';
				TestUtl.AssertEquals( "a a b b c c c", chars.ToString() );
				TestUtl.AssertEquals( "[2|a] [2|b] [3|c]", chars._Nodes.ToString() );
			}

			{
				// set - combines with previous node and the node disappears
				chars = new RleArray<char>();
				Set( chars, "aabcc" );
				chars[2] = 'a';
				TestUtl.AssertEquals( "a a a c c", chars.ToString() );
				TestUtl.AssertEquals( "[3|a] [2|c]", chars._Nodes.ToString() );

				// set - combines with next node and the node disappears
				chars = new RleArray<char>();
				Set( chars, "aabcc" );
				chars[2] = 'c';
				TestUtl.AssertEquals( "a a c c c", chars.ToString() );
				TestUtl.AssertEquals( "[2|a] [3|c]", chars._Nodes.ToString() );

				// set - combines with next and previous node, and the node disappears
				chars = new RleArray<char>();
				Set( chars, "aabaa" );
				chars[2] = 'a';
				TestUtl.AssertEquals( "a a a a a", chars.ToString() );
				TestUtl.AssertEquals( "[5|a]", chars._Nodes.ToString() );

				// set - combines with previous node and the node disappears
				chars = new RleArray<char>();
				Set( chars, "aabcc" );
				chars[2] = 'B';
				TestUtl.AssertEquals( "a a B c c", chars.ToString() );
				TestUtl.AssertEquals( "[2|a] [1|B] [2|c]", chars._Nodes.ToString() );
			}
		}

		static void Test_Insert()
		{
			RleArray<char> chars = new RleArray<char>();

			Set( chars, "aabb" );
			chars.Insert( 2, 'a' );
			TestUtl.AssertEquals( 5, chars.Count );
			TestUtl.AssertEquals( 'a', chars[0] );
			TestUtl.AssertEquals( 'a', chars[1] );
			TestUtl.AssertEquals( 'a', chars[2] );
			TestUtl.AssertEquals( 'b', chars[3] );
			TestUtl.AssertEquals( 'b', chars[4] );
			try{ chars[-1].ToString(); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertExceptionType<ArgumentOutOfRangeException>(ex); }
			try{ chars[5].ToString(); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertExceptionType<ArgumentOutOfRangeException>(ex); }

			Set( chars, "aabb" );
			chars.Insert( 2, 'b' );
			TestUtl.AssertEquals( 5, chars.Count );
			TestUtl.AssertEquals( 'a', chars[0] );
			TestUtl.AssertEquals( 'a', chars[1] );
			TestUtl.AssertEquals( 'b', chars[2] );
			TestUtl.AssertEquals( 'b', chars[3] );
			TestUtl.AssertEquals( 'b', chars[4] );

			Set( chars, "aabb" );
			chars.Insert( 2, 'c' );
			TestUtl.AssertEquals( 5, chars.Count );
			TestUtl.AssertEquals( "a a c b b", chars.ToString() );

			Set( chars, "aabb" );
			chars.Insert( 1, 'a' );
			TestUtl.AssertEquals( 5, chars.Count );
			TestUtl.AssertEquals( 'a', chars[0] );
			TestUtl.AssertEquals( 'a', chars[1] );
			TestUtl.AssertEquals( 'a', chars[2] );
			TestUtl.AssertEquals( 'b', chars[3] );
			TestUtl.AssertEquals( 'b', chars[4] );

			Set( chars, "aabb" );
			chars.Insert( 1, 'c' );
			TestUtl.AssertEquals( 5, chars.Count );
			TestUtl.AssertEquals( 'a', chars[0] );
			TestUtl.AssertEquals( 'c', chars[1] );
			TestUtl.AssertEquals( 'a', chars[2] );
			TestUtl.AssertEquals( 'b', chars[3] );
			TestUtl.AssertEquals( 'b', chars[4] );
		}

		static void Test_Remove()
		{
			RleArray<char> chars = new RleArray<char>();

			{
				try{ chars.RemoveAt(-1); throw new Exception(); }
				catch( Exception ex ){ TestUtl.AssertExceptionType<ArgumentOutOfRangeException>(ex); }

				Set( chars, "aabb" );
				chars.RemoveAt( 0 );
				TestUtl.AssertEquals( 3, chars.Count );
				TestUtl.AssertEquals( "a b b", chars.ToString() );

				Set( chars, "aabb" );
				chars.RemoveAt( 1 );
				TestUtl.AssertEquals( 3, chars.Count );
				TestUtl.AssertEquals( "a b b", chars.ToString() );

				Set( chars, "aabb" );
				chars.RemoveAt( 2 );
				TestUtl.AssertEquals( 3, chars.Count );
				TestUtl.AssertEquals( "a a b", chars.ToString() );

				Set( chars, "aabb" );
				chars.RemoveAt( 3 );
				TestUtl.AssertEquals( 3, chars.Count );
				TestUtl.AssertEquals( "a a b", chars.ToString() );

				try{ chars.RemoveAt(4); throw new Exception(); }
				catch( Exception ex ){ TestUtl.AssertExceptionType<ArgumentOutOfRangeException>(ex); }
			}
				
			{
				Set( chars, "abc" );
				chars.RemoveAt( 0 );
				TestUtl.AssertEquals( 2, chars.Count );
				TestUtl.AssertEquals( "b c", chars.ToString() );

				Set( chars, "abc" );
				chars.RemoveAt( 1 );
				TestUtl.AssertEquals( 2, chars.Count );
				TestUtl.AssertEquals( "a c", chars.ToString() );

				Set( chars, "abc" );
				chars.RemoveAt( 2 );
				TestUtl.AssertEquals( 2, chars.Count );
				TestUtl.AssertEquals( "a b", chars.ToString() );

				Set( chars, "_A_" );
				chars.RemoveAt( 1 );
				TestUtl.AssertEquals( 2, chars.Count );
				TestUtl.AssertEquals( "_ _", chars.ToString() );
			}
		}
		
		static void Test_IndexOf()
		{
			RleArray<char> chars = new RleArray<char>();
			
			Set( chars, "aabbcca" );
			TestUtl.AssertEquals( 0, chars.IndexOf('a') );
			TestUtl.AssertEquals( 2, chars.IndexOf('b') );
			TestUtl.AssertEquals( 4, chars.IndexOf('c') );
			TestUtl.AssertEquals( -1, chars.IndexOf('d') );
		}
		
		static void Test_Contains()
		{
			RleArray<char> chars = new RleArray<char>();
			
			Set( chars, "aabbcca" );
			TestUtl.AssertEquals( true, chars.Contains('a') );
			TestUtl.AssertEquals( true, chars.Contains('b') );
			TestUtl.AssertEquals( true, chars.Contains('c') );
			TestUtl.AssertEquals( false, chars.Contains('d') );
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
#endif
