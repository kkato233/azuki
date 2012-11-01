#if TEST
using System;

namespace Sgry.Azuki.Test
{
	using Highlighter;
	using WinForms;

	static class KeywordHighlighterTest
	{
		public static void Test()
		{
			int testNum = 0;
			Console.WriteLine( "[Test for Azuki.KeywordHighlighter]" );

			Console.WriteLine("test {0} - Keywords", testNum++);
			TestUtl.Do( Test_Keywords );

			Console.WriteLine("test {0} - Line highlight", testNum++);
			TestUtl.Do( Test_LineComment );

			Console.WriteLine("test {0} - Enclosure", testNum++);
			TestUtl.Do( Test_Enclosure );

			Console.WriteLine("test {0} - Word character", testNum++);
			TestUtl.Do( Test_WordChar );

			Console.WriteLine("test {0} - Hook", testNum++);
			TestUtl.Do( Test_Hook );

			Console.WriteLine( "done." );
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

		static void Test_Enclosure()
		{
			Document doc = new Document();
			KeywordHighlighter h = new KeywordHighlighter();
			h.AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );
			h.AddEnclosure( "/*", "*/", CharClass.Comment, true );
			doc.Highlighter = h;
			//---------------------------------------------

			doc.Text = @"""";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(0) );

			doc.Text = @"a""";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(1) );

			doc.Text = @"a""b";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(2) );

			doc.Text = @"a""b""";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(2) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(3) );

			doc.Text = @"a""b""c";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(2) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(3) );
			TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(4) );

			doc.Text = @"a""b\""c";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(2) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(3) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(4) );
			TestUtl.AssertEquals( CharClass.String, doc.GetCharClass(5) );

			doc.Text = @"/";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );

			doc.Text = @"/*";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(1) );

			doc.Text = @"/**";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(2) );

			doc.Text = @"/**a";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(2) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(3) );

			doc.Text = @"/*a*";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(2) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(3) );

			doc.Text = @"/**/";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(2) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(3) );

			doc.Text = @"/*a*/";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(2) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(3) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(4) );

			doc.Text = @"a/*a*/a";
			h.Highlight( doc );
			TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(1) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(2) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(3) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(4) );
			TestUtl.AssertEquals( CharClass.Comment, doc.GetCharClass(5) );
			TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(6) );
		}

		static void Test_Keywords()
		{
			Document doc = new Document();
			KeywordHighlighter h = new KeywordHighlighter();
			h.AddEnclosure( "\"", "\"", CharClass.String, '\\' );
			h.AddEnclosure( "/*", "*/", CharClass.Comment );
			h.AddKeywordSet( new string[]{
				"for", "if", "int", "interface", "join"
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
			try{ doc.GetCharClass(4); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
			try{ doc.GetCharClass(4); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
			try{ doc.GetCharClass(3); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
			try{ doc.GetCharClass(3); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
			try{ doc.GetCharClass(3); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
			try{ doc.GetCharClass(3); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
			try{ doc.GetCharClass(7); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
			try{ doc.GetCharClass(3); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
			try{ doc.GetCharClass(4); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
			try{ doc.GetCharClass(10); TestUtl.Fail("Exception wasn't thrown as expected."); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
		}

		static void Test_WordChar()
		{
			Document doc = new Document();
			KeywordHighlighter h;

			//---------------------------------------------
			h = new KeywordHighlighter();
			{
				h.AddKeywordSet( new string[]{"SELECT"}, CharClass.Keyword );
				doc.Highlighter = h;

				doc.Text = @"SELECT ABC-SELECT SELECT-ABC";
				h.Highlight( doc );
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(0) );		// S<--
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(5) );		// SELECT<--
				TestUtl.AssertEquals( CharClass.Normal,  doc.GetCharClass(6) );		// SELECT <--
				TestUtl.AssertEquals( CharClass.Normal,  doc.GetCharClass(7) );		// SELECT A<--
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(11) );	// SELECT ABC-S<--
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(16) );	// SELECT ABC-SELECT<--
				TestUtl.AssertEquals( CharClass.Normal,  doc.GetCharClass(17) );	// SELECT ABC-SELECT <--
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(23) );	// SELECT ABC-SELECT SELECT<--
				TestUtl.AssertEquals( CharClass.Normal,  doc.GetCharClass(26) );	// SELECT ABC-SELECT SELECT-ABC<--
			}

			h = new KeywordHighlighter();
			{
				h.AddKeywordSet( new string[]{"SELECT"}, CharClass.Keyword );
				h.WordCharSet = "-ABCDEFGHIJKLMNOPQRSTUVWXYZ";
				doc.Highlighter = h;

				doc.Text = @"SELECT ABC-SELECT SELECT-ABC";
				h.Highlight( doc );
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(0) );	// S<--
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(5) );	// SELECT<--
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(6) );	// SELECT <--
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(7) );	// SELECT A<--
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(11) );	// SELECT ABC-S<--
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(16) );	// SELECT ABC-SELECT<--
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(17) );	// SELECT ABC-SELECT <--
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(23) );	// SELECT ABC-SELECT SELECT<--
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(26) );	// SELECT ABC-SELECT SELECT-ABC<--
			}
		}

		static void Test_Hook()
		{
			Document doc = new Document();
			KeywordHighlighter h;

			//---------------------------------------------
			h = new KeywordHighlighter();
			{
				h.AddKeywordSet( new string[]{"int"}, CharClass.Keyword );
				doc.Highlighter = h;

				doc.Text = @"int x";
				h.Highlight( doc );
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(0) );
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(1) );
				TestUtl.AssertEquals( CharClass.Keyword, doc.GetCharClass(2) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(3) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(4) );

				h.HookProc = delegate( Document d, string token, int index, CharClass klass ) {
					return (token == "int");
				};
				doc.Text = @"int x";
				h.Highlight( doc );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(0) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(1) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(2) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(3) );
				TestUtl.AssertEquals( CharClass.Normal, doc.GetCharClass(4) );
			}
		}
	}
}
#endif
