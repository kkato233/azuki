#if TEST
using System;
using System.Text;
using System.Diagnostics;

namespace Sgry.Azuki.Test
{
	static class TextUtilTest
	{
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

		public static void Test()
		{
			int testNum = 0;

			Console.WriteLine( "[Test for Azuki.TextUtil]" );

			Console.WriteLine( "test {0} - LHI_Insert()", testNum++ );
			TestUtl.Do( Test_LHI_Insert );

			Console.WriteLine( "test {0} - LHI_Delete()", testNum++ );
			TestUtl.Do( Test_LHI_Delete );

			Console.WriteLine( "test {0} - NextLineHead()", testNum++ );
			TestUtl.Do( Test_NextLineHead );

			Console.WriteLine( "test {0} - PrevLineHead()", testNum++ );
			TestUtl.Do( Test_PrevLineHead );

			Console.WriteLine( "test {0} - GetLineLengthByCharIndex()", testNum++ );
			TestUtl.Do( Test_GetLineLengthByCharIndex );

			Console.WriteLine( "test {0} - GetLineRange()", testNum++ );
			TestUtl.Do( Test_GetLineRange );

			Console.WriteLine( "test {0} - GetCharIndexFromLineColumnIndex()", testNum++ );
			TestUtl.Do( Test_GetCharIndexFromLineColumnIndex );

			Console.WriteLine( "test {0} - GetLineIndexFromCharIndex()", testNum++ );
			TestUtl.Do( Test_GetLineIndexFromCharIndex );

			Console.WriteLine( "test {0} - GetLineColumnIndexFromCharIndex()", testNum++ );
			TestUtl.Do( Test_GetLineColumnIndexFromCharIndex );

			Console.WriteLine( "test {0} - LineHeadIndexFromCharIndex()", testNum++ );
			TestUtl.Do( Test_LineHeadIndexFromCharIndex );

			Console.WriteLine( "done." );
			Console.WriteLine();
		}

		static void Test_GetCharIndexFromLineColumnIndex()
		{
			TextBuffer text;
			SplitArray<int> lhi;
			SplitArray<LineDirtyState> lds;

			MakeTestData( out text, out lhi, out lds );

			TestUtl.AssertEquals(  0, TextUtil.GetCharIndexFromLineColumnIndex(text, lhi, 0,  0) );
			TestUtl.AssertEquals( 34, TextUtil.GetCharIndexFromLineColumnIndex(text, lhi, 2,  1) );
			TestUtl.AssertEquals( 71, TextUtil.GetCharIndexFromLineColumnIndex(text, lhi, 6, 18) );

			try
			{
				TextUtil.GetCharIndexFromLineColumnIndex( text, lhi, 6, 19 );
				TestUtl.Fail( "exception must be thrown here." );
			}
			catch( Exception ex )
			{
				TestUtl.AssertType<AssertException>( ex );
			}

			try
			{
				TextUtil.GetCharIndexFromLineColumnIndex( text, lhi, 0, 100 );
				TestUtl.Fail( "exception must be thrown here." );
			}
			catch( Exception ex )
			{
				TestUtl.AssertType<AssertException>( ex );
			}
		}

