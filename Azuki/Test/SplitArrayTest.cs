// 2008-12-28
#if DEBUG
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
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			Console.WriteLine( "[Test for Azuki.SplitArray]" );

			// init
			Console.WriteLine( "test 0 - initial state" );
			TestUtl.AssertEquals( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				try{ chars.GetAt(x); DebugUtl.Fail("exception must be thrown here. (index:"+x+")"); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
				try{ chars.SetAt('!', x); DebugUtl.Fail("exception must be thrown here. (index:"+x+")"); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
			}

			// clear
			Console.WriteLine( "test 1 - Clear()" );
			chars.Clear();
			TestUtl.AssertEquals( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				try{ chars.GetAt(x); DebugUtl.Fail("exception must be thrown here."); }
				catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
				try{ chars.SetAt('!', x); DebugUtl.Fail("exception must be thrown here."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
			}

			// add
			Console.WriteLine( "test 2 - Add()" );
			chars.Add( 'a' );
			TestUtl.AssertEquals( 1, chars.Count );
			TestUtl.AssertEquals( 'a', chars.GetAt(0) );
			chars.SetAt( 'b', 0 );
			TestUtl.AssertEquals( 'b', chars.GetAt(0) );
			try{ chars.GetAt(1); DebugUtl.Fail("exception must be thrown here."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			// Insert
			Console.WriteLine( "test 3 - Insert()" );
			TestUtl.Do( Test_Insert_One );
			TestUtl.Do( Test_Insert_Array );
			TestUtl.Do( Test_Insert_Cvt );

			// Replace
			Console.WriteLine( "test 4 - Replace()" );
			TestUtl.Do( Test_Replace );

// SetAt (to part2)
//chars.SetAt( 'Z', 6 ); // case 2
//TestUtl.AssertEquals( chars[6] == 'Z' );

			// Delete
			Console.WriteLine( "test 5 - Delete()" );
			TestUtl.Do( Test_Delete );
			
			// GetRange
			Console.WriteLine( "test 6 - GetRange()" );
			TestUtl.Do( Test_GetRange );

			// Convertion (ToArray, GetRange)
			Console.WriteLine( "test 7 - Convertion()" );
			TestUtl.Do( Test_Convertion );

			Console.WriteLine( "done." );
			Console.WriteLine();
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

		static void Test_Insert_Cvt()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );
			int[] nums = new int[]{ 15, 14, 13 };
			Converter<int, char> cvt = delegate( int n ){
				return n.ToString("X")[0];
			};

			// null array
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			try{ sary.Insert(0, null, cvt); Console.WriteLine("### INSC_NULL ###"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			
			// empty array
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, new int[]{}, cvt );
			TestUtl.AssertEquals( "hogepiyo", ToString(sary) );

			// before head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			try{ sary.Insert(-1, nums, cvt); Console.WriteLine("### INSC_BH ###"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			// head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, nums, cvt );
			TestUtl.AssertEquals( 11, sary.Count );
			TestUtl.AssertEquals( "FEDhogepiyo", ToString(sary) );

			// middle
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 4, nums, cvt );
			TestUtl.AssertEquals( 11, sary.Count );
			TestUtl.AssertEquals( "hogeFEDpiyo", ToString(sary) );

			// end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 8, nums, cvt );
			TestUtl.AssertEquals( 11, sary.Count );
			TestUtl.AssertEquals( "hogepiyoFED", ToString(sary) );

			// after end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			try{ sary.Insert(9, nums, cvt); Console.WriteLine("### INSC_AE ###"); }
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

		static void Test_Delete()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			// case 2 (moving gap to buffer head)
			chars.Add( InitData.ToCharArray() );
			chars.Delete( 2, 3 );
			TestUtl.AssertEquals( 7, chars.Count );
			TestUtl.AssertEquals( "hoepiyo", ToString(chars) );
			try{ chars.GetAt(7); Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			
			// case 1 (moving gap to buffer end)
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.Delete( 5, 7 );
			TestUtl.AssertEquals( 6, chars.Count );
			TestUtl.AssertEquals( "hogepo", ToString(chars) );
			try{ chars.GetAt(6); Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!"); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			
			// before head to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			try{ chars.Delete(-1, 2); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
			
			// head to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.Delete( 0, 2 );
			TestUtl.AssertEquals( "gepiyo", ToString(chars) );
			
			// middle to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.Delete( 2, 5 );
			TestUtl.AssertEquals( "hoiyo", ToString(chars) );
			
			// middle to end
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.Delete( 5, 8 );
			TestUtl.AssertEquals( "hogep", ToString(chars) );
			
			// middle to after end
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			try{ chars.Delete(5, 9); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
		}

		static void Test_GetRange()
		{
			const string initBufContent = "123456";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );
			char[] buf;
			sary.Insert( 0, "hogepiyo".ToCharArray() );

			// before head to middle
			buf = initBufContent.ToCharArray();
			try{ sary.GetRange(-1, 5, ref buf); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			// begin to middle
			buf = initBufContent.ToCharArray();
			sary.GetRange( 0, 5, ref buf );
			TestUtl.AssertEquals( "hogep6", new String(buf) );

			// middle to middle
			buf = initBufContent.ToCharArray();
			sary.GetRange( 1, 5, ref buf );
			TestUtl.AssertEquals( "ogep56", new String(buf) );

			// middle to end
			buf = initBufContent.ToCharArray();
			sary.GetRange( 5, 8, ref buf );
			TestUtl.AssertEquals( "iyo456", new String(buf) );

			// end to after end
			buf = initBufContent.ToCharArray();
			try{ sary.GetRange(5, 9, ref buf); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
		}

		static void Test_Convertion()
		{
			SplitArray<int> sary = new SplitArray<int>( 5, 8 );
			string[] ary;

			Converter<int, string> itos = delegate( int num ) {
				return num.ToString( "D4" );
			};
			Converter<string, int> stoi = delegate( string num ) {
				return Convert.ToInt32( num );
			};

			sary.Add( 0 );
			sary.Add( 1 );
			sary.Add( 2 );
			sary.Add( 3 );
			sary.Add( 4 );

			// ToArray<S>()
			ary = sary.ToArray<string>( itos );
			TestUtl.AssertEquals( "0000", ary[0] );
			TestUtl.AssertEquals( "0001", ary[1] );
			TestUtl.AssertEquals( "0002", ary[2] );
			TestUtl.AssertEquals( "0003", ary[3] );
			TestUtl.AssertEquals( "0004", ary[4] );

			// GetRange<S>()
			ary = new string[4];
			sary.GetRange<string>( 1, 3, ref ary, itos );
			TestUtl.AssertEquals( "0001", ary[0] );
			TestUtl.AssertEquals( "0002", ary[1] );
			TestUtl.AssertEquals( null, ary[2] );
			TestUtl.AssertEquals( null, ary[3] );

			// Insert<S>()
			ary = new string[]{"10", "10"};
			sary.Insert<string>( 1, ary, stoi );
			TestUtl.AssertEquals( 0, sary[0] );
			TestUtl.AssertEquals( 10, sary[1] );
			TestUtl.AssertEquals( 10, sary[2] );
			TestUtl.AssertEquals( 1, sary[3] );
			TestUtl.AssertEquals( 2, sary[4] );
			TestUtl.AssertEquals( 3, sary[5] );
		}

		static string ToString( SplitArray<char> sary )
		{
			return new string( sary.ToArray() );
		}
	}
}
#endif
