// 2008-07-05
#if DEBUG
using System;

namespace Sgry.Azuki.Test
{
	using Highlighter;
	using Windows;

	static class KeywordHighlighterTest
	{
		public static void Test()
		{
			int testNum = 0;
			Console.WriteLine( "[Test for Azuki.KeywordHighlighter]" );

			// around keywords
			Console.WriteLine("test {0} - Keywords", testNum++);
			TestUtl.Do( Test_Keywords );

			// around line-comment
			Console.WriteLine("test {0} - Line-Comment", testNum++);
			TestUtl.Do( Test_LineComment );

			// around asymmetric enclosing pair
			Console.WriteLine("test {0} - EPI / Asymmetric enclosing pair", testNum++);
			TestUtl.Do( Test_EnclosingPairs_EPI_Asym );

			// around symmetric enclosing pair
			Console.WriteLine("test {0} - EPI / Symmetric enclosing pair", testNum++);
			TestUtl.Do( Test_EnclosingPairs_EPI_Sym );

			// around escapement
			Console.WriteLine("test {0} - Escape of enclosing pair", testNum++);
			TestUtl.Do( Test_EnclosingPairs_Escape );

			// around enclosing pairs
			Console.WriteLine("test {0} - Enclosing Pairs", testNum++);
			TestUtl.Do( Test_EnclosingPairs );

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine( "done." );
			Console.ResetColor();
			Console.WriteLine();
		}

