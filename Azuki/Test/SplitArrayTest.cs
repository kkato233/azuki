#if TEST
using System;
using System.Collections;
using System.Collections.Generic;
using Conditional = System.Diagnostics.ConditionalAttribute;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.Test
{
	static class SplitArrayTest
	{
		public static void Test()
		{
			int test_num = 0;
			Console.WriteLine( "[Test for Azuki.SplitArray]" );

			// init
			Console.WriteLine( "test {0} - initial state", ++test_num );
			TestUtl.Do( Test_Init );

			// add
			Console.WriteLine( "test {0} - Add()", ++test_num );
			TestUtl.Do( Test_Add );

			// clear
			Console.WriteLine( "test {0} - Clear()", ++test_num );
			TestUtl.Do( Test_Clear );

			// Insert
			Console.WriteLine( "test {0} - Insert()", ++test_num );
			TestUtl.Do( Test_Insert_One );
			TestUtl.Do( Test_Insert_Array );

			// Replace
			Console.WriteLine( "test {0} - Replace()", ++test_num );
			TestUtl.Do( Test_Replace );

			// RemoveRange
			Console.WriteLine( "test {0} - RemoveRange()", ++test_num );
			TestUtl.Do( Test_RemoveRange );
			
			// CopyTo
			Console.WriteLine( "test {0} - CopyTo()", ++test_num );
			TestUtl.Do( Test_CopyTo );

			// Binary search
			Console.WriteLine( "test {0} - Binary search()", ++test_num );
			TestUtl.Do( Test_BinarySearch );

			Console.WriteLine( "done." );
			Console.WriteLine();
		}

		static void Test_Init()
		{
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			TestUtl.AssertEquals( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				try{ chars.GetAt(x); Debug.Fail("exception must be thrown here. (index:"+x+")"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
				try{ chars.SetAt('!', x); Debug.Fail("exception must be thrown here. (index:"+x+")"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			}
		}

		static void Test_Add()
		{
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			chars.Add( 'a' );
			TestUtl.AssertEquals( 1, chars.Count );
			TestUtl.AssertEquals( 'a', chars.GetAt(0) );
			chars.SetAt( 'b', 0 );
			TestUtl.AssertEquals( 'b', chars.GetAt(0) );
			try{ chars.GetAt(1); Debug.Fail("exception must be thrown here."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
		}

		static void Test_Clear()
		{
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			chars.Clear();
			TestUtl.AssertEquals( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				try{ chars.GetAt(x); Debug.Fail("exception must be thrown here."); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
				try{ chars.SetAt('!', x); Debug.Fail("exception must be thrown here."); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			}

			chars.Add( "hoge".ToCharArray() );
			chars.Clear();
			TestUtl.AssertEquals( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				try{ chars.GetAt(x); Debug.Fail("exception must be thrown here."); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
				try{ chars.SetAt('!', x); Debug.Fail("exception must be thrown here."); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			}
		}

		static void Test_Insert_One()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );

			// control-char
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 3, '\0' );
			TestUtl.AssertEquals( "hog\0epiyo", ToString(sary) );

			// before head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			try{ sary.Insert(-1, 'G'); Debug.Fail("### INSO_BH ###"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			// head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, 'G' );
			TestUtl.AssertEquals( 9, sary.Count );
			TestUtl.AssertEquals( "Ghogepiyo", ToString(sary) );

			// middle
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 4, 'G' );
			TestUtl.AssertEquals( 9, sary.Count );
			TestUtl.AssertEquals( "hogeGpiyo", ToString(sary) );

			// end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 8, 'G' );
			TestUtl.AssertEquals( 9, sary.Count );
			TestUtl.AssertEquals( "hogepiyoG", ToString(sary) );

			// after end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			try{ sary.Insert(9, 'G'); Console.WriteLine("### INSO_AE ###"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
		}

		static void Test_Insert_Array()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );

			// null array
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			try{ sary.Insert(0, null); Console.WriteLine("### INSA_NULL ###"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			
			// empty array
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, "".ToCharArray() );
			TestUtl.AssertEquals( "hogepiyo", ToString(sary) );

			// before head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			try{ sary.Insert(-1, "FOO".ToCharArray()); Console.WriteLine("### INSA_BH ###"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			// head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, "FOO".ToCharArray() );
			TestUtl.AssertEquals( 11, sary.Count );
			TestUtl.AssertEquals( "FOOhogepiyo", ToString(sary) );

			// middle
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 4, "FOO".ToCharArray() );
			TestUtl.AssertEquals( 11, sary.Count );
			TestUtl.AssertEquals( "hogeFOOpiyo", ToString(sary) );

			// end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 8, "FOO".ToCharArray() );
			TestUtl.AssertEquals( 11, sary.Count );
			TestUtl.AssertEquals( "hogepiyoFOO", ToString(sary) );

			// after end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			try{ sary.Insert(9, "FOO".ToCharArray()); Console.WriteLine("### INSA_AE ###"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
		}

		static void Test_Replace()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );

			// replace position
			{
				// before head
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				try{ sary.Replace(-1, "000".ToCharArray(), 0, 2); Console.WriteLine("### REP_P_BH ###"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

				// head
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				sary.Replace( 0, "000".ToCharArray(), 0, 2 );
				TestUtl.AssertEquals( "00gepiyo", ToString(sary) );

				// middle
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				sary.Replace( 4, "000".ToCharArray(), 0, 2 );
				TestUtl.AssertEquals( "hoge00yo", ToString(sary) );

				// end
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				sary.Replace( 6, "000".ToCharArray(), 0, 2 );
				TestUtl.AssertEquals( "hogepi00", ToString(sary) );

				// after end
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				try{ sary.Replace(7, "000".ToCharArray(), 0, 2); Console.WriteLine("### REP_P_AE1 ##"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
				try{ sary.Replace(8, "000".ToCharArray(), 0, 2); Console.WriteLine("### REP_P_AE2 ###"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			}

			// value array
			{
				// giving null
				try{ sary.Replace(0, null, 0, 1); Console.WriteLine("### REP_A_NULL ###"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

				// empty array
				sary.Replace( 0, "".ToCharArray(), 0, 0 );
				TestUtl.AssertEquals( "hogepiyo", ToString(sary) );

				// empty range
				sary.Replace( 0, "000".ToCharArray(), 0, 0 );
				TestUtl.AssertEquals( "hogepiyo", ToString(sary) );

				// invalid range (reversed)
				try{ sary.Replace(0, "000".ToCharArray(), 1, 0); Console.WriteLine("### REP_A_REV ###"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

				// invalid range (before head)
				try{ sary.Replace(0, "000".ToCharArray(), -1, 0); Console.WriteLine("### REP_A_BH ###"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

				// invalid range (after head)
				try{ sary.Replace(0, "000".ToCharArray(), 3, 4); Console.WriteLine("### REPEP_A_AE ###"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			}
		}

		static void Test_RemoveRange()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			// case 2 (moving gap to buffer head)
			chars.Add( InitData.ToCharArray() );
			chars.RemoveAt( 2 );
			TestUtl.AssertEquals( 7, chars.Count );
			TestUtl.AssertEquals( "hoepiyo", ToString(chars) );
			try{ chars.GetAt(7); Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			
			// case 1 (moving gap to buffer end)
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 5, 7 );
			TestUtl.AssertEquals( 6, chars.Count );
			TestUtl.AssertEquals( "hogepo", ToString(chars) );
			try{ chars.GetAt(6); Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			
			// before head to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			try{ chars.RemoveRange(-1, 2); Debug.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			
			// head to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 0, 2 );
			TestUtl.AssertEquals( "gepiyo", ToString(chars) );
			
			// middle to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 2, 5 );
			TestUtl.AssertEquals( "hoiyo", ToString(chars) );
			
			// middle to end
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 5, 8 );
			TestUtl.AssertEquals( "hogep", ToString(chars) );
			
			// middle to after end
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			try{ chars.RemoveRange(5, 9); Debug.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
		}

		static void Test_CopyTo()
		{
			const string initBufContent = "123456";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );
			char[] buf;
			sary.Insert( 0, "hogepiyo".ToCharArray() );

			// before head to middle
			buf = initBufContent.ToCharArray();
			try{ sary.CopyTo(-1, 5, buf); Debug.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			// begin to middle
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 0, 5, buf );
			TestUtl.AssertEquals( "hogep6", new String(buf) );

			// middle to middle
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 1, 5, buf );
			TestUtl.AssertEquals( "ogep56", new String(buf) );

			// middle to end
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 5, 8, buf );
			TestUtl.AssertEquals( "iyo456", new String(buf) );

			// end to after end
			buf = initBufContent.ToCharArray();
			try{ sary.CopyTo(5, 9, buf); Debug.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			// Range before the gap
			MoveGap( sary, 2 );
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 0, 1, buf );
			TestUtl.AssertEquals( "h23456", new String(buf) );

			// Range including the gap
			MoveGap( sary, 2 );
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 0, 4, buf );
			TestUtl.AssertEquals( "hoge56", new String(buf) );

			// Range after the gap
			MoveGap( sary, 2 );
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 4, 8, buf );
			TestUtl.AssertEquals( "piyo56", new String(buf) );
		}

		static void Test_BinarySearch()
		{
			SplitArray<int> ary = new SplitArray<int>( 4 );

			ary.Clear();
			TestUtl.AssertEquals( -1, ary.BinarySearch(1234) );

			ary.Clear();
			ary.Add( 3 );
			TestUtl.AssertEquals( ~(0), ary.BinarySearch(2) );
			TestUtl.AssertEquals(  (0), ary.BinarySearch(3) );
			TestUtl.AssertEquals( ~(1), ary.BinarySearch(4) );

			ary.Clear();
			ary.Add( 1, 3 );
			TestUtl.AssertEquals( ~(0), ary.BinarySearch(0) );
			TestUtl.AssertEquals(  (0), ary.BinarySearch(1) );
			TestUtl.AssertEquals( ~(1), ary.BinarySearch(2) );
			TestUtl.AssertEquals(  (1), ary.BinarySearch(3) );
			TestUtl.AssertEquals( ~(2), ary.BinarySearch(4) );

			SplitArray<System.Drawing.Point> points = new SplitArray<System.Drawing.Point>( 4 );
			points.Add( new System.Drawing.Point() );
			try
			{
				points.BinarySearch(new System.Drawing.Point(1,1));
				throw new AssertException();
			}
			catch( Exception ex )
			{
				TestUtl.AssertExceptionType<ArgumentException>(ex);
			}
		}

		static string ToString( SplitArray<char> sary )
		{
			return new string( sary.ToArray() );
		}

		static void MoveGap( SplitArray<char> sary, int index )
		{
			sary.Insert( index, new char[]{} );
		}
	}
}
#endif
