using System.Text;
using System.Diagnostics;
#if USEING_NUNIT
using Assert = NUnit.Framework.Assert;
using TestClassAttribute = NUnit.Framework.TestFixtureAttribute;
using TestMethodAttribute = NUnit.Framework.TestAttribute;
#else
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using TestClassAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TestMethodAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#endif

namespace Sgry.Azuki.Test
{
	[TestClass]
	public class TextUtilTest
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

		[TestMethod]
		public void GetCharIndexFromLineColumnIndex()
		{
			TextBuffer text;
			SplitArray<int> lhi;
			SplitArray<LineDirtyState> lds;

			MakeTestData( out text, out lhi, out lds );

			Assert.AreEqual(  0, TextUtil.GetCharIndexFromLineColumnIndex(text, lhi, 0,  0) );
			Assert.AreEqual( 34, TextUtil.GetCharIndexFromLineColumnIndex(text, lhi, 2,  1) );
			Assert.AreEqual( 71, TextUtil.GetCharIndexFromLineColumnIndex(text, lhi, 6, 18) );

			MyAssert.Throws<AssertException>( delegate{
				TextUtil.GetCharIndexFromLineColumnIndex( text, lhi, 6, 19 );
			} );
			MyAssert.Throws<AssertException>( delegate{
				TextUtil.GetCharIndexFromLineColumnIndex( text, lhi, 0, 100 );
			} );
		}

		[TestMethod]
		public void NextLineHead()
		{
			TextBuffer text = new TextBuffer( 1, 32 );

			text.Insert( 0, TestData.ToCharArray() );

			MyAssert.Throws<AssertException>( delegate{
				TextUtil.NextLineHead( text, -1 );
			} );

			int i = 0;
			for( ; i<32; i++ )
				Assert.AreEqual( 32, TextUtil.NextLineHead(text, i) );
			for( ; i<33; i++ )
				Assert.AreEqual( 33, TextUtil.NextLineHead(text, i) );
			for( ; i<37; i++ )
				Assert.AreEqual( 37, TextUtil.NextLineHead(text, i) );
			for( ; i<38; i++ )
				Assert.AreEqual( 38, TextUtil.NextLineHead(text, i) );
			for( ; i<52; i++ )
				Assert.AreEqual( 52, TextUtil.NextLineHead(text, i) );
			for( ; i<53; i++ )
				Assert.AreEqual( 53, TextUtil.NextLineHead(text, i) );
			for( ; i<71; i++ )
				Assert.AreEqual( -1, TextUtil.NextLineHead(text, i) );
			Assert.AreEqual( -1, TextUtil.NextLineHead(text, i) );
		}

		[TestMethod]
		public void PrevLineHead()
		{
			TextBuffer text = new TextBuffer( 1, 32 );

			text.Insert( 0, TestData.ToCharArray() );

			int i=71;
			for( ; 53<=i; i-- )
				Assert.AreEqual( 53, TextUtil.PrevLineHead(text, i) );
			for( ; 52<=i; i-- )
				Assert.AreEqual( 52, TextUtil.PrevLineHead(text, i) );
			for( ; 38<=i; i-- )
				Assert.AreEqual( 38, TextUtil.PrevLineHead(text, i) );
			for( ; 37<=i; i-- )
				Assert.AreEqual( 37, TextUtil.PrevLineHead(text, i) );
			for( ; 33<=i; i-- )
				Assert.AreEqual( 33, TextUtil.PrevLineHead(text, i) );
			for( ; 32<=i; i-- )
				Assert.AreEqual( 32, TextUtil.PrevLineHead(text, i) );
			for( ; 0<=i; i-- )
				Assert.AreEqual( 0,  TextUtil.PrevLineHead(text, i) );
		}

		[TestMethod]
		public void GetLineLengthByCharIndex()
		{
			TextBuffer text = new TextBuffer( 1, 32 );
			int i = 0;

			text.Insert( 0, TestData.ToCharArray() );

			for( ; i<32; i++ )
				Assert.AreEqual( 32, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<33; i++ )
				Assert.AreEqual(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<37; i++ )
				Assert.AreEqual(  4, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<38; i++ )
				Assert.AreEqual(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<52; i++ )
				Assert.AreEqual( 14, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<53; i++ )
				Assert.AreEqual(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<71; i++ )
				Assert.AreEqual( 17, TextUtil.GetLineLengthByCharIndex(text, i) );
			Assert.AreEqual( 17, TextUtil.GetLineLengthByCharIndex(text, i) ); // EOF
		}

