// 2008-11-01
#if DEBUG
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
			int l, c, head, end;
			int i;
			lhi.Add( 0 );

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

			LineLogic.LHI_Insert( lhi, text, TestData1, 0 );
			text.Add( TestData1.ToCharArray() );
			LineLogic.LHI_Insert( lhi, text, TestData2, 6 );
			text.Insert( 6, TestData2.ToCharArray() );
			LineLogic.LHI_Insert( lhi, text, "u", 34 );
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
				Debug.Assert( LineLogic.PrevLineHead(text, i) == 53, "expected 53 but "+LineLogic.PrevLineHead(text, i)+" (i="+i+")" );
			for( ; 52<=i; i-- )
				Debug.Assert( LineLogic.PrevLineHead(text, i) == 52, "expected 52 but "+LineLogic.PrevLineHead(text, i)+" (i="+i+")" );
			for( ; 38<=i; i-- )
				Debug.Assert( LineLogic.PrevLineHead(text, i) == 38, "expected 38 but "+LineLogic.PrevLineHead(text, i)+" (i="+i+")" );
			for( ; 37<=i; i-- )
				Debug.Assert( LineLogic.PrevLineHead(text, i) == 37, "expected 37 but "+LineLogic.PrevLineHead(text, i)+" (i="+i+")" );
			for( ; 33<=i; i-- )
				Debug.Assert( LineLogic.PrevLineHead(text, i) == 33, "expected 33 but "+LineLogic.PrevLineHead(text, i)+" (i="+i+")" );
			for( ; 32<=i; i-- )
				Debug.Assert( LineLogic.PrevLineHead(text, i) == 32, "expected 32 but "+LineLogic.PrevLineHead(text, i)+" (i="+i+")" );
			for( ; 0<=i; i-- )
				Debug.Assert( LineLogic.PrevLineHead(text, i) == 0, "expected 0 but "+LineLogic.PrevLineHead(text, i)+" (i="+i+")" );

			//
			// GetLineLengthByCharIndex
			//
			Console.WriteLine( "test {0} - GetLineLengthByCharIndex()", testNum++ );
			i=0;
			for( ; i<32; i++ )
				Debug.Assert( LineLogic.GetLineLengthByCharIndex(text, i) == 32, "expected 32 but "+LineLogic.GetLineLengthByCharIndex(text, i)+" (i="+i+")" );
			for( ; i<33; i++ )
				Debug.Assert( LineLogic.GetLineLengthByCharIndex(text, i) == 1, "expected 1 but "+LineLogic.GetLineLengthByCharIndex(text, i)+" (i="+i+")" );
			for( ; i<37; i++ )
				Debug.Assert( LineLogic.GetLineLengthByCharIndex(text, i) == 4, "expected 4 but "+LineLogic.GetLineLengthByCharIndex(text, i)+" (i="+i+")" );
			for( ; i<38; i++ )
				Debug.Assert( LineLogic.GetLineLengthByCharIndex(text, i) == 1, "expected 1 but "+LineLogic.GetLineLengthByCharIndex(text, i)+" (i="+i+")" );
			for( ; i<52; i++ )
				Debug.Assert( LineLogic.GetLineLengthByCharIndex(text, i) == 14, "expected 14 but "+LineLogic.GetLineLengthByCharIndex(text, i)+" (i="+i+")" );
			for( ; i<53; i++ )
				Debug.Assert( LineLogic.GetLineLengthByCharIndex(text, i) == 1, "expected 1 but "+LineLogic.GetLineLengthByCharIndex(text, i)+" (i="+i+")" );
			for( ; i<71; i++ )
				Debug.Assert( LineLogic.GetLineLengthByCharIndex(text, i) == 17, "expected 17 but "+LineLogic.GetLineLengthByCharIndex(text, i)+" (i="+i+")" );
			Debug.Assert( LineLogic.GetLineLengthByCharIndex(text, i) == 17, "expected -1 but "+LineLogic.GetLineLengthByCharIndex(text, i)+" (i="+i+")" ); // EOF

			//
			// GetLineRangeWithEol
			//
			Console.WriteLine( "test {0} - GetLineRangeWithEol()", testNum++ );
			LineLogic.GetLineRangeWithEol( text, lhi, 0, out head, out end );
			Debug.Assert( head == 0 && end == 32, String.Format("expected (0, 32) but ({0}, {1})", head, end) );
			LineLogic.GetLineRangeWithEol( text, lhi, 1, out head, out end );
			Debug.Assert( head == 32 && end == 33, String.Format("expected (32, 33) but ({0}, {1})", head, end) );
			LineLogic.GetLineRangeWithEol( text, lhi, 2, out head, out end );
			Debug.Assert( head == 33 && end == 37, String.Format("expected (33, 37) but ({0}, {1})", head, end) );
			LineLogic.GetLineRangeWithEol( text, lhi, 3, out head, out end );
			Debug.Assert( head == 37 && end == 38, String.Format("expected (37, 38) but ({0}, {1})", head, end) );
			LineLogic.GetLineRangeWithEol( text, lhi, 4, out head, out end );
			Debug.Assert( head == 38 && end == 52, String.Format("expected (38, 52) but ({0}, {1})", head, end) );
			LineLogic.GetLineRangeWithEol( text, lhi, 5, out head, out end );
			Debug.Assert( head == 52 && end == 53, String.Format("expected (52, 53) but ({0}, {1})", head, end) );
			LineLogic.GetLineRangeWithEol( text, lhi, 6, out head, out end );
			Debug.Assert( head == 53 && end == 71, String.Format("expected (53, 71) but ({0}, {1})", head, end) );

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
			Debug.Assert( LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 0, 0) == 0, "expected 0 but "+LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 0, 0) );
			Debug.Assert( LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 2, 1) == 34, "expected 34 but "+LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 2, 1) );
			Debug.Assert( LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 6, 18) == 71, "expected 71 but "+LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 6, 18) ); // EOF
			try{ LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 6, 19); Debug.Fail("exception must be thrown here."); }
			catch( Exception ex ){ Debug.Assert( ex is ArgumentException, "unexpected type of exception thrown:"+ex ); }
			try{ LineLogic.GetCharIndexFromLineColumnIndex(text, lhi, 0, 100); Debug.Fail("exception must be thrown here."); }
			catch( Exception ex ){ Debug.Assert( ex is ArgumentException ); }

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
			Debug.Assert( l == 0 && c == 0, String.Format("expected (0, 0) but ({0}, {1})", l, c) );
			LineLogic.GetLineColumnIndexFromCharIndex( text, lhi, 2, out l, out c );
			Debug.Assert( l == 0 && c == 2, String.Format("expected (0, 2) but ({0}, {1})", l, c) );
			LineLogic.GetLineColumnIndexFromCharIndex( text, lhi, 40, out l, out c );
			Debug.Assert( l == 4 && c == 2, String.Format("expected (4, 2) but ({0}, {1})", l, c) );
			LineLogic.GetLineColumnIndexFromCharIndex( text, lhi, 71, out l, out c ); // 71 --> EOF
			Debug.Assert( l == 6 && c == 18, String.Format("expected (6, 18) but ({0}, {1})", l, c) );
			//try{ LineLogic.GetLineColumnIndexFromCharIndex(text, lhi, 72, out l, out c); DebugUtl.Fail("exception must be thrown here."); }
			//catch( Exception ex ){ DebugUtl.Assert( ex is ArgumentException, "unexpected type of exception thrown:"+ex ); }

			//
			// LineHeadIndexFromCharIndex
			//
			Console.WriteLine( "test {0} - LineHeadIndexFromCharIndex()", testNum++ );
			i=0;
			for( ; i<32; i++ )
				Debug.Assert( LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) == 0, "expected 0 but "+LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i)+" (i="+i+")" );
			for( ; i<33; i++ )
				Debug.Assert( LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) == 32, "expected 32 but "+LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i)+" (i="+i+")" );
			for( ; i<37; i++ )
				Debug.Assert( LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) == 33, "expected 33 but "+LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i)+" (i="+i+")" );
			for( ; i<38; i++ )
				Debug.Assert( LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) == 37, "expected 37 but "+LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i)+" (i="+i+")" );
			for( ; i<52; i++ )
				Debug.Assert( LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) == 38, "expected 38 but "+LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i)+" (i="+i+")" );
			for( ; i<53; i++ )
				Debug.Assert( LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) == 52, "expected 52 but "+LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i)+" (i="+i+")" );
			for( ; i<=71; i++ )
				Debug.Assert( LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i) == 53, "expected 53 but "+LineLogic.GetLineHeadIndexFromCharIndex(text, lhi, i)+" (i="+i+")" );
			//MUST_FAIL//LineLogic.LineHeadIndexFromCharIndex(text, lhi, i);

			Console.WriteLine( "done." );
			Console.WriteLine();
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

			try{ LineLogic.NextLineHead(text, -1); DebugUtl.Fail("exception must be thrown here."); }
			catch( Exception ex ){ DebugUtl.Assert(ex is IndexOutOfRangeException); }

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
			lhi.Add( 0 );

			LineLogic.LHI_Insert( lhi, text, TestData1, 0 );
			text.Add( TestData1.ToCharArray() );
			Debug.Assert( lhi[0] == 0, "lhi[0] is expected to be 0 but "+lhi[0] );
			Debug.Assert( lhi[1] == 19, "lhi[1] is expected to be 19 but "+lhi[1] );
			Debug.Assert( lhi[2] == 20, "lhi[2] is expected to be 20 but "+lhi[2] );
			Debug.Assert( lhi.Count == 3, "lhi.Count is expected to be 3 but "+lhi.Count );

			LineLogic.LHI_Insert( lhi, text, TestData2, 6 );
			text.Insert( 6, TestData2.ToCharArray() );
			Debug.Assert( lhi[0] == 0, "lhi[0] is expected to be 0 but "+lhi[0] );
			Debug.Assert( lhi[1] == 32, "lhi[1] is expected to be 32 but "+lhi[1] );
			Debug.Assert( lhi[2] == 33, "lhi[2] is expected to be 33 but "+lhi[2] );
			Debug.Assert( lhi[3] == 36, "lhi[3] is expected to be 36 but "+lhi[3] );
			Debug.Assert( lhi[4] == 37, "lhi[4] is expected to be 37 but "+lhi[4] );
			Debug.Assert( lhi[5] == 51, "lhi[5] is expected to be 51 but "+lhi[5] );
			Debug.Assert( lhi[6] == 52, "lhi[6] is expected to be 52 but "+lhi[6] );
			Debug.Assert( lhi.Count == 7, "lhi.Count is expected to be 7 but "+lhi.Count );

			LineLogic.LHI_Insert( lhi, text, "u", 34 );
			text.Insert( 34, "u".ToCharArray() );
			Debug.Assert( lhi[0] == 0, "lhi[0] is expected to be 0 but "+lhi[0] );
			Debug.Assert( lhi[1] == 32, "lhi[1] is expected to be 32 but "+lhi[1] );
			Debug.Assert( lhi[2] == 33, "lhi[2] is expected to be 33 but "+lhi[2] );
			Debug.Assert( lhi[3] == 37, "lhi[3] is expected to be 37 but "+lhi[3] );
			Debug.Assert( lhi[4] == 38, "lhi[4] is expected to be 38 but "+lhi[4] );
			Debug.Assert( lhi[5] == 52, "lhi[5] is expected to be 52 but "+lhi[5] );
			Debug.Assert( lhi[6] == 53, "lhi[6] is expected to be 53 but "+lhi[6] );
			Debug.Assert( lhi.Count == 7, "lhi.Count is expected to be 7 but "+lhi.Count );

			//--- special care about CR+LF ---
			// (1) insertion divides a CR+LF
			// (2) inserting text begins with LF creates a new CR+LF at left side of the insertion point
			// (3) inserting text ends with CR creates a new CR+LF at right side of the insertion point
			//--------------------------------
			// (1)+(2)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 );
				LineLogic.LHI_Insert( lhi, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );

				LineLogic.LHI_Insert( lhi, text, "\nx", 4 );
				text.Insert( 4, "\nx".ToCharArray() );
				TestUtl.AssertEquals( 3, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 5, lhi[1] );
				TestUtl.AssertEquals( 7, lhi[2] );
			}

			// (1)+(3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 );
				LineLogic.LHI_Insert( lhi, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );

				LineLogic.LHI_Insert( lhi, text, "x\r", 4 );
				text.Insert( 4, "x\r".ToCharArray() );
				TestUtl.AssertEquals( 3, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 4, lhi[1] );
				TestUtl.AssertEquals( 7, lhi[2] );
			}

			// (1)+(2)+(3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 );
				LineLogic.LHI_Insert( lhi, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );

				LineLogic.LHI_Insert( lhi, text, "\n\r", 4 );
				text.Insert( 4, "\n\r".ToCharArray() );
				TestUtl.AssertEquals( 3, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 5, lhi[1] );
				TestUtl.AssertEquals( 7, lhi[2] );
			}

			// (2)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 );
				LineLogic.LHI_Insert( lhi, text, "foo\rbar", 0 );
				text.Add( "foo\rbar".ToCharArray() );

				LineLogic.LHI_Insert( lhi, text, "\nx", 4 );
				text.Insert( 4, "\nx".ToCharArray() );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 5, lhi[1] );
			}

			// (3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 );
				LineLogic.LHI_Insert( lhi, text, "foo\nbar", 0 );
				text.Add( "foo\nbar".ToCharArray() );

				LineLogic.LHI_Insert( lhi, text, "x\r", 3 );
				text.Insert( 3, "x\r".ToCharArray() );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 6, lhi[1] );
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

			// prepare
			lhi.Add( 0 );
			LineLogic.LHI_Insert( lhi, text, TestData, 0 );
			text.Add( TestData.ToCharArray() );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 32, lhi[1] );
			TestUtl.AssertEquals( 33, lhi[2] );
			TestUtl.AssertEquals( 37, lhi[3] );
			TestUtl.AssertEquals( 38, lhi[4] );
			TestUtl.AssertEquals( 52, lhi[5] );
			TestUtl.AssertEquals( 53, lhi[6] );
			TestUtl.AssertEquals( 7, lhi.Count );
			
			//--- delete range in line ---
			// valid range
			LineLogic.LHI_Delete( lhi, text, 2, 5 );
			text.Delete( 2, 5 );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 29, lhi[1] );
			TestUtl.AssertEquals( 30, lhi[2] );
			TestUtl.AssertEquals( 34, lhi[3] );
			TestUtl.AssertEquals( 35, lhi[4] );
			TestUtl.AssertEquals( 49, lhi[5] );
			TestUtl.AssertEquals( 50, lhi[6] );
			TestUtl.AssertEquals( 7, lhi.Count );

			// invalid range (before begin to middle)
			try{ LineLogic.LHI_Delete(lhi, text, -1, 5); throw new ApplicationException(); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex, typeof(AssertException)); }

			//--- delete range between different lines ---
			lhi.Clear();
			text.Clear();
			lhi.Add( 0 );
			LineLogic.LHI_Insert( lhi, text, TestData, 0 );
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
			LineLogic.LHI_Delete( lhi, text, 32, 33 );
			text.Delete( 32, 33 );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 32, lhi[1] );
			TestUtl.AssertEquals( 36, lhi[2] );
			TestUtl.AssertEquals( 37, lhi[3] );
			TestUtl.AssertEquals( 51, lhi[4] );
			TestUtl.AssertEquals( 52, lhi[5] );
			TestUtl.AssertEquals( 6, lhi.Count );

			// delete middle of the first line to not EOF pos
			//----
			// "keep it as simple as not simpler."\r (head: 0, len:35)
			// \r                                    (head:36, len: 1)
			//  - Albert Einstein[EOF]               (head:37, len:18)
			//----
			LineLogic.LHI_Delete( lhi, text, 22, 37 );
			text.Delete( 22, 37 );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 36, lhi[1] );
			TestUtl.AssertEquals( 37, lhi[2] );
			TestUtl.AssertEquals( 3, lhi.Count );

			// delete all
			//----
			// [EOF] (head:0, len:0)
			//----
			LineLogic.LHI_Delete( lhi, text, 0, 55 );
			text.Delete( 0, 55 );
			TestUtl.AssertEquals(  0, lhi[0] );
			TestUtl.AssertEquals( 1, lhi.Count );

			//--- special care about CR+LF ---
			// (1) deletion creates a new CR+LF
			// (2) deletion breaks a CR+LF at the left side of the deletion range
			// (3) deletion breaks a CR+LF at the left side of the deletion range
			//--------------------------------
			// (1)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 );
				LineLogic.LHI_Insert( lhi, text, "foo\rx\nbar", 0 );
				text.Add( "foo\rx\nbar".ToCharArray() );

				LineLogic.LHI_Delete( lhi, text, 4, 5 );
				text.Delete( 4, 5 );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 5, lhi[1] );
			}

			// (2)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 );
				LineLogic.LHI_Insert( lhi, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );

				LineLogic.LHI_Delete( lhi, text, 4, 6 );
				text.Delete( 4, 6 );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 4, lhi[1] );
			}

			// (3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 );
				LineLogic.LHI_Insert( lhi, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );

				LineLogic.LHI_Delete( lhi, text, 2, 4 );
				text.Delete( 2, 4 );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 3, lhi[1] );
			}

			// (1)+(2)+(3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 );
				LineLogic.LHI_Insert( lhi, text, "\r\nfoo\r\n", 0 );
				text.Add( "\r\nfoo\r\n".ToCharArray() );

				LineLogic.LHI_Delete( lhi, text, 1, 6 );
				text.Delete( 1, 6 );
				TestUtl.AssertEquals( 2, lhi.Count );
				TestUtl.AssertEquals( 0, lhi[0] );
				TestUtl.AssertEquals( 2, lhi[1] );
			}
		}
	}
}
#endif
