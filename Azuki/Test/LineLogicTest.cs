// 2009-10-31
#if TEST
using System;
using System.Collections;
using System.Text;
using Debug = Sgry.DebugUtl;

namespace Sgry.Azuki.Test
{
	static class LineLogicTest
	{
		public static void Test()
		{
			Console.WriteLine( "[Test for Azuki.LineLogic]" );
			// TEST DATA:
			// --------------------
			// "keep it as simple as possible\r\n (head: 0, len:31)
			// \n                                 (head:32, len: 1)
			// but\n                              (head:33, len: 4)
			// \r                                 (head:37, len: 1)
			// not simpler."\r                    (head:38, len:14)
			// \r                                 (head:52, len: 1)
			//  - Albert Einstein                 (head:53, len:18)
			// --------------------
			const string TestData1 = "\"keep ot simpler.\"\r\r - Albert Einstein";
			const string TestData2 = "it as simple as possible\r\n\nbt\n\rn";
			int testNum = 0;
			TextBuffer text = new TextBuffer( 1, 32 );
			SplitArray<int> lhi = new SplitArray<int>( 1, 8 );
			SplitArray<LineDirtyState> lms = new SplitArray<LineDirtyState>( 1, 8 );
			int l, c, head, end;
			int i;
			lhi.Add( 0 ); lms.Add( LineDirtyState.Clean );

			//
			// LHI_Insert
			//
			Console.WriteLine( "test {0} - LHI_Insert()", testNum++ );
			TestUtl.Do( Test_LHI_Insert );

			//
			// LHI_Delete
			//
			Console.WriteLine( "test {0} - LHI_Delete()", testNum++ );
			TestUtl.Do( Test_LHI_Delete );

			LineLogic.LHI_Insert( lhi, lms, text, TestData1, 0 );
			text.Add( TestData1.ToCharArray() );
			LineLogic.LHI_Insert( lhi, lms, text, TestData2, 6 );
			text.Insert( 6, TestData2.ToCharArray() );
			LineLogic.LHI_Insert( lhi, lms, text, "u", 34 );
			text.Insert( 34, "u".ToCharArray() );

			//
			// NextLineHead
			//
			Console.WriteLine( "test {0} - NextLineHead()", testNum++ );
			TestUtl.Do( Test_NextLineHead );

			//
			// PrevLineHead
			//
			Console.WriteLine( "test {0} - PrevLineHead()", testNum++ );
			i=71;
			for( ; 53<=i; i-- )
				TestUtl.AssertEquals( 53, LineLogic.PrevLineHead(text, i) );
			for( ; 52<=i; i-- )
				TestUtl.AssertEquals( 52, LineLogic.PrevLineHead(text, i) );
			for( ; 38<=i; i-- )
				TestUtl.AssertEquals( 38, LineLogic.PrevLineHead(text, i) );
			for( ; 37<=i; i-- )
				TestUtl.AssertEquals( 37, LineLogic.PrevLineHead(text, i) );
			for( ; 33<=i; i-- )
				TestUtl.AssertEquals( 33, LineLogic.PrevLineHead(text, i) );
			for( ; 32<=i; i-- )
				TestUtl.AssertEquals( 32, LineLogic.PrevLineHead(text, i) );
			for( ; 0<=i; i-- )
				TestUtl.AssertEquals( 0,  LineLogic.PrevLineHead(text, i) );

			//
			// GetLineLengthByCharIndex
			//
			Console.WriteLine( "test {0} - GetLineLengthByCharIndex()", testNum++ );
			TestUtl.Do( Test_GetLineLengthByCharIndex );

			//
			// GetLineRangeWithEol
			//
			Console.WriteLine( "test {0} - GetLineRangeWithEol()", testNum++ );
			LineLogic.GetLineRangeWithEol( text, lhi, 0, out head, out end );
			TestUtl.AssertEquals( 0, head );
			TestUtl.AssertEquals( 32, end );
			LineLogic.GetLineRangeWithEol( text, lhi, 1, out head, out end );
			TestUtl.AssertEquals( 32, head );
			TestUtl.AssertEquals( 33, end );
			LineLogic.GetLineRangeWithEol( text, lhi, 2, out head, out end );
			TestUtl.AssertEquals( 33, head );
			TestUtl.AssertEquals( 37, end );
			LineLogic.GetLineRangeWithEol( text, lhi, 3, out head, out end );
			TestUtl.AssertEquals( 37, head );
			TestUtl.AssertEquals( 38, end );
			LineLogic.GetLineRangeWithEol( text, lhi, 4, out head, out end );
			TestUtl.AssertEquals( 38, head );
			TestUtl.AssertEquals( 52, end );
			LineLogic.GetLineRangeWithEol( text, lhi, 5, out head, out end );
			TestUtl.AssertEquals( 52, head );
			TestUtl.AssertEquals( 53, end );
			LineLogic.GetLineRangeWithEol( text, lhi, 6, out head, out end );
			TestUtl.AssertEquals( 53, head );
			TestUtl.AssertEquals( 71, end );

			//
			// GetLineRange
			//
			Console.WriteLine( "test {0} - GetLineRange()", testNum++ );
			LineLogic.GetLineRange( text, lhi, 0, out head, out end );
			TestUtl.AssertEquals( 0, head );
			TestUtl.AssertEquals( 30, end );
			LineLogic.GetLineRange( text, lhi, 1, out head, out end );
			TestUtl.AssertEquals( 32, head );
			TestUtl.AssertEquals( 32, end );
			LineLogic.GetLineRange( text, lhi, 2, out head, out end );
			TestUtl.AssertEquals( 33, head );
			TestUtl.AssertEquals( 36, end );
			LineLogic.GetLineRange( text, lhi, 3, out head, out end );
			TestUtl.AssertEquals( 37, head );
			TestUtl.AssertEquals( 37, end );
			LineLogic.GetLineRange( text, lhi, 4, out head, out end );
			TestUtl.AssertEquals( 38, head );
			TestUtl.AssertEquals( 51, end );
			LineLogic.GetLineRange( text, lhi, 5, out head, out end );
			TestUtl.AssertEquals( 52, head );
			TestUtl.AssertEquals( 52, end );
			LineLogic.GetLineRange( text, lhi, 6, out head, out end );
			TestUtl.AssertEquals( 53, head );
			TestUtl.AssertEquals( 71, end );

			//
			// GetCharIndexFromLineColumnIndex
			//
			Console.WriteLine( "test {0} - GetCharIndexFromLineColumnIndex()", testNum++ );
			TestUtl.Do( Test_GetCharIndexFromLineColumnIndex );

			//
			// GetLineIndexFromCharIndex
			//
			Console.WriteLine( "test {0} - GetLineIndexFromCharIndex()", testNum++ );
			TestUtl.Do( Test_GetLineIndexFromCharIndex );

			//
			// GetLineColumnIndexFromCharIndex
			//
			Console.WriteLine( "test {0} - GetLineColumnIndexFromCharIndex()", testNum++ );
			LineLogic.GetLineColumnIndexFromCharIndex( text, lhi, 0, out l, out c );
			TestUtl.AssertEquals( 0, l );
			TestUtl.AssertEquals( 0, c );
			LineLogic.GetLineColumnIndexFromCharIndex( text, lhi, 2, out l, out c );
			TestUtl.AssertEquals( 0, l );
			TestUtl.AssertEquals( 2, c );
			LineLogic.GetLineColumnIndexFromCharIndex( text, lhi, 40, out l, out c );
			TestUtl.AssertEquals( 4, l );
			TestUtl.AssertEquals( 2, c );
			LineLogic.GetLineColumnIndexFromCharIndex( text, lhi, 71, out l, out c ); // 71 --> EOF
			TestUtl.AssertEquals( 6, l );
			TestUtl.AssertEquals( 18, c );
			try{ LineLogic.GetLineColumnIndexFromCharIndex(text, lhi, 72, out l, out c); Debug.Fail("exception must be thrown here."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			//
			// LineHeadIndexFromCharIndex
			//
			Console.WriteLine( "test {0} - LineHeadIndexFromCharIndex()", testNum++ );
			i=0;
			for( ; i<32; i++ )
				TestUtl.AssertEquals(  0, LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( 32, LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( 33, LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals( 37, LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 38, LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals( 52, LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<=71; i++ )
				TestUtl.AssertEquals( 53, LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			try{ LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i); Debug.Fail("exception must be thrown here."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			Console.WriteLine( "done." );
			Console.WriteLine();
		}

		static void Test_GetCharIndexFromLineColumnIndex()
		{
			// TEST DATA:
			// --------------------
			// "keep it as simple as possible\r\n (head: 0, len:31)
			// \n                                 (head:32, len: 1)
			// but\n                              (head:33, len: 4)
			// \r                                 (head:37, len: 1)
			// not simpler."\r                    (head:38, len:14)
			// \r                                 (head:52, len: 1)
			//  - Albert Einstein                 (head:53, len:18)
			// --------------------
			const string TestData = "\"keep it as simple as possible\r\n\nbut\n\rnot simpler.\"\r\r - Albert Einstein";
			TextBuffer text = new TextBuffer( 1, 1 );
			SplitArray<int> lhi = new SplitArray<int>( 1, 8 );
			SplitArray<LineDirtyState> lms = new SplitArray<LineDirtyState>( 1, 8 );
			lhi.Add( 0 ); lms.Add( LineDirtyState.Clean );
			LineLogic.LHI_Insert( lhi, lms, text, TestData, 0 );
			text.Insert( 0, TestData.ToCharArray() );

			TestUtl.AssertEquals(  0, LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 0,  0) );
			TestUtl.AssertEquals( 34, LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 2,  1) );
			TestUtl.AssertEquals( 71, LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 6, 18) );
			try{ LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 6, 19); Debug.Fail("exception must be thrown here."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>( ex ); }
			try{ LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 0, 100); Debug.Fail("exception must be thrown here."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>( ex ); }
		}

		static void Test_NextLineHead()
		{
			// TEST DATA:
			// --------------------
			// "keep it as simple as possible\r\n (head: 0, len:31)
			// \n                                 (head:32, len: 1)
			// but\n                              (head:33, len: 4)
			// \r                                 (head:37, len: 1)
			// not simpler."\r                    (head:38, len:14)
			// \r                                 (head:52, len: 1)
			//  - Albert Einstein                 (head:53, len:18)
			// --------------------
			const string TestData = "\"keep it as simple as possible\r\n\nbut\n\rnot simpler.\"\r\r - Albert Einstein";
			TextBuffer text = new TextBuffer( 1, 32 );
			SplitArray<int> lhi = new SplitArray<int>( 1, 8 );
			text.Insert( 0, TestData.ToCharArray() );
			int i = 0;

			try{ LineLogic.NextLineHead(text, -1); Debug.Fail("exception must be thrown here."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			for( ; i<32; i++ )
				TestUtl.AssertEquals( 32, LineLogic.NextLineHead(text, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( 33, LineLogic.NextLineHead(text, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( 37, LineLogic.NextLineHead(text, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals( 38, LineLogic.NextLineHead(text, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 52, LineLogic.NextLineHead(text, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals( 53, LineLogic.NextLineHead(text, i) );
			for( ; i<71; i++ )
				TestUtl.AssertEquals( -1, LineLogic.NextLineHead(text, i) );
			TestUtl.AssertEquals( -1, LineLogic.NextLineHead(text, i) );
		}

		static void Test_GetLineLengthByCharIndex()
		{
			// TEST DATA:
			// --------------------
			// "keep it as simple as possible\r\n (head: 0, len:31)
			// \n                                 (head:32, len: 1)
			// but\n                              (head:33, len: 4)
			// \r                                 (head:37, len: 1)
			// not simpler."\r                    (head:38, len:14)
			// \r                                 (head:52, len: 1)
			//  - Albert Einstein                 (head:53, len:18)
			// --------------------
			const string TestData = "\"keep it as simple as possible\r\n\nbut\n\rnot simpler.\"\r\r - Albert Einstein";
			TextBuffer text = new TextBuffer( 1, 32 );
			SplitArray<int> lhi = new SplitArray<int>( 1, 8 );
			text.Insert( 0, TestData.ToCharArray() );
			int i = 0;

			for( ; i<32; i++ )
				TestUtl.AssertEquals( 32, LineLogic.GetLineLengthByCharIndex(text, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals(  1, LineLogic.GetLineLengthByCharIndex(text, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals(  4, LineLogic.GetLineLengthByCharIndex(text, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals(  1, LineLogic.GetLineLengthByCharIndex(text, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 14, LineLogic.GetLineLengthByCharIndex(text, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals(  1, LineLogic.GetLineLengthByCharIndex(text, i) );
			for( ; i<71; i++ )
				TestUtl.AssertEquals( 17, LineLogic.GetLineLengthByCharIndex(text, i) );
			TestUtl.AssertEquals( 17, LineLogic.GetLineLengthByCharIndex(text, i) ); // EOF
		}

		static void Test_GetLineIndexFromCharIndex()
		{
			SplitArray<int> lhi = new SplitArray<int>( 32, 32 );
			lhi.Add( 0 );
			lhi.Add( 32 );
			lhi.Add( 33 );
			lhi.Add( 37 );
			lhi.Add( 38 );
			lhi.Add( 52 );
			lhi.Add( 53 );

			int i=0;
			for( ; i<32; i++ )
				TestUtl.AssertEquals( 0, LineLogic.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( 1, LineLogic.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( 2, LineLogic.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals( 3, LineLogic.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 4, LineLogic.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals( 5, LineLogic.GetLineIndexFromCharIndex(lhi, i) );
			TestUtl.AssertEquals( 6, LineLogic.GetLineIndexFromCharIndex(lhi, 54) );
		}

		static void Test_LHI_Insert()
		{
			// TEST DATA:
			// --------------------
			// "keep it as simple as possible\r\n (head: 0, len:31)
			// \n                                 (head:32, len: 1)
			// but\n                              (head:33, len: 4)
			// \r                                 (head:37, len: 1)
			// not simpler."\r                    (head:38, len:14)
			// \r                                 (head:52, len: 1)
			//  - Albert Einstein                 (head:53, len:18)
			// --------------------
			const string TestData1 = "\"keep ot simpler.\"\r\r - Albert Einstein";
			const string TestData2 = "it as simple as possible\r\n\nbt\n\rn";
			TextBuffer text = new TextBuffer( 1, 1 );
			SplitArray<int> lhi = new SplitArray<int>( 1, 1 );
			SplitArray<LineDirtyState> lms = new SplitArray<LineDirtyState>( 1, 1 );
			lhi.Add( 0 ); lms.Add( LineDirtyState.Clean );

			LineLogic.LHI_Insert( lhi, lms, text, TestData1, 0 );
			text.Add( TestData1.ToCharArray() );
			Debug.Assert( lhi[0] == 0, "lhi[0] is expected to be 0 but "+lhi[0] );
			Debug.Assert( lhi[1] == 19, "lhi[1] is expected to be 19 but "+lhi[1] );
			Debug.Assert( lhi[2] == 20, "lhi[2] is expected to be 20 but "+lhi[2] );
			Debug.Assert( lhi.Count == 3, "lhi.Count is expected to be 3 but "+lhi.Count );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[2] );
			TestUtl.AssertEquals( lhi.Count, lms.Count );

			for( int i=0; i<lms.Count; i++ )
				lms[i] = LineDirtyState.Clean;
			LineLogic.LHI_Insert( lhi, lms, text, TestData2, 6 );
			text.Insert( 6, TestData2.ToCharArray() );
			Debug.Assert( lhi[0] == 0, "lhi[0] is expected to be 0 but "+lhi[0] );
			Debug.Assert( lhi[1] == 32, "lhi[1] is expected to be 32 but "+lhi[1] );
			Debug.Assert( lhi[2] == 33, "lhi[2] is expected to be 33 but "+lhi[2] );
			Debug.Assert( lhi[3] == 36, "lhi[3] is expected to be 36 but "+lhi[3] );
			Debug.Assert( lhi[4] == 37, "lhi[4] is expected to be 37 but "+lhi[4] );
			Debug.Assert( lhi[5] == 51, "lhi[5] is expected to be 51 but "+lhi[5] );
			Debug.Assert( lhi[6] == 52, "lhi[6] is expected to be 52 but "+lhi[6] );
			Debug.Assert( lhi.Count == 7, "lhi.Count is expected to be 7 but "+lhi.Count );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[2] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[3] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[4] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[5] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[6] );
			TestUtl.AssertEquals( lhi.Count, lms.Count );

			for( int i=0; i<lms.Count; i++ )
				lms[i] = LineDirtyState.Clean;
			LineLogic.LHI_Insert( lhi, lms, text, "u", 34 );
			text.Insert( 34, "u".ToCharArray() );
			Debug.Assert( lhi[0] == 0, "lhi[0] is expected to be 0 but "+lhi[0] );
			Debug.Assert( lhi[1] == 32, "lhi[1] is expected to be 32 but "+lhi[1] );
			Debug.Assert( lhi[2] == 33, "lhi[2] is expected to be 33 but "+lhi[2] );
			Debug.Assert( lhi[3] == 37, "lhi[3] is expected to be 37 but "+lhi[3] );
			Debug.Assert( lhi[4] == 38, "lhi[4] is expected to be 38 but "+lhi[4] );
			Debug.Assert( lhi[5] == 52, "lhi[5] is expected to be 52 but "+lhi[5] );
			Debug.Assert( lhi[6] == 53, "lhi[6] is expected to be 53 but "+lhi[6] );
			Debug.Assert( lhi.Count == 7, "lhi.Count is expected to be 7 but "+lhi.Count );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[0] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[1] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[2] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[3] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[4] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[5] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[6] );
			TestUtl.AssertEquals( lhi.Count, lms.Count );

			//--- special care about CR+LF ---
			// (1) insertion divides a CR+LF
			// (2) inserting text begins with LF creates a new CR+LF at left side of the insertion point
			// (3) inserting text ends with CR creates a new CR+LF at right side of the insertion point
			//--------------------------------
			// (1)+(2)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );
				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;

				LineLogic.LHI_Insert( lhi, lms, text, "\nx", 4 );
				text.Insert( 4, "\nx".ToCharArray() );
				TestUtl.AssertEquals( 3, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 5, lhi[1] );
				TestUtl.AssertEquals( 7, lhi[2] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
				TestUtl.AssertEquals( LineDirtyState.Clean, lms[2] );
			}

			// (1)+(3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );
				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;

				LineLogic.LHI_Insert( lhi, lms, text, "x\r", 4 );
				text.Insert( 4, "x\r".ToCharArray() );
				TestUtl.AssertEquals( 3, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 4, lhi[1] );
				TestUtl.AssertEquals( 7, lhi[2] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
				TestUtl.AssertEquals( LineDirtyState.Clean, lms[2] );
			}

			// (1)+(2)+(3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );
				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;

				LineLogic.LHI_Insert( lhi, lms, text, "\n\r", 4 );
				text.Insert( 4, "\n\r".ToCharArray() );
				TestUtl.AssertEquals( 3, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 5, lhi[1] );
				TestUtl.AssertEquals( 7, lhi[2] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
				TestUtl.AssertEquals( LineDirtyState.Clean, lms[2] );
			}

			// (2)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "foo\rbar", 0 );
				text.Add( "foo\rbar".ToCharArray() );
				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;

				LineLogic.LHI_Insert( lhi, lms, text, "\nx", 4 );
				text.Insert( 4, "\nx".ToCharArray() );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 5, lhi[1] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
			}

			// (3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "foo\nbar", 0 );
				text.Add( "foo\nbar".ToCharArray() );
				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;

				LineLogic.LHI_Insert( lhi, lms, text, "x\r", 3 );
				text.Insert( 3, "x\r".ToCharArray() );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 6, lhi[1] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Clean, lms[1] );
			}
		}

		static void Test_LHI_Delete()
		{
			// TEST DATA:
			// --------------------
			// "keep it as simple as possible\r\n (head: 0, len:31)
			// \n                                 (head:32, len: 1)
			// but\n                              (head:33, len: 4)
			// \r                                 (head:37, len: 1)
			// not simpler."\r                    (head:38, len:14)
			// \r                                 (head:52, len: 1)
			//  - Albert Einstein                 (head:53, len:18)
			// --------------------
			const string TestData = "\"keep it as simple as possible\r\n\nbut\n\rnot simpler.\"\r\r - Albert Einstein";
			TextBuffer text = new TextBuffer( 1, 32 );
			SplitArray<int> lhi = new SplitArray<int>( 1, 8 );
			SplitArray<LineDirtyState> lms = new SplitArray<LineDirtyState>( 1, 8 );
			lms.Add( LineDirtyState.Clean );

			// prepare
			lhi.Add( 0 );
			LineLogic.LHI_Insert( lhi, lms, text, TestData, 0 );
			text.Add( TestData.ToCharArray() );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 32, lhi[1] );
			TestUtl.AssertEquals( 33, lhi[2] );
			TestUtl.AssertEquals( 37, lhi[3] );
			TestUtl.AssertEquals( 38, lhi[4] );
			TestUtl.AssertEquals( 52, lhi[5] );
			TestUtl.AssertEquals( 53, lhi[6] );
			TestUtl.AssertEquals( 7, lhi.Count );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[2] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[3] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[4] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[5] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[6] );
			TestUtl.AssertEquals( lhi.Count, lms.Count );
			for( int i=0; i<lms.Count; i++ )
				lms[i] = LineDirtyState.Clean;
			
			//--- delete range in line ---
			// valid range
			LineLogic.LHI_Delete( lhi, lms, text, 2, 5 );
			text.Delete( 2, 5 );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 29, lhi[1] );
			TestUtl.AssertEquals( 30, lhi[2] );
			TestUtl.AssertEquals( 34, lhi[3] );
			TestUtl.AssertEquals( 35, lhi[4] );
			TestUtl.AssertEquals( 49, lhi[5] );
			TestUtl.AssertEquals( 50, lhi[6] );
			TestUtl.AssertEquals( 7, lhi.Count );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[1] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[2] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[3] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[4] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[5] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[6] );
			TestUtl.AssertEquals( lhi.Count, lms.Count );

			// invalid range (before begin to middle)
			try{ LineLogic.LHI_Delete(lhi, lms, text, -1, 5); throw new ApplicationException(); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			//--- delete range between different lines ---
			text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
			LineLogic.LHI_Insert( lhi, lms, text, TestData, 0 );
			text.Add( TestData.ToCharArray() );
			// "keep it as simple as possible\r\n (head: 0, len:31)
			// \n                                 (head:32, len: 1)
			// but\n                              (head:33, len: 4)
			// \r                                 (head:37, len: 1)
			// not simpler."\r                    (head:38, len:14)
			// \r                                 (head:52, len: 1)
			//  - Albert Einstein[EOF]            (head:53, len:18)

			// delete only one EOL code
			//----
			// "keep it as simple as possible\r\n (head: 0, len:31)
			// but\n                              (head:32, len: 4)
			// \r                                 (head:36, len: 1)
			// not simpler."\r                    (head:37, len:14)
			// \r                                 (head:51, len: 1)
			//  - Albert Einstein[EOF]            (head:52, len:18)
			//----
			for( int i=0; i<lms.Count; i++ )
				lms[i] = LineDirtyState.Clean;
			LineLogic.LHI_Delete( lhi, lms, text, 32, 33 );
			text.Delete( 32, 33 );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 32, lhi[1] );
			TestUtl.AssertEquals( 36, lhi[2] );
			TestUtl.AssertEquals( 37, lhi[3] );
			TestUtl.AssertEquals( 51, lhi[4] );
			TestUtl.AssertEquals( 52, lhi[5] );
			TestUtl.AssertEquals( 6, lhi.Count );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[0] );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[2] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[3] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[4] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[5] );
			TestUtl.AssertEquals( lhi.Count, lms.Count );

			// delete middle of the first line to not EOF pos
			//----
			// "keep it as simple as not simpler."\r (head: 0, len:35)
			// \r                                    (head:36, len: 1)
			//  - Albert Einstein[EOF]               (head:37, len:18)
			//----
			for( int i=0; i<lms.Count; i++ )
				lms[i] = LineDirtyState.Clean;
			LineLogic.LHI_Delete( lhi, lms, text, 22, 37 );
			text.Delete( 22, 37 );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 36, lhi[1] );
			TestUtl.AssertEquals( 37, lhi[2] );
			TestUtl.AssertEquals( 3, lhi.Count );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[1] );
			TestUtl.AssertEquals( LineDirtyState.Clean, lms[2] );
			TestUtl.AssertEquals( lhi.Count, lms.Count );

			// delete all
			//----
			// [EOF] (head:0, len:0)
			//----
			for( int i=0; i<lms.Count; i++ )
				lms[i] = LineDirtyState.Clean;
			LineLogic.LHI_Delete( lhi, lms, text, 0, 55 );
			text.Delete( 0, 55 );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 1, lhi.Count );
			TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
			TestUtl.AssertEquals( lhi.Count, lms.Count );

			//--- special care about CR+LF ---
			// (1) deletion creates a new CR+LF
			// (2) deletion breaks a CR+LF at the left side of the deletion range
			// (3) deletion breaks a CR+LF at the left side of the deletion range
			//--------------------------------
			// (1)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "foo\rx\nbar", 0 );
				text.Add( "foo\rx\nbar".ToCharArray() );

				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;
				LineLogic.LHI_Delete( lhi, lms, text, 4, 5 );
				text.Delete( 4, 5 );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 5, lhi[1] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Clean, lms[1] );
			}

			// (2)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );

				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;
				LineLogic.LHI_Delete( lhi, lms, text, 4, 6 );
				text.Delete( 4, 6 );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 4, lhi[1] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
			}

			// (3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );

				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;
				LineLogic.LHI_Delete( lhi, lms, text, 2, 4 );
				text.Delete( 2, 4 );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 3, lhi[1] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Clean, lms[1] );
			}

			// (1)+(2)+(3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "\r\nfoo\r\n", 0 );
				text.Add( "\r\nfoo\r\n".ToCharArray() );

				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;
				LineLogic.LHI_Delete( lhi, lms, text, 1, 6 );
				text.Delete( 1, 6 );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 2, lhi[1] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Clean, lms[1] );
			}

			//--- misc ---
			// insert "\n" after '\r' at end of document (boundary check for LHI_Insert)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "\r", 0 );
				text.Add( "\r".ToCharArray() );

				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;
				LineLogic.LHI_Insert( lhi, lms, text, "\n", 1 );
				text.Add( "\n".ToCharArray() );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 2, lhi[1] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Clean, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
			}

			// insert "\n" after '\r' at end of document (boundary check for LHI_Delete)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lms.Clear(); lms.Add( LineDirtyState.Clean );
				LineLogic.LHI_Insert( lhi, lms, text, "\r\n", 0 );
				text.Add( "\r\n".ToCharArray() );

				for( int i=0; i<lms.Count; i++ )
					lms[i] = LineDirtyState.Clean;
				LineLogic.LHI_Delete( lhi, lms, text, 1, 2 );
				text.Delete( 1, 2 );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 1, lhi[1] );
				TestUtl.AssertEquals( lhi.Count, lms.Count );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[0] );
				TestUtl.AssertEquals( LineDirtyState.Dirty, lms[1] );
			}
		}
	}
}
#endif