		static void Test_NextLineHead()
		{
			TextBuffer text = new TextBuffer( 1, 32 );

			text.Insert( 0, TestData.ToCharArray() );

			try
			{
				TextUtil.NextLineHead( text, -1 );
				TestUtl.Fail( "exception must be thrown here." );
			}
			catch( Exception ex )
			{
				TestUtl.AssertType<AssertException>( ex );
			}

			int i = 0;
			for( ; i<32; i++ )
				TestUtl.AssertEquals( 32, TextUtil.NextLineHead(text, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( 33, TextUtil.NextLineHead(text, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( 37, TextUtil.NextLineHead(text, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals( 38, TextUtil.NextLineHead(text, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 52, TextUtil.NextLineHead(text, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals( 53, TextUtil.NextLineHead(text, i) );
			for( ; i<71; i++ )
				TestUtl.AssertEquals( -1, TextUtil.NextLineHead(text, i) );
			TestUtl.AssertEquals( -1, TextUtil.NextLineHead(text, i) );
		}

		static void Test_PrevLineHead()
		{
			TextBuffer text = new TextBuffer( 1, 32 );

			text.Insert( 0, TestData.ToCharArray() );

			int i=71;
			for( ; 53<=i; i-- )
				TestUtl.AssertEquals( 53, TextUtil.PrevLineHead(text, i) );
			for( ; 52<=i; i-- )
				TestUtl.AssertEquals( 52, TextUtil.PrevLineHead(text, i) );
			for( ; 38<=i; i-- )
				TestUtl.AssertEquals( 38, TextUtil.PrevLineHead(text, i) );
			for( ; 37<=i; i-- )
				TestUtl.AssertEquals( 37, TextUtil.PrevLineHead(text, i) );
			for( ; 33<=i; i-- )
				TestUtl.AssertEquals( 33, TextUtil.PrevLineHead(text, i) );
			for( ; 32<=i; i-- )
				TestUtl.AssertEquals( 32, TextUtil.PrevLineHead(text, i) );
			for( ; 0<=i; i-- )
				TestUtl.AssertEquals( 0,  TextUtil.PrevLineHead(text, i) );
		}

		static void Test_GetLineLengthByCharIndex()
		{
			TextBuffer text = new TextBuffer( 1, 32 );
			int i = 0;

			text.Insert( 0, TestData.ToCharArray() );

			for( ; i<32; i++ )
				TestUtl.AssertEquals( 32, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals(  4, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 14, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<71; i++ )
				TestUtl.AssertEquals( 17, TextUtil.GetLineLengthByCharIndex(text, i) );
			TestUtl.AssertEquals( 17, TextUtil.GetLineLengthByCharIndex(text, i) ); // EOF
		}

		static void Test_GetLineRange()
		{
			int head, end;
			TextBuffer text;
			SplitArray<int> lhi;
			SplitArray<LineDirtyState> lds;

			MakeTestData( out text, out lhi, out lds );

			TextUtil.GetLineRange( text, lhi, 0, false, out head, out end );
			TestUtl.AssertEquals( 0, head );
			TestUtl.AssertEquals( 30, end );
			TextUtil.GetLineRange( text, lhi, 0, true, out head, out end );
			TestUtl.AssertEquals( 0, head );
			TestUtl.AssertEquals( 32, end );

			TextUtil.GetLineRange( text, lhi, 1, false, out head, out end );
			TestUtl.AssertEquals( 32, head );
			TestUtl.AssertEquals( 32, end );
			TextUtil.GetLineRange( text, lhi, 1, true, out head, out end );
			TestUtl.AssertEquals( 32, head );
			TestUtl.AssertEquals( 33, end );

			TextUtil.GetLineRange( text, lhi, 2, false, out head, out end );
			TestUtl.AssertEquals( 33, head );
			TestUtl.AssertEquals( 36, end );
			TextUtil.GetLineRange( text, lhi, 2, true, out head, out end );
			TestUtl.AssertEquals( 33, head );
			TestUtl.AssertEquals( 37, end );

			TextUtil.GetLineRange( text, lhi, 3, false, out head, out end );
			TestUtl.AssertEquals( 37, head );
			TestUtl.AssertEquals( 37, end );
			TextUtil.GetLineRange( text, lhi, 3, true, out head, out end );
			TestUtl.AssertEquals( 37, head );
			TestUtl.AssertEquals( 38, end );

			TextUtil.GetLineRange( text, lhi, 4, false, out head, out end );
			TestUtl.AssertEquals( 38, head );
			TestUtl.AssertEquals( 51, end );
			TextUtil.GetLineRange( text, lhi, 4, true, out head, out end );
			TestUtl.AssertEquals( 38, head );
			TestUtl.AssertEquals( 52, end );

			TextUtil.GetLineRange( text, lhi, 5, false, out head, out end );
			TestUtl.AssertEquals( 52, head );
			TestUtl.AssertEquals( 52, end );
			TextUtil.GetLineRange( text, lhi, 5, true, out head, out end );
			TestUtl.AssertEquals( 52, head );
			TestUtl.AssertEquals( 53, end );

			TextUtil.GetLineRange( text, lhi, 6, false, out head, out end );
			TestUtl.AssertEquals( 53, head );
			TestUtl.AssertEquals( 71, end );
			TextUtil.GetLineRange( text, lhi, 6, true, out head, out end );
			TestUtl.AssertEquals( 53, head );
			TestUtl.AssertEquals( 71, end );
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
				TestUtl.AssertEquals( 0, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( 1, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( 2, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals( 3, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 4, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals( 5, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			TestUtl.AssertEquals( 6, TextUtil.GetLineIndexFromCharIndex(lhi, 54) );
		}

		static void Test_GetLineColumnIndexFromCharIndex()
		{
			TextBuffer text;
			SplitArray<int> lhi;
			int l, c;

			MakeTestData( out text, out lhi );

			TextUtil.GetLineColumnIndexFromCharIndex( text, lhi, 0, out l, out c );
			TestUtl.AssertEquals( 0, l );
			TestUtl.AssertEquals( 0, c );
			TextUtil.GetLineColumnIndexFromCharIndex( text, lhi, 2, out l, out c );
			TestUtl.AssertEquals( 0, l );
			TestUtl.AssertEquals( 2, c );
			TextUtil.GetLineColumnIndexFromCharIndex( text, lhi, 40, out l, out c );
			TestUtl.AssertEquals( 4, l );
			TestUtl.AssertEquals( 2, c );
			TextUtil.GetLineColumnIndexFromCharIndex( text, lhi, 71, out l, out c ); // 71 --> EOF
			TestUtl.AssertEquals( 6, l );
			TestUtl.AssertEquals( 18, c );
			try
			{
				TextUtil.GetLineColumnIndexFromCharIndex(text, lhi, 72, out l, out c);
				TestUtl.Fail("exception must be thrown here.");
			}
			catch( Exception ex )
			{
				TestUtl.AssertType<AssertException>(ex);
			}
		}

		static void Test_LineHeadIndexFromCharIndex()
		{
			TextBuffer text;
			SplitArray<int> lhi;

			MakeTestData( out text, out lhi );

			int i=0;
			for( ; i<32; i++ )
				TestUtl.AssertEquals(  0, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( 32, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( 33, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals( 37, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 38, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals( 52, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<=71; i++ )
				TestUtl.AssertEquals( 53, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			try{ TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i); TestUtl.Fail("exception must be thrown here."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
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
			SplitArray<LineDirtyState> lds = new SplitArray<LineDirtyState>( 1, 1 );
			lhi.Add( 0 ); lds.Add( LineDirtyState.Clean );

			TextUtil.LHI_Insert( lhi, lds, text, TestData1, 0 );
			text.Add( TestData1.ToCharArray() );
			TestUtl.AssertEquals( "0 19 20", lhi.ToString() );
			TestUtl.AssertEquals( "DDD", MakeLdsText(lds) );

			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			TextUtil.LHI_Insert( lhi, lds, text, TestData2, 6 );
			text.Insert( 6, TestData2.ToCharArray() );
			TestUtl.AssertEquals( "0 32 33 36 37 51 52", lhi.ToString() );
			TestUtl.AssertEquals( "DDDDDCC", MakeLdsText(lds) );

			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			TextUtil.LHI_Insert( lhi, lds, text, "u", 34 );
			text.Insert( 34, "u".ToCharArray() );
			TestUtl.AssertEquals( "0 32 33 37 38 52 53", lhi.ToString() );
			TestUtl.AssertEquals( "CCDCCCC", MakeLdsText(lds) );

			//--- special care about CR+LF ---
			// (1) insertion divides a CR+LF
			// (2) inserting text begins with LF creates a new CR+LF at left side of the insertion point
			// (3) inserting text ends with CR creates a new CR+LF at right side of the insertion point
			//--------------------------------
			// (1)+(2)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );
				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;

				TextUtil.LHI_Insert( lhi, lds, text, "\nx", 4 );
				text.Insert( 4, "\nx".ToCharArray() );
				TestUtl.AssertEquals( "0 5 7", lhi.ToString() );
				TestUtl.AssertEquals( "DDC", MakeLdsText(lds) );
			}

			// (1)+(3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );
				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;

				TextUtil.LHI_Insert( lhi, lds, text, "x\r", 4 );
				text.Insert( 4, "x\r".ToCharArray() );
				TestUtl.AssertEquals( "0 4 7", lhi.ToString() );
				TestUtl.AssertEquals( "DDC", MakeLdsText(lds) );
			}

			// (1)+(2)+(3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );
				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;

				TextUtil.LHI_Insert( lhi, lds, text, "\n\r", 4 );
				text.Insert( 4, "\n\r".ToCharArray() );
				TestUtl.AssertEquals( "0 5 7", lhi.ToString() );
				TestUtl.AssertEquals( "DDC", MakeLdsText(lds) );
			}

			// (2)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "foo\rbar", 0 );
				text.Add( "foo\rbar".ToCharArray() );
				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;

				TextUtil.LHI_Insert( lhi, lds, text, "\nx", 4 );
				text.Insert( 4, "\nx".ToCharArray() );
				TestUtl.AssertEquals( "0 5", lhi.ToString() );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// (3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "foo\nbar", 0 );
				text.Add( "foo\nbar".ToCharArray() );
				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;

				TextUtil.LHI_Insert( lhi, lds, text, "x\r", 3 );
				text.Insert( 3, "x\r".ToCharArray() );
				TestUtl.AssertEquals( "0 6", lhi.ToString() );
				TestUtl.AssertEquals( "DC", MakeLdsText(lds) );
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
			SplitArray<LineDirtyState> lds = new SplitArray<LineDirtyState>( 1, 8 );
			lds.Add( LineDirtyState.Clean );

			// prepare
			lhi.Add( 0 );
			TextUtil.LHI_Insert( lhi, lds, text, TestData, 0 );
			text.Add( TestData.ToCharArray() );
			TestUtl.AssertEquals( "0 32 33 37 38 52 53", lhi.ToString() );
			TestUtl.AssertEquals( "DDDDDDD", MakeLdsText(lds) );
			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			
			//--- delete range in line ---
			// valid range
			TextUtil.LHI_Delete( lhi, lds, text, 2, 5 );
			text.RemoveRange( 2, 5 );
			TestUtl.AssertEquals( "0 29 30 34 35 49 50", lhi.ToString() );
			TestUtl.AssertEquals( "DCCCCCC", MakeLdsText(lds) );

			// invalid range (before begin to middle)
			try{ TextUtil.LHI_Delete(lhi, lds, text, -1, 5); throw new ApplicationException(); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }

			//--- delete range between different lines ---
			text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
			TextUtil.LHI_Insert( lhi, lds, text, TestData, 0 );
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
			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			TextUtil.LHI_Delete( lhi, lds, text, 32, 33 );
			text.RemoveAt( 32 );
			TestUtl.AssertEquals( "0 32 36 37 51 52", lhi.ToString() );
			TestUtl.AssertEquals( "CDCCCC", MakeLdsText(lds) );

			// delete middle of the first line to not EOF pos
			//----
			// "keep it as simple as not simpler."\r (head: 0, len:35)
			// \r                                    (head:36, len: 1)
			//  - Albert Einstein[EOF]               (head:37, len:18)
			//----
			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			TextUtil.LHI_Delete( lhi, lds, text, 22, 37 );
			text.RemoveRange( 22, 37 );
			TestUtl.AssertEquals( "0 36 37", lhi.ToString() );
			TestUtl.AssertEquals( "DCC", MakeLdsText(lds) );

			// delete all
			//----
			// [EOF] (head:0, len:0)
			//----
			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			TextUtil.LHI_Delete( lhi, lds, text, 0, 55 );
			text.RemoveRange( 0, 55 );
			TestUtl.AssertEquals( "0", lhi.ToString() );
			TestUtl.AssertEquals( "D", MakeLdsText(lds) );

			//--- special care about CR+LF ---
			// (1) deletion creates a new CR+LF
			// (2) deletion breaks a CR+LF at the left side of the deletion range
			// (3) deletion breaks a CR+LF at the left side of the deletion range
			//--------------------------------
			// (1)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "foo\rx\nbar", 0 );
				text.Add( "foo\rx\nbar".ToCharArray() );

				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;
				TextUtil.LHI_Delete( lhi, lds, text, 4, 5 );
				text.RemoveRange( 4, 5 );
				TestUtl.AssertEquals( "0 5", lhi.ToString() );
				TestUtl.AssertEquals( "DC", MakeLdsText(lds) );
			}

			// (2)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );

				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;
				TextUtil.LHI_Delete( lhi, lds, text, 4, 6 );
				text.RemoveRange( 4, 6 );
				TestUtl.AssertEquals( "0 4", lhi.ToString() );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// (3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "foo\r\nbar", 0 );
				text.Add( "foo\r\nbar".ToCharArray() );

				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;
				TextUtil.LHI_Delete( lhi, lds, text, 2, 4 );
				text.RemoveRange( 2, 4 );
				TestUtl.AssertEquals( "0 3", lhi.ToString() );
				TestUtl.AssertEquals( "DC", MakeLdsText(lds) );
			}

			// (1)+(2)+(3)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "\r\nfoo\r\n", 0 );
				text.Add( "\r\nfoo\r\n".ToCharArray() );

				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;
				TextUtil.LHI_Delete( lhi, lds, text, 1, 6 );
				text.RemoveRange( 1, 6 );
				TestUtl.AssertEquals( "0 2", lhi.ToString() );
				TestUtl.AssertEquals( "DC", MakeLdsText(lds) );
			}

			//--- misc ---
			// insert "\n" after '\r' at end of document (boundary check for LHI_Insert)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "\r", 0 );
				text.Add( "\r".ToCharArray() );

				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;
				TextUtil.LHI_Insert( lhi, lds, text, "\n", 1 );
				text.Add( "\n".ToCharArray() );
				TestUtl.AssertEquals( "0 2", lhi.ToString() );
				TestUtl.AssertEquals( "CD", MakeLdsText(lds) );
			}

			// insert "\n" after '\r' at end of document (boundary check for LHI_Delete)
			{
				text.Clear(); lhi.Clear(); lhi.Add( 0 ); lds.Clear(); lds.Add( LineDirtyState.Clean );
				TextUtil.LHI_Insert( lhi, lds, text, "\r\n", 0 );
				text.Add( "\r\n".ToCharArray() );

				for( int i=0; i<lds.Count; i++ )
					lds[i] = LineDirtyState.Clean;
				TextUtil.LHI_Delete( lhi, lds, text, 1, 2 );
				text.RemoveRange( 1, 2 );
				TestUtl.AssertEquals( "0 1", lhi.ToString() );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}
		}

		static void MakeTestData( out TextBuffer text, out SplitArray<int> lhi )
		{
			SplitArray<LineDirtyState> lds = new SplitArray<LineDirtyState>( 8 );

			MakeTestData( out text, out lhi, out lds );
		}

		static void MakeTestData( out TextBuffer text, out SplitArray<int> lhi, out SplitArray<LineDirtyState> lds )
		{
			text = new TextBuffer( 1, 1 );
			lhi = new SplitArray<int>( 1, 8 );
			lds = new SplitArray<LineDirtyState>( 1 );

			lhi.Add( 0 );
			lds.Add( LineDirtyState.Clean );

			TextUtil.LHI_Insert( lhi, lds, text, TestData, 0 );
			text.Insert( 0, TestData.ToCharArray() );
		}

		static string MakeLdsText( SplitArray<LineDirtyState> lds )
		{
			StringBuilder buf = new StringBuilder( 32 );

			for( int i=0; i<lds.Count; i++ )
			{
				char ch = '#';

				switch ( lds[i] )
				{
					case LineDirtyState.Clean:	ch = 'C';	break;
					case LineDirtyState.Dirty:	ch = 'D';	break;
					case LineDirtyState.Cleaned:ch = 'S';	break;
					default:	Debug.Fail("invalid LineDirtyState enum value");	break;
				}
				buf.Append( ch );
			}
			return buf.ToString();
		}
	}
}
#endif