		static void Test_LineComment()
		{
			Document doc = new Document();
			KeywordHighlighter h = new KeywordHighlighter();
			h.AddLineHighlight( "///", CharClass.DocComment );
			h.AddLineHighlight( "//", CharClass.Comment );
			doc.Highlighter = h;
			//---------------------------------------------

			const string initText =
@"hoge
//hoge
ho///ge
hoge//
ho//ge";

			doc.Text = initText;
			h.Highlight( doc );

			int i=0;
			for( ; i<6; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<14; i++ )
				TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(i) );
			for( ; i<16; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<23; i++ )
				TestUtl.AssertEquals( CharClass.DocComment, doc.GetCharClass(i) );
			for( ; i<27; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<31; i++ )
				TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(i) );
		}

		static void Test_Keywords()
		{
			Document doc = new Document();
			KeywordHighlighter h = new KeywordHighlighter();
			h.AddEnclosure( "\"", "\"", CharClass.String, '\\' );
			h.AddEnclosure( "/*", "*/", CharClass.Comment );
			h.SetKeywords( new string[]{
				"if", "int", "interface", "for"
			}, CharClass.Keyword );
			doc.Highlighter = h;
			//---------------------------------------------

			int i;

			// "int" --> "int "
			doc.Text = "int";
			h.Highlight( doc );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( " ", 3, 3 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "int ", doc.Text );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			for( ; i<4; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			try{ doc.GetCharClass(4); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }

			// "int" --> "-int"
			doc.Text = "int";
			h.Highlight( doc );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( "-", 0, 0 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "-int", doc.Text );
			for( i=0; i<1; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<4; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			try{ doc.GetCharClass(4); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }

			// in --> int
			doc.Text = "in";
			h.Highlight( doc );
			for( i=0; i<2; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( "t", 2, 2 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "int", doc.Text );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			try{ doc.GetCharClass(3); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }

			// it --> int
			doc.Text = "it";
			h.Highlight( doc );
			for( i=0; i<2; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( "n", 1, 1 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "int", doc.Text );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			try{ doc.GetCharClass(3); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }

			// nt --> int
			doc.Text = "nt";
			h.Highlight( doc );
			for( i=0; i<2; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( "i", 0, 0 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "int", doc.Text );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			try{ doc.GetCharClass(3); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }

			// "insert at" --> int
			doc.Text = "insert at";
			h.Highlight( doc );
			for( i=0; i<9; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( "n", 1, 8 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "int", doc.Text );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			try{ doc.GetCharClass(3); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }

			// "hoge" --> "h int e"
			doc.Text = "hoge";
			h.Highlight( doc );
			for( i=0; i<4; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( " int ", 1, 3 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "h int e", doc.Text );
			for( i=0; i<2; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<5; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			for( ; i<7; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			try{ doc.GetCharClass(7); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }

			// "int" --> "if!"
			doc.Text = "int";
			h.Highlight( doc );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( "f!", 1, 3 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "if!", doc.Text );
			for( i=0; i<2; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			for( ; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			try{ doc.GetCharClass(3); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }

			// "int" --> "inte"
			doc.Text = "int";
			h.Highlight( doc );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( "e", 3, 3 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "inte", doc.Text );
			for( i=0; i<4; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			try{ doc.GetCharClass(4); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }

			// "int" --> "interface"
			doc.Text = "int";
			h.Highlight( doc );
			for( i=0; i<3; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( "erface", 3, 3 );
			h.Highlight( doc );
			TestUtl.AssertEquals( "interface", doc.Text );
			for( i=0; i<9; i++ )
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(i) );
			try{ doc.GetCharClass(10); DebugUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertExceptionType(ex , typeof(ArgumentOutOfRangeException)); }
		}

		static void Test_EnclosingPairs_EPI_Asym()
		{
			Document doc = new Document();
			KeywordHighlighter h = new KeywordHighlighter();
			int begin_, end_;
			h.AddEnclosure( "/*", "*/", CharClass.Comment );
			doc.Highlighter = h;
			//---------------------------------------------

			const string initText = "The /*UpdateEPI*/ method is /*currently */main target.";
			{
				doc.Text = initText;
				h.Highlight( doc );
				TestUtl.AssertEquals( "4 17 28 42", h._EPI.ToString() );
			}

			// replace from out to in
			{
				const int begin = 4;
				const int end = 12;

				// no pair
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "**XX**", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The **XX**EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "26 40", h._EPI.ToString() );

				// opener
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "/*XX**", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*XX**EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 15 26 40", h._EPI.ToString() );

				// closer
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "**XX*/", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The **XX*/EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "26 40", h._EPI.ToString() );

				// opener + closer
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "/*XX*/", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*XX*/EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 10 26 40", h._EPI.ToString() );

				// closer + opener
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "*/XX/*", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The */XX/*EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "8 15 26 40", h._EPI.ToString() );
			}

			// replace from in to in
			{
				const int begin=8, end=12;

				// no pair
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "**XX**", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*Up**XX**EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 19 30 44", h._EPI.ToString() );

				// opener
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "/*XX**", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*Up/*XX**EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 19 30 44", h._EPI.ToString() );

				// closer
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "**XX*/", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*Up**XX*/EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 14 30 44", h._EPI.ToString() );

				// opener + closer
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "/*XX*/", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*Up/*XX*/EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 14 30 44", h._EPI.ToString() );

				// closer + opener
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "*/XX/*", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*Up*/XX/*EPI*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 10 12 19 30 44", h._EPI.ToString() );
			}

			// replace from in to out
			{
				int begin=13, end=17;

				// no pair
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "**XX**", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateE**XX** method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 44", h._EPI.ToString() );

				// opener
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "/*XX**", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateE/*XX** method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 44", h._EPI.ToString() );

				// closer
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "**XX*/", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateE**XX*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 19 30 44", h._EPI.ToString() );

				// opener + closer
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "/*XX*/", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateE/*XX*/ method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 19 30 44", h._EPI.ToString() );

				// closer + opener
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "*/XX/*", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateE*/XX/* method is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 15 17 44", h._EPI.ToString() );
			}

			// replace from out to out
			{
				const int begin=19, end=23;

				// no pair
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "**XX**", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateEPI*/ m**XX**d is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 17 30 44", h._EPI.ToString() );

				// opener
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "**XX/*", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateEPI*/ m**XX/*d is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 17 23 44", h._EPI.ToString() );

				// closer
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "**XX*/", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateEPI*/ m**XX*/d is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 17 30 44", h._EPI.ToString() );

				// opener + closer
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "/*XX*/", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateEPI*/ m/*XX*/d is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 17 19 25 30 44", h._EPI.ToString() );

				// closer + opener
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "*/XX/*", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The /*UpdateEPI*/ m*/XX/*d is /*currently */main target.", doc.Text );
				TestUtl.AssertEquals( "4 17 23 44", h._EPI.ToString() );
			}
		}

		static void Test_EnclosingPairs_EPI_Sym()
		{
			Document doc = new Document();
			KeywordHighlighter h = new KeywordHighlighter();
			int begin_, end_;
			h.AddEnclosure( "\"", "\"", CharClass.String, '\\' );
			doc.Highlighter = h;
			//---------------------------------------------

			const string initText = "The \"UpdateEPI\" method is \"currently \"main target.";
			{
				doc.Text = initText;
				h.Highlight( doc );
				TestUtl.AssertEquals( "4 15 26 38", h._EPI.ToString() );
			}

			// replace from out to in
			{
				int begin=4, end=11;

				// no pair
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "XXXX", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The XXXXEPI\" method is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "11 24 34", h._EPI.ToString() );

				// odd number of pairs
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "X\"XX", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The X\"XXEPI\" method is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "5 12 23 35", h._EPI.ToString() );

				// even number of pairs
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "X\"X\"", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The X\"X\"EPI\" method is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "5 8 11 24 34", h._EPI.ToString() );
			}

			// replace from in to in
			{
				int begin=7, end=11;

				// no pair
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "h/og/e", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The \"Uph/og/eEPI\" method is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "4 17 28 40", h._EPI.ToString() );

				// odd number of pairs
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "h\"og/e", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The \"Uph\"og/eEPI\" method is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "4 9 16 29 39", h._EPI.ToString() );

				// even number of pairs
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "h\"og\"e", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The \"Uph\"og\"eEPI\" method is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "4 9 11 17 28 40", h._EPI.ToString() );
			}

			// replace from in to out
			{
				int begin=11, end=15;

				// no pair
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "XXXX", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The \"UpdateXXXX method is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "4 27 37", h._EPI.ToString() );

				// odd number of pairs
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "XX\"X", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The \"UpdateXX\"X method is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "4 14 26 38", h._EPI.ToString() );

				// even number of pairs
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "\"XX\"", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The \"Update\"XX\" method is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "4 12 14 27 37", h._EPI.ToString() );
			}

			// replace from out to out
			{
				int begin=17, end=21;

				// no pair
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "h/og/e", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The \"UpdateEPI\" mh/og/ed is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "4 15 28 40", h._EPI.ToString() );

				// odd number of pairs
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "h\"og/e", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The \"UpdateEPI\" mh\"og/ed is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "4 15 18 29 39", h._EPI.ToString() );

				// even number of pairs
				doc.Text = initText;
				h.Highlight( doc );
				doc.Replace( "h\"og\"e", begin, end );
				begin_ = begin; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( "The \"UpdateEPI\" mh\"og\"ed is \"currently \"main target.", doc.Text );
				TestUtl.AssertEquals( "4 15 18 22 28 40", h._EPI.ToString() );
			}
		}

		static void Test_EnclosingPairs_Escape()
		{
			Document doc = new Document();
			KeywordHighlighter h = new KeywordHighlighter();
			h.AddEnclosure( "\"", "\"", CharClass.String, '\\' );
			h.AddEnclosure( "/*", "*/", CharClass.Comment );
			doc.Highlighter = h;
			int i;

			// syn
			doc.Text = "A\"B\\\"C\"D";
			h.Highlight( doc );
			for( i=0; i<1; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<7; i++ )
				TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(i) );
			for( ; i<8; i++ )
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(i) );
		}

		static void Test_EnclosingPairs()
		{
			Document doc = new Document();
			KeywordHighlighter h = new KeywordHighlighter();
			int begin_, end_;
			h.AddEnclosure( "\"", "\"", CharClass.String );
			h.AddEnclosure( "/*", "*/", CharClass.Comment );
			h.SetKeywords( new string[]{
				"if", "int", "interface", "for"
			}, CharClass.Keyword );
			doc.Highlighter = h;
			//---------------------------------------------

			{
				doc.Text = @"printf(""%s\n"", name);"; // printf("%s\n", name);
				h.Highlight( doc );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(6) );
				TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(7) );
				TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(12) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(13) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(20) );

				doc.Replace( "Z", 7, 8 ); // printf(Z%s\n", name);
				begin_ = 7; end_ = doc.Length;
				h.Highlight( doc, ref begin_, ref end_ );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(11) );
				TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(12) );
				TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(20) );
			}
		}
	}
}
#endif