		[TestMethod]
		public void GetLineRange()
		{
			TextSegment range;
			TextBuffer text;
			SplitArray<int> lhi;
			SplitArray<LineDirtyState> lds;

			MakeTestData( out text, out lhi, out lds );

			range = TextUtil.GetLineRange( text, lhi, 0, false );
			Assert.AreEqual( 0, range.Begin );
			Assert.AreEqual( 30, range.End );
			range = TextUtil.GetLineRange( text, lhi, 0, true );
			Assert.AreEqual( 0, range.Begin );
			Assert.AreEqual( 32, range.End );

			range = TextUtil.GetLineRange( text, lhi, 1, false );
			Assert.AreEqual( 32, range.Begin );
			Assert.AreEqual( 32, range.End );
			range = TextUtil.GetLineRange( text, lhi, 1, true );
			Assert.AreEqual( 32, range.Begin );
			Assert.AreEqual( 33, range.End );

			range = TextUtil.GetLineRange( text, lhi, 2, false );
			Assert.AreEqual( 33, range.Begin );
			Assert.AreEqual( 36, range.End );
			range = TextUtil.GetLineRange( text, lhi, 2, true );
			Assert.AreEqual( 33, range.Begin );
			Assert.AreEqual( 37, range.End );

			range = TextUtil.GetLineRange( text, lhi, 3, false );
			Assert.AreEqual( 37, range.Begin );
			Assert.AreEqual( 37, range.End );
			range = TextUtil.GetLineRange( text, lhi, 3, true );
			Assert.AreEqual( 37, range.Begin );
			Assert.AreEqual( 38, range.End );

			range = TextUtil.GetLineRange( text, lhi, 4, false );
			Assert.AreEqual( 38, range.Begin );
			Assert.AreEqual( 51, range.End );
			range = TextUtil.GetLineRange( text, lhi, 4, true );
			Assert.AreEqual( 38, range.Begin );
			Assert.AreEqual( 52, range.End );

			range = TextUtil.GetLineRange( text, lhi, 5, false );
			Assert.AreEqual( 52, range.Begin );
			Assert.AreEqual( 52, range.End );
			range = TextUtil.GetLineRange( text, lhi, 5, true );
			Assert.AreEqual( 52, range.Begin );
			Assert.AreEqual( 53, range.End );

			range = TextUtil.GetLineRange( text, lhi, 6, false );
			Assert.AreEqual( 53, range.Begin );
			Assert.AreEqual( 71, range.End );
			range = TextUtil.GetLineRange( text, lhi, 6, true );
			Assert.AreEqual( 53, range.Begin );
			Assert.AreEqual( 71, range.End );
		}

		[TestMethod]
		public void GetLineIndexFromCharIndex()
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
				Assert.AreEqual( 0, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<33; i++ )
				Assert.AreEqual( 1, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<37; i++ )
				Assert.AreEqual( 2, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<38; i++ )
				Assert.AreEqual( 3, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<52; i++ )
				Assert.AreEqual( 4, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<53; i++ )
				Assert.AreEqual( 5, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			Assert.AreEqual( 6, TextUtil.GetLineIndexFromCharIndex(lhi, 54) );
		}

		[TestMethod]
		public void GetLineColumnIndexFromCharIndex()
		{
			TextBuffer text;
			SplitArray<int> lhi;
			int l, c;

			MakeTestData( out text, out lhi );

			TextUtil.GetLineColumnIndexFromCharIndex( text, lhi, 0, out l, out c );
			Assert.AreEqual( 0, l );
			Assert.AreEqual( 0, c );
			TextUtil.GetLineColumnIndexFromCharIndex( text, lhi, 2, out l, out c );
			Assert.AreEqual( 0, l );
			Assert.AreEqual( 2, c );
			TextUtil.GetLineColumnIndexFromCharIndex( text, lhi, 40, out l, out c );
			Assert.AreEqual( 4, l );
			Assert.AreEqual( 2, c );
			TextUtil.GetLineColumnIndexFromCharIndex( text, lhi, 71, out l, out c ); // 71 --> EOF
			Assert.AreEqual( 6, l );
			Assert.AreEqual( 18, c );
			MyAssert.Throws<AssertException>( delegate{
				TextUtil.GetLineColumnIndexFromCharIndex(text, lhi, 72, out l, out c);
			} );
		}

