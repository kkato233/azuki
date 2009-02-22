// 2009-02-22
#if DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Sgry.Azuki.Test
{
	static class DocumentTest
	{
		public static void Test()
		{
			int testNum = 0;
			Console.WriteLine( "[Test for Azuki.Document]" );

			// Text / GetLineContent / GetLineContentWithEolCode
			Console.WriteLine("test {0} - Text / GetLineContent / GetLineContentWithEolCode", testNum++);
			TestUtl.Do( Test_GetText );

			// GetLineColumnIndexFromCharIndex
			Console.WriteLine("test {0} - GetLineColumnIndexFromCharIndex", testNum++);
			TestUtl.Do( Test_GetLineColumnIndexFromCharIndex );

			// GetLineIndexFromCharIndex
			Console.WriteLine("test {0} - GetLineIndexFromCharIndex", testNum++);
			TestUtl.Do( Test_GetLineIndexFromCharIndex );

			// GetTextInRange
			Console.WriteLine("test {0} - GetTextInRange", testNum++);
			TestUtl.Do( Test_GetTextInRange );

			// GetTextInRange
			Console.WriteLine("test {0} - GetTextInRange (surrogate pair)", testNum++);
			TestUtl.Do( Test_GetTextInRange_SurrogatePair );

			// Replace
			Console.WriteLine("test {0} - Replace", testNum++);
			TestUtl.Do( Test_Replace );
			TestUtl.Do( Test_Replace_SelectionRange );
			
			// GetLineLength
			Console.WriteLine("test {0} - GetLineLength", testNum++);
			TestUtl.Do( Test_GetLineLength );

			// SetSelection
			Console.WriteLine("test {0} - SetSelection", testNum++);
			TestUtl.Do( Test_Selection );

			// FindNext
			Console.WriteLine("test {0} - FindNext", testNum++);
			TestUtl.Do( Test_FindNext );

			// FindPrev
			Console.WriteLine("test {0} - FindPrev", testNum++);
			TestUtl.Do( Test_FindPrev );

			// FindNext (Regex)
			Console.WriteLine("test {0} - FindNext (regex)", testNum++);
			TestUtl.Do( Test_FindNextR );

			// FindPrev (Regex)
			Console.WriteLine("test {0} - FindPrev (regex)", testNum++);
			TestUtl.Do( Test_FindPrevR );

			Console.WriteLine("done.");
			Console.WriteLine();
		}

		static void Test_GetLineColumnIndexFromCharIndex()
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
				TestUtl.AssertEquals( 0, line );
				TestUtl.AssertEquals( i, column );
			}
			for( ; i<35; i++ )
			{
				doc.GetLineColumnIndexFromCharIndex( i, out line, out column );
				TestUtl.AssertEquals( 1, line );
				TestUtl.AssertEquals( i-31, column );
			}
			for( ; i<49; i++ )
			{
				doc.GetLineColumnIndexFromCharIndex( i, out line, out column );
				TestUtl.AssertEquals( 2, line );
				TestUtl.AssertEquals( i-35, column );
			}
			doc.GetLineColumnIndexFromCharIndex( 49, out line, out column );
			TestUtl.AssertEquals( 3, line );
			TestUtl.AssertEquals( i-49, column );

			// out of range
			try{ doc.GetLineColumnIndexFromCharIndex(50, out line, out column); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
		}

		static void Test_GetLineIndexFromCharIndex()
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
				TestUtl.AssertEquals( 0, doc.GetLineIndexFromCharIndex(i) );
			}
			for( ; i<35; i++ )
			{
				TestUtl.AssertEquals( 1, doc.GetLineIndexFromCharIndex(i) );
			}
			for( ; i<49; i++ )
			{
				TestUtl.AssertEquals( 2, doc.GetLineIndexFromCharIndex(i) );
			}
			TestUtl.AssertEquals( 3, doc.GetLineIndexFromCharIndex(49) );

			// out of range
			try{ doc.GetLineIndexFromCharIndex(50); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
		}

		static void Test_GetText()
		{
			// keep it as simple as possible\r\n
			// but\n
			// not simpler.\r\n
			Document doc = new Document();

			doc.Text = "keep it as simple as possible\r\nbut\nnot simpler.\r\n";
			TestUtl.AssertEquals( "keep it as simple as possible\r\nbut\nnot simpler.\r\n", doc.Text );
			TestUtl.AssertEquals( "keep it as simple as possible", doc.GetLineContent(0) );
			TestUtl.AssertEquals( "keep it as simple as possible\r\n", doc.GetLineContentWithEolCode(0) );
			TestUtl.AssertEquals( "but", doc.GetLineContent(1) );
			TestUtl.AssertEquals( "but\n", doc.GetLineContentWithEolCode(1) );
			TestUtl.AssertEquals( "not simpler.", doc.GetLineContent(2) );
			TestUtl.AssertEquals( "not simpler.\r\n", doc.GetLineContentWithEolCode(2) );
			TestUtl.AssertEquals( "", doc.GetLineContent(3) );
			TestUtl.AssertEquals( "", doc.GetLineContentWithEolCode(3) );
		}

		static void Test_GetTextInRange()
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
				try{ doc.GetTextInRange(-1, 4); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// valid range (begin to middle)
				TestUtl.AssertEquals( "ke", doc.GetTextInRange(0, 2) );

				// valid range (middle to middle)
				TestUtl.AssertEquals( "ep it", doc.GetTextInRange(2, 7) );

				// valid range (middle to end)
				TestUtl.AssertEquals( "simpler.", doc.GetTextInRange(39, 47) );

				// invalid range (middle to after end)
				try{ doc.GetTextInRange(39, 48); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// invalid range (minus range)
				try{ doc.GetTextInRange(10, 9); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
			}

			// line/column index type
			{
				// invalid range (before begin to middle)
				try{ doc.GetTextInRange(-1, -1, 1, 1); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
				try{ doc.GetTextInRange(-1, 0, 1, 1); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
				try{ doc.GetTextInRange(1, -1, 1, 1); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
				
				// valid range (begin to middle)
				TestUtl.AssertEquals( "ke", doc.GetTextInRange(0, 0, 0, 2) );
				TestUtl.AssertEquals( "ee", doc.GetTextInRange(0, 1, 0, 3) );
				TestUtl.AssertEquals( "as", doc.GetTextInRange(1, 0, 1, 2) );
				
				// valid range (middle to middle)
				TestUtl.AssertEquals( "s simple as possible\r\nbut\nn", doc.GetTextInRange(1, 1, 3, 1) );
				
				// valid range (middle to end)
				TestUtl.AssertEquals( "t\nnot simpler.", doc.GetTextInRange(2, 2, 3, 12) );
				
				// invalid range (middle to after end)
				try{ doc.GetTextInRange(2, 2, 3, 13); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
				try{ doc.GetTextInRange(2, 2, 4, 0); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
				try{ doc.GetTextInRange(2, 2, 4, 13); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// invalid range (minus range)
				try{ doc.GetTextInRange(1, 1, 1, 0); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
				try{ doc.GetTextInRange(1, 1, 0, 0); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
			}
		}

		static void Test_GetTextInRange_SurrogatePair()
		{
			Document doc = new Document();
			string str;
			int begin, end;
			doc.Text = "\xd85a\xdd51";

			// hi-surrogate to hi-surrogate
			str = doc.GetTextInRange( 0, 0 );
			TestUtl.AssertEquals( "", str );
			doc.SetSelection( 0, 0 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( 0, begin );
			TestUtl.AssertEquals( 0, end );

			// hi-surrogate to lo-surrogate (dividing pair)
			str = doc.GetTextInRange( 0, 1 );
			TestUtl.AssertEquals( "\xd85a\xdd51", str );
			doc.SetSelection( 0, 1 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( 0, begin );
			TestUtl.AssertEquals( 2, end );
			doc.SetSelection( 1, 0 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( 0, begin );
			TestUtl.AssertEquals( 2, end );

			// lo-surrogate to lo-surrogate
			str = doc.GetTextInRange( 1, 1 );
			TestUtl.AssertEquals( "", str );
			doc.SetSelection( 1, 1 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( 0, begin );
			TestUtl.AssertEquals( 0, end );

			// lo-surrogate to after lo-surrogate (dividing pair)
			str = doc.GetTextInRange( 1, 2 );
			TestUtl.AssertEquals( "\xd85a\xdd51", str );
			doc.SetSelection( 1, 2 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( 0, begin );
			TestUtl.AssertEquals( 2, end );
			doc.SetSelection( 2, 1 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( 0, begin );
			TestUtl.AssertEquals( 2, end );

			// after lo-surrogate to after lo-surrogate
			str = doc.GetTextInRange( 2, 2 );
			TestUtl.AssertEquals( "", str );
			doc.SetSelection( 2, 2 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( 2, begin );
			TestUtl.AssertEquals( 2, end );
		}

		static void Test_Replace()
		{
			Document doc = new Document();
			doc.Text = "keep it as simple as possible\r\nbut\nnot simpler.";
			
			// empty range (1)
			doc.Replace( "n\r", 8, 8 );
			TestUtl.AssertEquals( "keep it n\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
			TestUtl.AssertEquals( 0, doc.AnchorIndex );
			TestUtl.AssertEquals( 0, doc.CaretIndex );

			// empty range (2)
			doc.SetSelection( 9, 9 );
			doc.Replace( "ot" );
			TestUtl.AssertEquals( "keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
			TestUtl.AssertEquals( 11, doc.AnchorIndex );
			TestUtl.AssertEquals( 11, doc.CaretIndex );

			// invalid range (before begin to middle)
			try{ doc.Replace("FOO", -1, 10); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

			// valid range (begin to middle)
			{
				// replace to same length (1)
				doc.SetSelection( 0, 4 );
				doc.Replace( "KEP" );
				TestUtl.AssertEquals( "KEP it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 3, doc.AnchorIndex );
				TestUtl.AssertEquals( 3, doc.CaretIndex );

				// replace to same length (2)
				doc.Replace( "Keep", 0, 3 );
				TestUtl.AssertEquals( "Keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 4, doc.AnchorIndex );
				TestUtl.AssertEquals( 4, doc.CaretIndex );
				
				// replace to longer (1)
				doc.SetSelection( 0, 3 );
				doc.Replace( "KEEP!" );
				TestUtl.AssertEquals( "KEEP!p it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 5, doc.AnchorIndex );
				TestUtl.AssertEquals( 5, doc.CaretIndex );
				
				// replace to longer (2)
				doc.Replace( "Keeeeep", 0, 6 );
				TestUtl.AssertEquals( "Keeeeep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 7, doc.AnchorIndex );
				TestUtl.AssertEquals( 7, doc.CaretIndex );
				
				// replace to shorter (1)
				doc.SetSelection( 0, 7 );
				doc.Replace( "KEEEP" );
				TestUtl.AssertEquals( "KEEEP it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 5, doc.AnchorIndex );
				TestUtl.AssertEquals( 5, doc.CaretIndex );

				// replace to shorter (2)
				doc.Replace( "Keep", 0, 5 );
				TestUtl.AssertEquals( "Keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 4, doc.AnchorIndex );
				TestUtl.AssertEquals( 4, doc.CaretIndex );
			}
			
			// valid range (middle to middle)
			{
				// replace to same length (1)
				doc.Replace( "ZIMPLE", 15, 21 );
				TestUtl.AssertEquals( "Keep it not\ras ZIMPLE as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 4, doc.AnchorIndex );
				TestUtl.AssertEquals( 4, doc.CaretIndex );

				// replace to same length (2)
				doc.SetSelection( 15, 21 );
				doc.Replace( "SIMPLE" );
				TestUtl.AssertEquals( "Keep it not\ras SIMPLE as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 21, doc.AnchorIndex );
				TestUtl.AssertEquals( 21, doc.CaretIndex );

				// replace to longer (1)
				doc.SetSelection( 14, 15 );
				doc.Replace( "COMPLEX", 15, 21 );
				TestUtl.AssertEquals( "Keep it not\ras COMPLEX as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 14, doc.AnchorIndex );
				TestUtl.AssertEquals( 22, doc.CaretIndex );
				
				// replace to longer (2)
				doc.SetSelection( 19, 22 );
				doc.Replace( "LEX!" );
				TestUtl.AssertEquals( "Keep it not\ras COMPLEX! as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 23, doc.AnchorIndex );
				TestUtl.AssertEquals( 23, doc.CaretIndex );

				// replace to shorter (1)
				doc.Replace( "simple!", 15, 23 );
				TestUtl.AssertEquals( "Keep it not\ras simple! as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 22, doc.AnchorIndex );
				TestUtl.AssertEquals( 22, doc.CaretIndex );
				
				// replace to shorter (2)
				doc.SetSelection( 15, 22 );
				doc.Replace( "simple" );
				TestUtl.AssertEquals( "Keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 21, doc.AnchorIndex );
				TestUtl.AssertEquals( 21, doc.CaretIndex );
			}
			
			// valid range (middle to end)
			{
				// replace to same length (1)
				doc.Replace( "?", 50, 51 );
				TestUtl.AssertEquals( "Keep it not\ras simple as possible\r\nbut\nnot simpler?", doc.Text );
				TestUtl.AssertEquals( 21, doc.AnchorIndex );
				TestUtl.AssertEquals( 21, doc.CaretIndex );
				
				// replace to same length (2)
				doc.SetSelection( 50, 51 );
				doc.Replace( "!" );
				TestUtl.AssertEquals( "Keep it not\ras simple as possible\r\nbut\nnot simpler!", doc.Text );
				TestUtl.AssertEquals( 51, doc.AnchorIndex );
				TestUtl.AssertEquals( 51, doc.CaretIndex );

				// replace to longer (1)
				doc.Replace( "??", 50, 51 );
				TestUtl.AssertEquals( "Keep it not\ras simple as possible\r\nbut\nnot simpler??", doc.Text );
				TestUtl.AssertEquals( 52, doc.AnchorIndex );
				TestUtl.AssertEquals( 52, doc.CaretIndex );

				// replace to longer (2)
				doc.Replace( "!!!", 50, 52 );
				TestUtl.AssertEquals( "Keep it not\ras simple as possible\r\nbut\nnot simpler!!!", doc.Text );
				TestUtl.AssertEquals( 53, doc.AnchorIndex );
				TestUtl.AssertEquals( 53, doc.CaretIndex );
				
				// replace to shorter (1)
				doc.Replace( "..", 50, 53 );
				TestUtl.AssertEquals( "Keep it not\ras simple as possible\r\nbut\nnot simpler..", doc.Text );
				TestUtl.AssertEquals( 52, doc.AnchorIndex );
				TestUtl.AssertEquals( 52, doc.CaretIndex );
				
				// replace to shorter (2)
				doc.Replace( ".", 50, 52 );
				TestUtl.AssertEquals( "Keep it not\ras simple as possible\r\nbut\nnot simpler.", doc.Text );
				TestUtl.AssertEquals( 51, doc.AnchorIndex );
				TestUtl.AssertEquals( 51, doc.CaretIndex );
			}
			
			// invalid range (middle to after end)
			try{ doc.Replace("PIYO", 51, 53); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
		}

		static void Test_Replace_SelectionRange()
		{
			Document doc = new Document();

			{
				// replace before head to before head
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "1", 0, 0 ); // 1ab[cd)ef
				TestUtl.AssertEquals( 3, doc.AnchorIndex );
				TestUtl.AssertEquals( 5, doc.CaretIndex );

				// replace before head to head
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "2", 0, 2 ); // 2[cd)ef
				TestUtl.AssertEquals( 1, doc.AnchorIndex );
				TestUtl.AssertEquals( 3, doc.CaretIndex );

				// replace before head to middle
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "G", 0, 3 ); // G[d)ef
				TestUtl.AssertEquals( 1, doc.AnchorIndex );
				TestUtl.AssertEquals( 2, doc.CaretIndex );

				// replace before head to end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "hoge", 0, 4 ); // hoge[)ef
				TestUtl.AssertEquals( 4, doc.AnchorIndex );
				TestUtl.AssertEquals( 4, doc.CaretIndex );

				// replace before head to after end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "Z", 0, 5 ); // Z[)f
				TestUtl.AssertEquals( 1, doc.AnchorIndex );
				TestUtl.AssertEquals( 1, doc.CaretIndex );
			}

			{
				// replace head to head
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "2", 2, 2 ); // ab2[cd)ef
				TestUtl.AssertEquals( 3, doc.AnchorIndex );
				TestUtl.AssertEquals( 5, doc.CaretIndex );

				// replace head to middle
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "G", 2, 3 ); // abG[d)ef
				TestUtl.AssertEquals( 3, doc.AnchorIndex );
				TestUtl.AssertEquals( 4, doc.CaretIndex );

				// replace head to end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "hoge", 2, 4 ); // abhoge[)ef
				TestUtl.AssertEquals( 6, doc.AnchorIndex );
				TestUtl.AssertEquals( 6, doc.CaretIndex );

				// replace head to after end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "Z", 2, 5 ); // abZ[)f
				TestUtl.AssertEquals( 3, doc.AnchorIndex );
				TestUtl.AssertEquals( 3, doc.CaretIndex );
			}

			{
				// replace middle to middle
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "!", 3, 3 ); // ab[c!d)ef
				TestUtl.AssertEquals( 2, doc.AnchorIndex );
				TestUtl.AssertEquals( 5, doc.CaretIndex );

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
				TestUtl.AssertEquals( 2, doc.AnchorIndex );
				TestUtl.AssertEquals( 4, doc.CaretIndex );
			}

			{
				// replace end to end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "?", 4, 4 ); // ab[cd?)ef
				TestUtl.AssertEquals( 2, doc.AnchorIndex );
				TestUtl.AssertEquals( 5, doc.CaretIndex );

				// replace end to after end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "Z", 4, 5 ); // ab[cdZ)f
				TestUtl.AssertEquals( 2, doc.AnchorIndex );
				TestUtl.AssertEquals( 5, doc.CaretIndex );
			}

			{
				// replace after end to after end
				doc.Text = "abcdef";
				doc.SetSelection( 2, 4 ); // ab[cd)ef
				doc.Replace( "Z", 5, 5 ); // ab[cd)eZf
				TestUtl.AssertEquals( 2, doc.AnchorIndex );
				TestUtl.AssertEquals( 4, doc.CaretIndex );
			}
		}

		static void Test_GetLineLength()
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

			TestUtl.AssertEquals( 7, doc.GetLineLength(0) );
			TestUtl.AssertEquals( 0, doc.GetLineLength(1) );
			TestUtl.AssertEquals( 21, doc.GetLineLength(2) );
			TestUtl.AssertEquals( 0, doc.GetLineLength(3) );
			TestUtl.AssertEquals( 3, doc.GetLineLength(4) );
			TestUtl.AssertEquals( 0, doc.GetLineLength(5) );
			TestUtl.AssertEquals( 12, doc.GetLineLength(6) );
			try{ doc.GetLineLength(7); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
		}

		static void Test_Selection()
		{
			Document doc = new Document();
			int begin, end;

			doc.Text = "臼と似た形をした文字「\xd85a\xdd51」は、UCS 文字空間の第２面に位置する";

			// before head to head
			try{ doc.SetSelection(-1, 0); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
			
			// head to head
			doc.SetSelection( 0, 0 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( begin, 0 );
			TestUtl.AssertEquals( end, 0 );
			
			// head to before hi-surrogate
			doc.SetSelection( 0, 4 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( begin, 0 );
			TestUtl.AssertEquals( end, 4 );
			
			// before hi-surrogate to lo-surrogate
			doc.SetSelection( 4, 11 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( begin, 4 );
			TestUtl.AssertEquals( end, 11 );
			
			// hi-surrogate to lo-surrogate
			doc.SetSelection( 11, 12 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( begin, 11 );
			TestUtl.AssertEquals( end, 13 ); // surely expanded?

			// hi-surrogate to after lo-surrogate
			doc.SetSelection( 11, 13 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( begin, 11 );
			TestUtl.AssertEquals( end, 13 );

			// middle of the surrogate pair
			// ('moving caret between the pair' must be treated as 'moving caret to the *char*')
			doc.SetSelection( 12, 12 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( begin, 11 );
			TestUtl.AssertEquals( end, 11 );

			// lo-surrogate to after lo-surrogate
			doc.SetSelection( 12, 13 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( begin, 11 );
			TestUtl.AssertEquals( end, 13 );

			// after lo-surrogate to end
			doc.SetSelection( 13, 33 );
			doc.GetSelection( out begin, out end );
			TestUtl.AssertEquals( begin, 13 );
			TestUtl.AssertEquals( end, 33 );

			// end to after end
			try{ doc.SetSelection(33, 36); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
		}

		static void Test_FindNext()
		{
			Document doc = new Document();
			doc.Replace( "aababcabcd" );

			// black box test (interface test)
			{
				// null target
				try{ doc.FindNext((string)null, 0); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentNullException>(ex); }

				// negative index
				try{ doc.FindNext("a", -1); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// end index at out of range
				try{ doc.FindNext("a", 0, doc.Length+1, true); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// inverted range
				try{ doc.FindNext("a", 1, 0, true); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentException>(ex); }

				// empty range
				TestUtl.AssertEquals( null, doc.FindNext("a", 0, 0, true) );

				// find in valid range
				TestUtl.AssertEquals( 0, doc.FindNext("a", 0, 1, true).Begin );
				TestUtl.AssertEquals( 1, doc.FindNext("ab", 0).Begin );
				TestUtl.AssertEquals( 3, doc.FindNext("abc", 0).Begin );
				TestUtl.AssertEquals( 6, doc.FindNext("abcd", 0).Begin );
				TestUtl.AssertEquals( null, doc.FindNext("abcde", 0) );

				// empty pattern (returns begin index)
				TestUtl.AssertEquals( 1, doc.FindNext("", 1).Begin );

				// comp. options
				TestUtl.AssertEquals( null, doc.FindNext("aBcD", 0, doc.Length, true) );
				TestUtl.AssertEquals(  6, doc.FindNext("aBcD", 0, doc.Length, false).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 6, doc.FindNext("ab", 5, doc.Length, true).Begin );

				// gap == begin
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 6, doc.FindNext("ab", 4, doc.Length, true).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 2, doc.FindNext("ba", 2, doc.Length, true).Begin );

					// word crossing the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 3, doc.FindNext("ab", 2, doc.Length, true).Begin );

					// word after the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 5, doc.FindNext("cab", 2, doc.Length, true).Begin );
				}

				// gap == end
				{
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 1, doc.FindNext("ab", 0, 4, true).Begin );

					// word at the end
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 2, doc.FindNext("ba", 0, 4, true).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( null, doc.FindNext("abc", 0, 4, true) );
				}

				// end <= gap
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 1, doc.FindNext("ab", 0, 4, true).Begin );
			}
		}

		static void Test_FindPrev()
		{
			Document doc = new Document();
			doc.Replace( "abcdabcaba" );

			// black box test (interface test)
			{
				// null target
				try{ doc.FindPrev((string)null, 0, 10, true); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentNullException>(ex); }

				// negative index
				try{ doc.FindPrev("a", -1, 10, true); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// end index at out of range
				try{ doc.FindPrev("a", 0, doc.Length+1, true); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// inverted range
				try{ doc.FindPrev("a", 1, 0, true); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentException>(ex); }

				// empty range
				TestUtl.AssertEquals( null, doc.FindPrev("a", 0, 0, true) );

				// find in valid range
				TestUtl.AssertEquals( 9, doc.FindPrev(   "a", 0, 10, true).Begin );
				TestUtl.AssertEquals( 7, doc.FindPrev(  "ab", 0, 10, true).Begin );
				TestUtl.AssertEquals( 4, doc.FindPrev( "abc", 0, 10, true).Begin );
				TestUtl.AssertEquals( 0, doc.FindPrev("abcd", 0, 10, true).Begin );
				TestUtl.AssertEquals( null, doc.FindPrev("abcde", 0, 10, true) );

				// empty pattern (returns end index)
				TestUtl.AssertEquals( 10, doc.FindPrev("", 0, 10, true).Begin );

				// comp. options
				TestUtl.AssertEquals( null, doc.FindPrev("aBcD", 0, 10, true) );
				TestUtl.AssertEquals(  0, doc.FindPrev("aBcD", 0, 10, false).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: abcda......bcaba)

				// gap < begin
				MoveGap( doc, 5 );
				TestUtl.AssertEquals( 7, doc.FindPrev("ab", 7, 10, true).Begin );

				// gap == begin
				{
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 7, doc.FindPrev("ab", 5, 10, true).Begin );

					// word at the begin
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 5, doc.FindPrev("bc", 5, 10, true).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( null, doc.FindPrev("abca", 5, 10, true) );
				}

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 3, doc.FindPrev("da", 0, 10, true).Begin );

					// word crossing the gap
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 4, doc.FindPrev("abc", 0, 10, true).Begin );

					// word after the gap
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 5, doc.FindPrev("bca", 0, 10, true).Begin );
				}

				// gap == end
				MoveGap( doc, 5 );
				TestUtl.AssertEquals( 0, doc.FindPrev("ab", 0, 5, true).Begin );

				// end <= gap
				MoveGap( doc, 5 );
				TestUtl.AssertEquals( 0, doc.FindPrev("ab", 0, 4, true).Begin );
			}
		}

		static void Test_FindNextR()
		{
			Document doc = new Document();
			SearchResult result;
			doc.Replace( "aababcabcd" );

			// black box test
			{
				// null argument
				try{ doc.FindNext((Regex)null, 1, 2); Debug.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentNullException>(ex); }

				// negative index
				try{ doc.FindNext(new Regex("a[^b]+"), -1, 2); Debug.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// inverted range
				try{ doc.FindNext(new Regex("a[^b]+"), 2, 1); Debug.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentException>(ex); }

				// empty range
				result = doc.FindNext( new Regex("a[^b]+"), 0, 0 );
				TestUtl.AssertEquals( null, result );

				// range exceeding text length
				try{ doc.FindNext(new Regex("a[^b]+"), 1, 9999); Debug.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// invalid Regex option
				try{ doc.FindNext(new Regex("a[^b]+", RegexOptions.RightToLeft), 1, 4); Debug.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentException>(ex); }

				// pattern ord at begin
				result = doc.FindNext( new Regex("a[^b]+"), 0, 2 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 2, result.End );

				// pattern in the range
				result = doc.FindNext( new Regex("a[^a]+"), 0, 3 );
				TestUtl.AssertEquals( 1, result.Begin );
				TestUtl.AssertEquals( 3, result.End );

				// pattern which ends at end
				result = doc.FindNext( new Regex("[ab]+"), 0, 5 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 5, result.End );

				// pattern... well, pretty hard to describe in English for me...
				result = doc.FindNext( new Regex("[abc]+"), 0, 5 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 5, result.End );
				result = doc.FindNext( new Regex("[abc]+"), 0, 10 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 9, result.End );

				// empty pattern
				result = doc.FindNext( new Regex(""), 0, 10 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 0, result.End );

				// comp. options
				result = doc.FindNext( new Regex("aBcD"), 0, doc.Length );
				TestUtl.AssertEquals( null, result );
				result = doc.FindNext( new Regex("aBcD", RegexOptions.IgnoreCase), 0, doc.Length );
				TestUtl.AssertEquals(  6, result.Begin );
				TestUtl.AssertEquals( 10, result.End);
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 6, doc.FindNext(new Regex("ab"), 5, doc.Length).Begin );

				// gap == begin
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 6, doc.FindNext(new Regex("ab"), 4, doc.Length).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 2, doc.FindNext(new Regex("ba"), 2, doc.Length).Begin );

					// word crossing the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 3, doc.FindNext(new Regex("ab"), 2, doc.Length).Begin );

					// word after the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 5, doc.FindNext(new Regex("cab"), 2, doc.Length).Begin );
				}

				// gap == end
				{
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 1, doc.FindNext(new Regex("ab"), 0, 4).Begin );

					// word at the end
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 2, doc.FindNext(new Regex("ba"), 0, 4).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( null, doc.FindNext(new Regex("abc"), 0, 4) );
				}

				// end <= gap
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 1, doc.FindNext(new Regex("ab"), 0, 4).Begin );
			}
		}

		static void Test_FindPrevR()
		{
			Document doc = new Document();
			doc.Replace( "abcdabcaba" );

			// black box test (interface test)
			{
				// null target
				try{ doc.FindPrev((Regex)null, 0, 10); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentNullException>(ex); }

				// negative index
				try{ doc.FindPrev(new Regex("a", RegexOptions.RightToLeft), -1, 10); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// invalid regex option
				try{ doc.FindPrev(new Regex("a", RegexOptions.None), 0, doc.Length); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentException>(ex); }

				// end index at out of range
				try{ doc.FindPrev(new Regex("a", RegexOptions.RightToLeft), 0, doc.Length+1); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

				// inverted range
				try{ doc.FindPrev(new Regex("a", RegexOptions.RightToLeft), 1, 0); DebugUtl.Fail("Exception wasn't thrown as expected."); }
				catch( Exception ex ){ TestUtl.AssertType<ArgumentException>(ex); }

				// empty range
				TestUtl.AssertEquals( (Regex)null, doc.FindPrev(new Regex("a", RegexOptions.RightToLeft), 0, 0) );

				// find in valid range
				TestUtl.AssertEquals( 9, doc.FindPrev(new Regex(   "a", RegexOptions.RightToLeft), 0, 10).Begin );
				TestUtl.AssertEquals( 7, doc.FindPrev(new Regex(  "ab", RegexOptions.RightToLeft), 0, 10).Begin );
				TestUtl.AssertEquals( 4, doc.FindPrev(new Regex( "abc", RegexOptions.RightToLeft), 0, 10).Begin );
				TestUtl.AssertEquals( 0, doc.FindPrev(new Regex("abcd", RegexOptions.RightToLeft), 0, 10).Begin );
				TestUtl.AssertEquals( null, doc.FindPrev(new Regex("abcde", RegexOptions.RightToLeft), 0, 10) );

				// empty pattern (returns end index)
				TestUtl.AssertEquals( 10, doc.FindPrev(new Regex("", RegexOptions.RightToLeft), 0, 10).Begin );

				// comp. options
				TestUtl.AssertEquals( null, doc.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft), 0, 10) );
				TestUtl.AssertEquals(  0, doc.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft|RegexOptions.IgnoreCase), 0, 10).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: abcda......bcaba)

				// gap < begin
				MoveGap( doc, 5 );
				TestUtl.AssertEquals( 7, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 7, 10).Begin );

				// gap == begin
				{
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 7, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 5, 10).Begin );

					// word at the begin
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 5, doc.FindPrev(new Regex("bc", RegexOptions.RightToLeft), 5, 10).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( null, doc.FindPrev(new Regex("abca", RegexOptions.RightToLeft), 5, 10) );
				}

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 3, doc.FindPrev(new Regex("da", RegexOptions.RightToLeft), 0, 10).Begin );

					// word crossing the gap
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 4, doc.FindPrev(new Regex("abc", RegexOptions.RightToLeft), 0, 10).Begin );

					// word after the gap
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 5, doc.FindPrev(new Regex("bca", RegexOptions.RightToLeft), 0, 10).Begin );
				}

				// gap == end
				MoveGap( doc, 5 );
				TestUtl.AssertEquals( 0, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 5).Begin );

				// end <= gap
				MoveGap( doc, 5 );
				TestUtl.AssertEquals( 0, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 4).Begin );
			}
		}

		static void MoveGap( Document doc, int index )
		{
			doc.InternalBuffer.Insert( index, String.Empty.ToCharArray() );
		}
	}
}
#endif
