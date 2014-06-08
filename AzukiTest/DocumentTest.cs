using System;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Sgry.Azuki.Test
{
	[TestFixture]
	public class DocumentTest
	{
		[Test]
		public void GetLineColumnIndexFromCharIndex()
		{
			// keep it as simple as possible\r\n
			// but\n
			// not simpler.\r\n
			Document doc = new Document();
			int line, column;
			int i = 0;

			doc.Text = "keep it as simple as possible\r\nbut\nnot simpler.\r\n";

			i = 0;
			for( ; i<31; i++ )
			{
				doc.GetLineColumnIndexFromCharIndex( i, out line, out column );
				Assert.AreEqual( 0, line );
				Assert.AreEqual( i, column );
			}
			for( ; i<35; i++ )
			{
				doc.GetLineColumnIndexFromCharIndex( i, out line, out column );
				Assert.AreEqual( 1, line );
				Assert.AreEqual( i-31, column );
			}
			for( ; i<49; i++ )
			{
				doc.GetLineColumnIndexFromCharIndex( i, out line, out column );
				Assert.AreEqual( 2, line );
				Assert.AreEqual( i-35, column );
			}
			doc.GetLineColumnIndexFromCharIndex( 49, out line, out column );
			Assert.AreEqual( 3, line );
			Assert.AreEqual( i-49, column );

			// out of range
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetLineColumnIndexFromCharIndex(50, out line, out column);
			} );
		}

		[Test]
		public void GetLineIndexFromCharIndex()
		{
			// keep it as simple as possible\r\n
			// but\n
			// not simpler.\r\n
			Document doc = new Document();
			int i = 0;

			doc.Text = "keep it as simple as possible\r\nbut\nnot simpler.\r\n";

			i = 0;
			for( ; i<31; i++ )
			{
				Assert.AreEqual( 0, doc.GetLineIndexFromCharIndex(i) );
			}
			for( ; i<35; i++ )
			{
				Assert.AreEqual( 1, doc.GetLineIndexFromCharIndex(i) );
			}
			for( ; i<49; i++ )
			{
				Assert.AreEqual( 2, doc.GetLineIndexFromCharIndex(i) );
			}
			Assert.AreEqual( 3, doc.GetLineIndexFromCharIndex(49) );

			// out of range
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetLineIndexFromCharIndex( 50 );
			} );
		}

		[Test]
		public void GetText()
		{
			// keep it as simple as possible\r\n
			// but\n
			// not simpler.\r\n
			Document doc = new Document();

			doc.Text = "keep it as simple as possible\r\nbut\nnot simpler.\r\n";
			Assert.AreEqual( "keep it as simple as possible\r\nbut\nnot simpler.\r\n", doc.Text );
			Assert.AreEqual( "keep it as simple as possible", doc.GetLineContent(0) );
			Assert.AreEqual( "keep it as simple as possible\r\n", doc.GetLineContentWithEolCode(0) );
			Assert.AreEqual( "but", doc.GetLineContent(1) );
			Assert.AreEqual( "but\n", doc.GetLineContentWithEolCode(1) );
			Assert.AreEqual( "not simpler.", doc.GetLineContent(2) );
			Assert.AreEqual( "not simpler.\r\n", doc.GetLineContentWithEolCode(2) );
			Assert.AreEqual( "", doc.GetLineContent(3) );
			Assert.AreEqual( "", doc.GetLineContentWithEolCode(3) );
		}

		[Test]
		public void GetTextInRange()
		{
			// keep it\r
			// as simple as possible\r\n
			// but\n
			// not simpler.
			Document doc = new Document();
			doc.Text = "keep it\ras simple as possible\r\nbut\nnot simpler.";

			// char-index type
			{
				// invalid range (before begin to middle)
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.GetTextInRange( -1, 4 );
				} );

				// valid range (begin to middle)
				Assert.AreEqual( "ke", doc.GetTextInRange(0, 2) );

				// valid range (middle to middle)
				Assert.AreEqual( "ep it", doc.GetTextInRange(2, 7) );

				// valid range (middle to end)
				Assert.AreEqual( "simpler.", doc.GetTextInRange(39, 47) );

				// invalid range (middle to after end)
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.GetTextInRange( 39, 48 );
				} );

				// invalid range (minus range)
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.GetTextInRange( 10, 9 );
				} );
			}

			// line/column index type
			{
				// invalid range (before begin to middle)
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.GetTextInRange( -1, -1, 1, 1 );
				} );
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.GetTextInRange( -1, 0, 1, 1 );
				} );
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.GetTextInRange( 1, -1, 1, 1 );
				} );
				
				// valid range (empty range)
				Assert.AreEqual( "", doc.GetTextInRange(0, 0, 0, 0) );
				Assert.AreEqual( "", doc.GetTextInRange(1, 1, 1, 1) );

				// valid range (begin to middle)
				Assert.AreEqual( "ke", doc.GetTextInRange(0, 0, 0, 2) );
				Assert.AreEqual( "ee", doc.GetTextInRange(0, 1, 0, 3) );
				Assert.AreEqual( "as", doc.GetTextInRange(1, 0, 1, 2) );
				
				// valid range (middle to middle)
				Assert.AreEqual( "s simple as possible\r\nbut\nn", doc.GetTextInRange(1, 1, 3, 1) );
				
				// valid range (middle to end)
				Assert.AreEqual( "t\nnot simpler.", doc.GetTextInRange(2, 2, 3, 12) );
				
				// invalid range (middle to after end)
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.GetTextInRange( 2, 2, 3, 13 );
				} );
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.GetTextInRange( 2, 2, 4, 0 );
				} );
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.GetTextInRange( 2, 2, 4, 13 );
				} );

				// invalid range (minus range)
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.GetTextInRange( 1, 1, 1, 0 );
				} );
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.GetTextInRange( 1, 1, 0, 0 );
				} );
			}
		}

		[Test]
		public void GetTextInRange_SurrogatePair()
		{
			Document doc = new Document();
			string str;
			int begin, end;
			doc.Text = "\xd85a\xdd51";

			// hi-surrogate to hi-surrogate
			str = doc.GetTextInRange( 0, 0 );
			Assert.AreEqual( "", str );
			doc.SetSelection( 0, 0 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( 0, begin );
			Assert.AreEqual( 0, end );

			// hi-surrogate to lo-surrogate (dividing pair)
			str = doc.GetTextInRange( 0, 1 );
			Assert.AreEqual( "\xd85a\xdd51", str );
			doc.SetSelection( 0, 1 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( 0, begin );
			Assert.AreEqual( 2, end );
			doc.SetSelection( 1, 0 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( 0, begin );
			Assert.AreEqual( 2, end );

			// lo-surrogate to lo-surrogate
			str = doc.GetTextInRange( 1, 1 );
			Assert.AreEqual( "", str );
			doc.SetSelection( 1, 1 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( 0, begin );
			Assert.AreEqual( 0, end );

			// lo-surrogate to after lo-surrogate (dividing pair)
			str = doc.GetTextInRange( 1, 2 );
			Assert.AreEqual( "\xd85a\xdd51", str );
			doc.SetSelection( 1, 2 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( 0, begin );
			Assert.AreEqual( 2, end );
			doc.SetSelection( 2, 1 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( 0, begin );
			Assert.AreEqual( 2, end );

			// after lo-surrogate to after lo-surrogate
			str = doc.GetTextInRange( 2, 2 );
			Assert.AreEqual( "", str );
			doc.SetSelection( 2, 2 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( 2, begin );
			Assert.AreEqual( 2, end );
		}

		[Test]
		public void Replace()
		{
			Document doc = new Document();
			doc.Text = "keep it as simple as possible\r\nbut\nnot simpler.";
			
			// empty range (1)
			doc.Replace( "n\r", 8, 8 );
			Assert.AreEqual( "keep it n\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
			Assert.AreEqual( 0, doc.AnchorIndex );
			Assert.AreEqual( 0, doc.CaretIndex );

			// empty range (2)
			doc.SetSelection( 9, 9 );
			doc.Replace( "ot" );
			Assert.AreEqual( "keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
			Assert.AreEqual( 11, doc.AnchorIndex );
			Assert.AreEqual( 11, doc.CaretIndex );

			// invalid range (before begin to middle)
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.Replace( "FOO", -1, 10 );
			} );

			// valid range (begin to middle)
			{
				// replace to same length (1)
				doc.SetSelection( 0, 4 );
				doc.Replace( "KEP" );
				Assert.AreEqual( "KEP it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 3, doc.AnchorIndex );
				Assert.AreEqual( 3, doc.CaretIndex );

				// replace to same length (2)
				doc.Replace( "Keep", 0, 3 );
				Assert.AreEqual( "Keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 4, doc.AnchorIndex );
				Assert.AreEqual( 4, doc.CaretIndex );
				
				// replace to longer (1)
				doc.SetSelection( 0, 3 );
				doc.Replace( "KEEP!" );
				Assert.AreEqual( "KEEP!p it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 5, doc.AnchorIndex );
				Assert.AreEqual( 5, doc.CaretIndex );
				
				// replace to longer (2)
				doc.Replace( "Keeeeep", 0, 6 );
				Assert.AreEqual( "Keeeeep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 7, doc.AnchorIndex );
				Assert.AreEqual( 7, doc.CaretIndex );
				
				// replace to shorter (1)
				doc.SetSelection( 0, 7 );
				doc.Replace( "KEEEP" );
				Assert.AreEqual( "KEEEP it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 5, doc.AnchorIndex );
				Assert.AreEqual( 5, doc.CaretIndex );

				// replace to shorter (2)
				doc.Replace( "Keep", 0, 5 );
				Assert.AreEqual( "Keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 4, doc.AnchorIndex );
				Assert.AreEqual( 4, doc.CaretIndex );
			}
			
			// valid range (middle to middle)
			{
				// replace to same length (1)
				doc.Replace( "ZIMPLE", 15, 21 );
				Assert.AreEqual( "Keep it not\ras ZIMPLE as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 4, doc.AnchorIndex );
				Assert.AreEqual( 4, doc.CaretIndex );

				// replace to same length (2)
				doc.SetSelection( 15, 21 );
				doc.Replace( "SIMPLE" );
				Assert.AreEqual( "Keep it not\ras SIMPLE as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 21, doc.AnchorIndex );
				Assert.AreEqual( 21, doc.CaretIndex );

				// replace to longer (1)
				doc.SetSelection( 14, 15 );
				doc.Replace( "COMPLEX", 15, 21 );
				Assert.AreEqual( "Keep it not\ras COMPLEX as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 14, doc.AnchorIndex );
				Assert.AreEqual( 22, doc.CaretIndex );
				
				// replace to longer (2)
				doc.SetSelection( 19, 22 );
				doc.Replace( "LEX!" );
				Assert.AreEqual( "Keep it not\ras COMPLEX! as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 23, doc.AnchorIndex );
				Assert.AreEqual( 23, doc.CaretIndex );

				// replace to shorter (1)
				doc.Replace( "simple!", 15, 23 );
				Assert.AreEqual( "Keep it not\ras simple! as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 22, doc.AnchorIndex );
				Assert.AreEqual( 22, doc.CaretIndex );
				
				// replace to shorter (2)
				doc.SetSelection( 15, 22 );
				doc.Replace( "simple" );
				Assert.AreEqual( "Keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 21, doc.AnchorIndex );
				Assert.AreEqual( 21, doc.CaretIndex );
			}
			
			// valid range (middle to end)
			{
				// replace to same length (1)
				doc.Replace( "?", 50, 51 );
				Assert.AreEqual( "Keep it not\ras simple as possible\r\nbut\nnot simpler?", doc.Text );
				Assert.AreEqual( 21, doc.AnchorIndex );
				Assert.AreEqual( 21, doc.CaretIndex );
				
				// replace to same length (2)
				doc.SetSelection( 50, 51 );
				doc.Replace( "!" );
				Assert.AreEqual( "Keep it not\ras simple as possible\r\nbut\nnot simpler!", doc.Text );
				Assert.AreEqual( 51, doc.AnchorIndex );
				Assert.AreEqual( 51, doc.CaretIndex );

				// replace to longer (1)
				doc.Replace( "??", 50, 51 );
				Assert.AreEqual( "Keep it not\ras simple as possible\r\nbut\nnot simpler??", doc.Text );
				Assert.AreEqual( 52, doc.AnchorIndex );
				Assert.AreEqual( 52, doc.CaretIndex );

				// replace to longer (2)
				doc.Replace( "!!!", 50, 52 );
				Assert.AreEqual( "Keep it not\ras simple as possible\r\nbut\nnot simpler!!!", doc.Text );
				Assert.AreEqual( 53, doc.AnchorIndex );
				Assert.AreEqual( 53, doc.CaretIndex );
				
				// replace to shorter (1)
				doc.Replace( "..", 50, 53 );
				Assert.AreEqual( "Keep it not\ras simple as possible\r\nbut\nnot simpler..", doc.Text );
				Assert.AreEqual( 52, doc.AnchorIndex );
				Assert.AreEqual( 52, doc.CaretIndex );
				
				// replace to shorter (2)
				doc.Replace( ".", 50, 52 );
				Assert.AreEqual( "Keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				Assert.AreEqual( 51, doc.AnchorIndex );
				Assert.AreEqual( 51, doc.CaretIndex );
			}
			
			// invalid range (middle to after end)
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.Replace( "PIYO", 51, 53 );
			} );
		}

		[Test]
		public void Replace_SelectionRange()
		{
			Document doc = new Document();

			{
				// replace before head to before head
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "1", 0, 0 ); // 1ab[cd)ef
				Assert.AreEqual( 3, doc.AnchorIndex );
				Assert.AreEqual( 5, doc.CaretIndex );

				// replace before head to head
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "2", 0, 2 ); // 2[cd)ef
				Assert.AreEqual( 1, doc.AnchorIndex );
				Assert.AreEqual( 3, doc.CaretIndex );

				// replace before head to middle
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "G", 0, 3 ); // G[d)ef
				Assert.AreEqual( 1, doc.AnchorIndex );
				Assert.AreEqual( 2, doc.CaretIndex );

				// replace before head to end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "hoge", 0, 4 ); // hoge[)ef
				Assert.AreEqual( 4, doc.AnchorIndex );
				Assert.AreEqual( 4, doc.CaretIndex );

				// replace before head to after end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "Z", 0, 5 ); // Z[)f
				Assert.AreEqual( 1, doc.AnchorIndex );
				Assert.AreEqual( 1, doc.CaretIndex );
			}

			{
				// replace head to head
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "2", 2, 2 ); // ab2[cd)ef
				Assert.AreEqual( 3, doc.AnchorIndex );
				Assert.AreEqual( 5, doc.CaretIndex );

				// replace head to middle
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "G", 2, 3 ); // abG[d)ef
				Assert.AreEqual( 3, doc.AnchorIndex );
				Assert.AreEqual( 4, doc.CaretIndex );

				// replace head to end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "hoge", 2, 4 ); // abhoge[)ef
				Assert.AreEqual( 6, doc.AnchorIndex );
				Assert.AreEqual( 6, doc.CaretIndex );

				// replace head to after end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "Z", 2, 5 ); // abZ[)f
				Assert.AreEqual( 3, doc.AnchorIndex );
				Assert.AreEqual( 3, doc.CaretIndex );
			}

			{
				// replace middle to middle
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "!", 3, 3 ); // ab[c!d)ef
				Assert.AreEqual( 2, doc.AnchorIndex );
				Assert.AreEqual( 5, doc.CaretIndex );

				// replace middle to end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "HOGE", 3, 4 ); // ab[cHOGE)ef
				DebugUtl.Assert( doc.AnchorIndex == 2, "(anchor, caret) = ("+doc.AnchorIndex+", "+doc.CaretIndex+")" );
				DebugUtl.Assert( doc.CaretIndex == 7, "(anchor, caret) = ("+doc.AnchorIndex+", "+doc.CaretIndex+")" );

				// replace middle to after end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "Z", 3, 5 ); // ab[cZ)f
				Assert.AreEqual( 2, doc.AnchorIndex );
				Assert.AreEqual( 4, doc.CaretIndex );
			}

			{
				// replace end to end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "?", 4, 4 ); // ab[cd?)ef
				Assert.AreEqual( 2, doc.AnchorIndex );
				Assert.AreEqual( 5, doc.CaretIndex );

				// replace end to after end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "Z", 4, 5 ); // ab[cdZ)f
				Assert.AreEqual( 2, doc.AnchorIndex );
				Assert.AreEqual( 5, doc.CaretIndex );
			}

			{
				// replace after end to after end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "Z", 5, 5 ); // ab[cd)eZf
				Assert.AreEqual( 2, doc.AnchorIndex );
				Assert.AreEqual( 4, doc.CaretIndex );
			}
		}

		[Test]
		public void GetLineLength()
		{
			// 0 keep it\r
			// 1 \r
			// 2 as simple as possible\r\n
			// 3 \n
			// 4 but\n
			// 5 \r\n
			// 6 not simpler.
			Document doc = new Document();
			doc.Text = "keep it\r\ras simple as possible\r\n\nbut\n\r\nnot simpler.";

			Assert.AreEqual( 7, doc.GetLineLength(0) );
			Assert.AreEqual( 0, doc.GetLineLength(1) );
			Assert.AreEqual( 21, doc.GetLineLength(2) );
			Assert.AreEqual( 0, doc.GetLineLength(3) );
			Assert.AreEqual( 3, doc.GetLineLength(4) );
			Assert.AreEqual( 0, doc.GetLineLength(5) );
			Assert.AreEqual( 12, doc.GetLineLength(6) );
			Assert.Throws<ArgumentOutOfRangeException>( delegate {
				doc.GetLineLength( 7 );
			} );
		}

		[Test]
		public void Selection()
		{
			Document doc = new Document();
			int begin, end;

			doc.Text = "臼と似た形をした文字「\xd85a\xdd51」は、UCS 文字空間の第２面に位置する";

			// before head to head
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.SetSelection( -1, 0 );
			} );
			
			// head to head
			doc.SetSelection( 0, 0 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( begin, 0 );
			Assert.AreEqual( end, 0 );
			
			// head to before hi-surrogate
			doc.SetSelection( 0, 4 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( begin, 0 );
			Assert.AreEqual( end, 4 );
			
			// before hi-surrogate to lo-surrogate
			doc.SetSelection( 4, 11 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( begin, 4 );
			Assert.AreEqual( end, 11 );
			
			// hi-surrogate to lo-surrogate
			doc.SetSelection( 11, 12 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( begin, 11 );
			Assert.AreEqual( end, 13 ); // surely expanded?

			// hi-surrogate to after lo-surrogate
			doc.SetSelection( 11, 13 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( begin, 11 );
			Assert.AreEqual( end, 13 );

			// middle of the surrogate pair
			// ('moving caret between the pair' must be treated as 'moving caret to the *char*')
			doc.SetSelection( 12, 12 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( begin, 11 );
			Assert.AreEqual( end, 11 );

			// lo-surrogate to after lo-surrogate
			doc.SetSelection( 12, 13 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( begin, 11 );
			Assert.AreEqual( end, 13 );

			// after lo-surrogate to end
			doc.SetSelection( 13, 33 );
			doc.GetSelection( out begin, out end );
			Assert.AreEqual( begin, 13 );
			Assert.AreEqual( end, 33 );

			// end to after end
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.SetSelection( 33, 36 );
			} );
		}

		[Test]
		public void FindNext()
		{
			Document doc = new Document();
			doc.Replace( "aababcabcd" );

			// black box test (interface test)
			{
				// null target
				Assert.Throws<ArgumentNullException>( delegate{
					doc.FindNext( (string)null, 0 );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( "a", -1 );
				} );

				// end index at out of range
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( "a", 0, doc.Length+1, true );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( "a", 1, 0, true );
				} );

				// empty range
				Assert.AreEqual( null, doc.FindNext("a", 0, 0, true) );

				// find in valid range
				Assert.AreEqual( 0, doc.FindNext("a", 0, 1, true).Begin );
				Assert.AreEqual( 1, doc.FindNext("ab", 0).Begin );
				Assert.AreEqual( 3, doc.FindNext("abc", 0).Begin );
				Assert.AreEqual( 6, doc.FindNext("abcd", 0).Begin );
				Assert.AreEqual( null, doc.FindNext("abcde", 0) );

				// empty pattern (returns begin index)
				Assert.AreEqual( 1, doc.FindNext("", 1).Begin );

				// comp. options
				Assert.AreEqual( null, doc.FindNext("aBcD", 0, doc.Length, true) );
				Assert.AreEqual(  6, doc.FindNext("aBcD", 0, doc.Length, false).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( doc, 4 );
				Assert.AreEqual( 6, doc.FindNext("ab", 5, doc.Length, true).Begin );

				// gap == begin
				MoveGap( doc, 4 );
				Assert.AreEqual( 6, doc.FindNext("ab", 4, doc.Length, true).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 2, doc.FindNext("ba", 2, doc.Length, true).Begin );

					// word crossing the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 3, doc.FindNext("ab", 2, doc.Length, true).Begin );

					// word after the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 5, doc.FindNext("cab", 2, doc.Length, true).Begin );
				}

				// gap == end
				{
					MoveGap( doc, 4 );
					Assert.AreEqual( 1, doc.FindNext("ab", 0, 4, true).Begin );

					// word at the end
					MoveGap( doc, 4 );
					Assert.AreEqual( 2, doc.FindNext("ba", 0, 4, true).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 4 );
					Assert.AreEqual( null, doc.FindNext("abc", 0, 4, true) );
				}

				// end <= gap
				MoveGap( doc, 4 );
				Assert.AreEqual( 1, doc.FindNext("ab", 0, 4, true).Begin );
			}
		}

		[Test]
		public void FindPrev()
		{
			Document doc = new Document();
			doc.Replace( "abcdabcaba" );

			// black box test (interface test)
			{
				// null target
				Assert.Throws<ArgumentNullException>( delegate{
					doc.FindPrev( (string)null, 0, 10, true );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindPrev( "a", -1, 10, true );
				} );

				// end index at out of range
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindPrev( "a", 0, doc.Length+1, true );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindPrev( "a", 1, 0, true );
				} );

				// empty range
				Assert.AreEqual( null, doc.FindPrev("a", 0, 0, true) );

				// find in valid range
				Assert.AreEqual( 9, doc.FindPrev(   "a", 0, 10, true).Begin );
				Assert.AreEqual( 7, doc.FindPrev(  "ab", 0, 10, true).Begin );
				Assert.AreEqual( 4, doc.FindPrev( "abc", 0, 10, true).Begin );
				Assert.AreEqual( 0, doc.FindPrev("abcd", 0, 10, true).Begin );
				Assert.AreEqual( null, doc.FindPrev("abcde", 0, 10, true) );

				// empty pattern (returns end index)
				Assert.AreEqual( 10, doc.FindPrev("", 0, 10, true).Begin );

				// comp. options
				Assert.AreEqual( null, doc.FindPrev("aBcD", 0, 10, true) );
				Assert.AreEqual(  0, doc.FindPrev("aBcD", 0, 10, false).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: abcda......bcaba)

				// gap < begin
				MoveGap( doc, 5 );
				Assert.AreEqual( 7, doc.FindPrev("ab", 7, 10, true).Begin );

				// gap == begin
				{
					MoveGap( doc, 5 );
					Assert.AreEqual( 7, doc.FindPrev("ab", 5, 10, true).Begin );

					// word at the begin
					MoveGap( doc, 5 );
					Assert.AreEqual( 5, doc.FindPrev("bc", 5, 10, true).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 5 );
					Assert.AreEqual( null, doc.FindPrev("abca", 5, 10, true) );
				}

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 5 );
					Assert.AreEqual( 3, doc.FindPrev("da", 0, 10, true).Begin );

					// word crossing the gap
					MoveGap( doc, 5 );
					Assert.AreEqual( 4, doc.FindPrev("abc", 0, 10, true).Begin );

					// word after the gap
					MoveGap( doc, 5 );
					Assert.AreEqual( 5, doc.FindPrev("bca", 0, 10, true).Begin );
				}

				// gap == end
				MoveGap( doc, 5 );
				Assert.AreEqual( 0, doc.FindPrev("ab", 0, 5, true).Begin );

				// end <= gap
				MoveGap( doc, 5 );
				Assert.AreEqual( 0, doc.FindPrev("ab", 0, 4, true).Begin );
			}
		}

		[Test]
		public void FindNextR()
		{
			Document doc = new Document();
			SearchResult result;
			doc.Replace( "aababcabcd" );

			// black box test
			{
				// null argument
				Assert.Throws<ArgumentNullException>( delegate{
					doc.FindNext( (Regex)null, 1, 2 );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( new Regex("a[^b]+"), -1, 2 );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( new Regex("a[^b]+"), 2, 1 );
				} );

				// empty range
				result = doc.FindNext( new Regex("a[^b]+"), 0, 0 );
				Assert.AreEqual( null, result );

				// range exceeding text length
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( new Regex("a[^b]+"), 1, 9999 );
				} );

				// invalid Regex option
				Assert.Throws<ArgumentException>( delegate{
					doc.FindNext( new Regex("a[^b]+", RegexOptions.RightToLeft), 1, 4 );
				} );

				// pattern ord at begin
				result = doc.FindNext( new Regex("a[^b]+"), 0, 2 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 2, result.End );

				// pattern in the range
				result = doc.FindNext( new Regex("a[^a]+"), 0, 3 );
				Assert.AreEqual( 1, result.Begin );
				Assert.AreEqual( 3, result.End );

				// pattern which ends at end
				result = doc.FindNext( new Regex("[ab]+"), 0, 5 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 5, result.End );

				// pattern... well, pretty hard to describe in English for me...
				result = doc.FindNext( new Regex("[abc]+"), 0, 5 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 5, result.End );
				result = doc.FindNext( new Regex("[abc]+"), 0, 10 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 9, result.End );

				// empty pattern
				result = doc.FindNext( new Regex(""), 0, 10 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 0, result.End );

				// comp. options
				result = doc.FindNext( new Regex("aBcD"), 0, doc.Length );
				Assert.AreEqual( null, result );
				result = doc.FindNext( new Regex("aBcD", RegexOptions.IgnoreCase), 0, doc.Length );
				Assert.AreEqual(  6, result.Begin );
				Assert.AreEqual( 10, result.End);
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( doc, 4 );
				Assert.AreEqual( 6, doc.FindNext(new Regex("ab"), 5, doc.Length).Begin );

				// gap == begin
				MoveGap( doc, 4 );
				Assert.AreEqual( 6, doc.FindNext(new Regex("ab"), 4, doc.Length).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 2, doc.FindNext(new Regex("ba"), 2, doc.Length).Begin );

					// word crossing the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 3, doc.FindNext(new Regex("ab"), 2, doc.Length).Begin );

					// word after the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 5, doc.FindNext(new Regex("cab"), 2, doc.Length).Begin );
				}

				// gap == end
				{
					MoveGap( doc, 4 );
					Assert.AreEqual( 1, doc.FindNext(new Regex("ab"), 0, 4).Begin );

					// word at the end
					MoveGap( doc, 4 );
					Assert.AreEqual( 2, doc.FindNext(new Regex("ba"), 0, 4).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 4 );
					Assert.AreEqual( null, doc.FindNext(new Regex("abc"), 0, 4) );
				}

				// end <= gap
				MoveGap( doc, 4 );
				Assert.AreEqual( 1, doc.FindNext(new Regex("ab"), 0, 4).Begin );
			}
		}

		[Test]
		public void FindPrevR()
		{
			Document doc = new Document();
			doc.Replace( "abcdabcaba" );

			// black box test (interface test)
			{
				// null target
				Assert.Throws<ArgumentNullException>( delegate{
					doc.FindPrev( (Regex)null, 0, 10 );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindPrev( new Regex("a", RegexOptions.RightToLeft), -1, 10 );
				} );

				// invalid regex option
				Assert.Throws<ArgumentException>( delegate{
					doc.FindPrev( new Regex("a", RegexOptions.None), 0, doc.Length );
				} );

				// end index at out of range
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindPrev( new Regex("a", RegexOptions.RightToLeft), 0, doc.Length+1 );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindPrev( new Regex("a", RegexOptions.RightToLeft), 1, 0 );
				} );

				// empty range
				Assert.AreEqual( null, doc.FindPrev(new Regex("a", RegexOptions.RightToLeft), 0, 0) );

				// find in valid range
				Assert.AreEqual( 9, doc.FindPrev(new Regex(   "a", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( 7, doc.FindPrev(new Regex(  "ab", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( 4, doc.FindPrev(new Regex( "abc", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( 0, doc.FindPrev(new Regex("abcd", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( null, doc.FindPrev(new Regex("abcde", RegexOptions.RightToLeft), 0, 10) );

				// empty pattern (returns end index)
				Assert.AreEqual( 10, doc.FindPrev(new Regex("", RegexOptions.RightToLeft), 0, 10).Begin );

				// comp. options
				Assert.AreEqual( null, doc.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft), 0, 10) );
				Assert.AreEqual(  0, doc.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft|RegexOptions.IgnoreCase), 0, 10).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: abcda......bcaba)

				// gap < begin
				MoveGap( doc, 5 );
				Assert.AreEqual( 7, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 7, 10).Begin );

				// gap == begin
				{
					MoveGap( doc, 5 );
					Assert.AreEqual( 7, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 5, 10).Begin );

					// word at the begin
					MoveGap( doc, 5 );
					Assert.AreEqual( 5, doc.FindPrev(new Regex("bc", RegexOptions.RightToLeft), 5, 10).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 5 );
					Assert.AreEqual( null, doc.FindPrev(new Regex("abca", RegexOptions.RightToLeft), 5, 10) );
				}

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 5 );
					Assert.AreEqual( 3, doc.FindPrev(new Regex("da", RegexOptions.RightToLeft), 0, 10).Begin );

					// word crossing the gap
					MoveGap( doc, 5 );
					Assert.AreEqual( 4, doc.FindPrev(new Regex("abc", RegexOptions.RightToLeft), 0, 10).Begin );

					// word after the gap
					MoveGap( doc, 5 );
					Assert.AreEqual( 5, doc.FindPrev(new Regex("bca", RegexOptions.RightToLeft), 0, 10).Begin );
				}

				// gap == end
				MoveGap( doc, 5 );
				Assert.AreEqual( 0, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 5).Begin );

				// end <= gap
				MoveGap( doc, 5 );
				Assert.AreEqual( 0, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 4).Begin );
			}
		}

		#region Utilities
		static void MoveGap( Document doc, int index )
		{
			doc.InternalBuffer.Insert( index, String.Empty.ToCharArray() );
		}
		#endregion
	}
}