		[TestMethod]
		public void LineHeadIndexFromCharIndex()
		{
			TextBuffer text;
			SplitArray<int> lhi;

			MakeTestData( out text, out lhi );

			int i=0;
			for( ; i<32; i++ )
				Assert.AreEqual(  0, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<33; i++ )
				Assert.AreEqual( 32, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<37; i++ )
				Assert.AreEqual( 33, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<38; i++ )
				Assert.AreEqual( 37, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<52; i++ )
				Assert.AreEqual( 38, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<53; i++ )
				Assert.AreEqual( 52, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<=71; i++ )
				Assert.AreEqual( 53, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			MyAssert.Throws<AssertException>( delegate{
				TextUtil.GetLineHeadIndexFromCharIndex( text, lhi, i );
			} );
		}

		[TestMethod]
		public void LHI_Insert()
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
			Assert.AreEqual( "0 19 20", lhi.ToString() );
			Assert.AreEqual( "DDD", MakeLdsText(lds) );

			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			TextUtil.LHI_Insert( lhi, lds, text, TestData2, 6 );
			text.Insert( 6, TestData2.ToCharArray() );
			Assert.AreEqual( "0 32 33 36 37 51 52", lhi.ToString() );
			Assert.AreEqual( "DDDDDCC", MakeLdsText(lds) );

			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			TextUtil.LHI_Insert( lhi, lds, text, "u", 34 );
			text.Insert( 34, "u".ToCharArray() );
			Assert.AreEqual( "0 32 33 37 38 52 53", lhi.ToString() );
			Assert.AreEqual( "CCDCCCC", MakeLdsText(lds) );

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
				Assert.AreEqual( "0 5 7", lhi.ToString() );
				Assert.AreEqual( "DDC", MakeLdsText(lds) );
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
				Assert.AreEqual( "0 4 7", lhi.ToString() );
				Assert.AreEqual( "DDC", MakeLdsText(lds) );
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
				Assert.AreEqual( "0 5 7", lhi.ToString() );
				Assert.AreEqual( "DDC", MakeLdsText(lds) );
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
				Assert.AreEqual( "0 5", lhi.ToString() );
				Assert.AreEqual( "DD", MakeLdsText(lds) );
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
				Assert.AreEqual( "0 6", lhi.ToString() );
				Assert.AreEqual( "DC", MakeLdsText(lds) );
			}
		}

		[TestMethod]
		public void LHI_Delete()
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
			Assert.AreEqual( "0 32 33 37 38 52 53", lhi.ToString() );
			Assert.AreEqual( "DDDDDDD", MakeLdsText(lds) );
			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			
			//--- delete range in line ---
			// valid range
			TextUtil.LHI_Delete( lhi, lds, text, 2, 5 );
			text.RemoveRange( 2, 5 );
			Assert.AreEqual( "0 29 30 34 35 49 50", lhi.ToString() );
			Assert.AreEqual( "DCCCCCC", MakeLdsText(lds) );

			// invalid range (before begin to middle)
			MyAssert.Throws<AssertException>( delegate {
				TextUtil.LHI_Delete( lhi, lds, text, -1, 5 );
			} );

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
			Assert.AreEqual( "0 32 36 37 51 52", lhi.ToString() );
			Assert.AreEqual( "CDCCCC", MakeLdsText(lds) );

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
			Assert.AreEqual( "0 36 37", lhi.ToString() );
			Assert.AreEqual( "DCC", MakeLdsText(lds) );

			// delete all
			//----
			// [EOF] (head:0, len:0)
			//----
			for( int i=0; i<lds.Count; i++ )
				lds[i] = LineDirtyState.Clean;
			TextUtil.LHI_Delete( lhi, lds, text, 0, 55 );
			text.RemoveRange( 0, 55 );
			Assert.AreEqual( "0", lhi.ToString() );
			Assert.AreEqual( "D", MakeLdsText(lds) );

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
				Assert.AreEqual( "0 5", lhi.ToString() );
				Assert.AreEqual( "DC", MakeLdsText(lds) );
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
				Assert.AreEqual( "0 4", lhi.ToString() );
				Assert.AreEqual( "DD", MakeLdsText(lds) );
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
				Assert.AreEqual( "0 3", lhi.ToString() );
				Assert.AreEqual( "DC", MakeLdsText(lds) );
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
				Assert.AreEqual( "0 2", lhi.ToString() );
				Assert.AreEqual( "DC", MakeLdsText(lds) );
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
				Assert.AreEqual( "0 2", lhi.ToString() );
				Assert.AreEqual( "CD", MakeLdsText(lds) );
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
				Assert.AreEqual( "0 1", lhi.ToString() );
				Assert.AreEqual( "DD", MakeLdsText(lds) );
			}
		}

		#region Utilities
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
		#endregion
	}
}
