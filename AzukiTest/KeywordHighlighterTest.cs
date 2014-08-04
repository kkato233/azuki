using System;
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
	using Highlighter;

	[TestClass]
	public class KeywordHighlighterTest
	{
		[TestMethod]
		public void LineComment()
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
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<14; i++ )
				Assert.AreEqual( CharClass.Comment, doc.GetCharClass(i) );
			for( ; i<16; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<23; i++ )
				Assert.AreEqual( CharClass.DocComment, doc.GetCharClass(i) );
			for( ; i<27; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<31; i++ )
				Assert.AreEqual( CharClass.Comment, doc.GetCharClass(i) );
			for( ; i<33; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<37; i++ )
				Assert.AreEqual( CharClass.Comment, doc.GetCharClass(i) );
		}

		[TestMethod]
		public void Enclosure()
		{
			Document doc = new Document();
			KeywordHighlighter h = new KeywordHighlighter();
			h.AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );
			h.AddEnclosure( "/*", "*/", CharClass.Comment, true );
			doc.Highlighter = h;
			//---------------------------------------------

			doc.Text = @"""";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(0) );

			doc.Text = @"a""";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Normal, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(1) );

			doc.Text = @"a""b";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Normal, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(2) );

			doc.Text = @"a""b""";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Normal, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(2) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(3) );

			doc.Text = @"a""b""c";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Normal, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(2) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(3) );
			Assert.AreEqual( CharClass.Normal, doc.GetCharClass(4) );

			doc.Text = @"a""b\""c";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Normal, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(2) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(3) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(4) );
			Assert.AreEqual( CharClass.String, doc.GetCharClass(5) );

			doc.Text = @"/";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Normal, doc.GetCharClass(0) );

			doc.Text = @"/*";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(1) );

			doc.Text = @"/**";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(2) );

			doc.Text = @"/**a";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(2) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(3) );

			doc.Text = @"/*a*";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(2) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(3) );

			doc.Text = @"/**/";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(2) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(3) );

			doc.Text = @"/*a*/";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(2) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(3) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(4) );

			doc.Text = @"a/*a*/a";
			h.Highlight( doc );
			Assert.AreEqual( CharClass.Normal, doc.GetCharClass(0) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(1) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(2) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(3) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(4) );
			Assert.AreEqual( CharClass.Comment, doc.GetCharClass(5) );
			Assert.AreEqual( CharClass.Normal, doc.GetCharClass(6) );
		}

		[TestMethod]
		public void Keywords()
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
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( " ", 3, 3 );
			h.Highlight( doc );
			Assert.AreEqual( "int ", doc.Text );
			for( i=0; i<3; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			for( ; i<4; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 4 );
			} );

			// "int" --> "-int"
			doc.Text = "int";
			h.Highlight( doc );
			for( i=0; i<3; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( "-", 0, 0 );
			h.Highlight( doc );
			Assert.AreEqual( "-int", doc.Text );
			for( i=0; i<1; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<4; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 4 );
			} );

			// in --> int
			doc.Text = "in";
			h.Highlight( doc );
			for( i=0; i<2; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( "t", 2, 2 );
			h.Highlight( doc );
			Assert.AreEqual( "int", doc.Text );
			for( i=0; i<3; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 3 );
			} );

			// it --> int
			doc.Text = "it";
			h.Highlight( doc );
			for( i=0; i<2; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( "n", 1, 1 );
			h.Highlight( doc );
			Assert.AreEqual( "int", doc.Text );
			for( i=0; i<3; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 3 );
			} );

			// nt --> int
			doc.Text = "nt";
			h.Highlight( doc );
			for( i=0; i<2; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( "i", 0, 0 );
			h.Highlight( doc );
			Assert.AreEqual( "int", doc.Text );
			for( i=0; i<3; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 3 );
			} );

			// "insert at" --> int
			doc.Text = "insert at";
			h.Highlight( doc );
			for( i=0; i<9; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( "n", 1, 8 );
			h.Highlight( doc );
			Assert.AreEqual( "int", doc.Text );
			for( i=0; i<3; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 3 );
			} );

			// "hoge" --> "h int e"
			doc.Text = "hoge";
			h.Highlight( doc );
			for( i=0; i<4; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			doc.Replace( " int ", 1, 3 );
			h.Highlight( doc );
			Assert.AreEqual( "h int e", doc.Text );
			for( i=0; i<2; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			for( ; i<5; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			for( ; i<7; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 7 );
			} );

			// "int" --> "if!"
			doc.Text = "int";
			h.Highlight( doc );
			for( i=0; i<3; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( "f!", 1, 3 );
			h.Highlight( doc );
			Assert.AreEqual( "if!", doc.Text );
			for( i=0; i<2; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			for( ; i<3; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 3 );
			} );

			// "int" --> "inte"
			doc.Text = "int";
			h.Highlight( doc );
			for( i=0; i<3; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( "e", 3, 3 );
			h.Highlight( doc );
			Assert.AreEqual( "inte", doc.Text );
			for( i=0; i<4; i++ )
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 4 );
			} );

			// "int" --> "interface"
			doc.Text = "int";
			h.Highlight( doc );
			for( i=0; i<3; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			doc.Replace( "erface", 3, 3 );
			h.Highlight( doc );
			Assert.AreEqual( "interface", doc.Text );
			for( i=0; i<9; i++ )
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(i) );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				doc.GetCharClass( 10 );
			} );
		}

		[TestMethod]
		public void WordChar()
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
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(0) );		// S<--
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(5) );		// SELECT<--
				Assert.AreEqual( CharClass.Normal,  doc.GetCharClass(6) );		// SELECT <--
				Assert.AreEqual( CharClass.Normal,  doc.GetCharClass(7) );		// SELECT A<--
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(11) );	// SELECT ABC-S<--
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(16) );	// SELECT ABC-SELECT<--
				Assert.AreEqual( CharClass.Normal,  doc.GetCharClass(17) );	// SELECT ABC-SELECT <--
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(23) );	// SELECT ABC-SELECT SELECT<--
				Assert.AreEqual( CharClass.Normal,  doc.GetCharClass(26) );	// SELECT ABC-SELECT SELECT-ABC<--
			}

			h = new KeywordHighlighter();
			{
				h.AddKeywordSet( new string[]{"SELECT"}, CharClass.Keyword );
				h.WordCharSet = "-ABCDEFGHIJKLMNOPQRSTUVWXYZ";
				doc.Highlighter = h;

				doc.Text = @"SELECT ABC-SELECT SELECT-ABC";
				h.Highlight( doc );
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(0) );	// S<--
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(5) );	// SELECT<--
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(6) );	// SELECT <--
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(7) );	// SELECT A<--
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(11) );	// SELECT ABC-S<--
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(16) );	// SELECT ABC-SELECT<--
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(17) );	// SELECT ABC-SELECT <--
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(23) );	// SELECT ABC-SELECT SELECT<--
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(26) );	// SELECT ABC-SELECT SELECT-ABC<--
			}
		}

		[TestMethod]
		public void Hook()
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
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(0) );
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(1) );
				Assert.AreEqual( CharClass.Keyword, doc.GetCharClass(2) );
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(3) );
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(4) );

				h.HookProc = delegate( Document d, string token, int index, CharClass klass ) {
					return (token == "int");
				};
				doc.Text = @"int x";
				h.Highlight( doc );
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(0) );
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(1) );
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(2) );
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(3) );
				Assert.AreEqual( CharClass.Normal, doc.GetCharClass(4) );
			}
		}
	}
}
