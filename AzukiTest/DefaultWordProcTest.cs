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
	[TestClass]
	public class DefaultWordProcTest
	{
		[TestMethod]
		public void WordDetection()
		{
			Document doc = new Document();
			DefaultWordProc wordProc = new DefaultWordProc();
			string[] samples = new string[]{ "aa", "11", ",,", "\x3042\x3042", "\x963f\x963f", "\n\n", "  " };
			int[] expected;
			int[] expectedForSameClass;

			// NextWordStart
			expected = new int[]{ 0, 2, 2, 4, 4 };
			expectedForSameClass = new int[]{ 0, 4, 4, 4, 4 };
			for( int left=0; left<samples.Length; left++ )
			{
				for( int right=0; right<samples.Length; right++ )
				{
					doc.Text = samples[left] + samples[right];
					for( int i=0; i<doc.Length; i++ )
					{
						int actual = wordProc.NextWordStart( doc, i );
						if( left == right )
							Assert.AreEqual( expectedForSameClass[i], actual );
						else
							Assert.AreEqual( expected[i], actual );
					}
				}
			}
			Assert.AreEqual( doc.Length, wordProc.NextWordStart(doc, doc.Length) );

			// combining character
			doc.Text = ".a\x0300.";
			Assert.AreEqual( 1, wordProc.NextWordStart(doc, 1) );
			Assert.AreEqual( 3, wordProc.NextWordStart(doc, 2) );
			Assert.AreEqual( 3, wordProc.NextWordStart(doc, 3) );

			// combining character
			doc.Text = "aa\x0300a";
			Assert.AreEqual( 4, wordProc.NextWordStart(doc, 1) );
			Assert.AreEqual( 4, wordProc.NextWordStart(doc, 2) );
			Assert.AreEqual( 4, wordProc.NextWordStart(doc, 3) );
			MyAssert.Throws<ArgumentNullException>( delegate{
				wordProc.NextWordStart( null, 0 );
			} );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				wordProc.NextWordStart( doc, -1 );
			} );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				wordProc.NextWordStart( doc, doc.Length+1 );
			} );

			// NextWordEnd
			expected = new int[]{ 2, 2, 2, 4, 4 };
			expectedForSameClass = new int[]{ 4, 4, 4, 4, 4 };
			for( int left=0; left<samples.Length; left++ )
			{
				for( int right=0; right<samples.Length; right++ )
				{
					doc.Text = samples[left] + samples[right];
					for( int i=0; i<doc.Length; i++ )
					{
						int actual = wordProc.NextWordEnd( doc, i );
						if( left == right )
							Assert.AreEqual( expectedForSameClass[i], actual );
						else
							Assert.AreEqual( expected[i], actual );
					}
				}
			}
			Assert.AreEqual( doc.Length, wordProc.NextWordEnd(doc, doc.Length) );

			// combining character
			doc.Text = ".a\x0300.";
			Assert.AreEqual( 1, wordProc.NextWordEnd(doc, 1) );
			Assert.AreEqual( 3, wordProc.NextWordEnd(doc, 2) );
			Assert.AreEqual( 3, wordProc.NextWordEnd(doc, 3) );

			// combining character
			doc.Text = "aa\x0300a";
			Assert.AreEqual( 4, wordProc.NextWordEnd(doc, 1) );
			Assert.AreEqual( 4, wordProc.NextWordEnd(doc, 2) );
			Assert.AreEqual( 4, wordProc.NextWordEnd(doc, 3) );
			MyAssert.Throws<ArgumentNullException>( delegate{
				wordProc.NextWordEnd( null, 0 );
			} );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				wordProc.NextWordEnd( doc, -1 );
			} );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				wordProc.NextWordEnd( doc, doc.Length+1 );
			} );

			// PrevWordStart
			expected = new int[]{ 0, 0, 2, 2, 4 };
			expectedForSameClass = new int[]{ 0, 0, 0, 0, 4 };
			for( int left=0; left<samples.Length; left++ )
			{
				for( int right=0; right<samples.Length; right++ )
				{
					doc.Text = samples[left] + samples[right];
					for( int i=0; i<doc.Length; i++ )
					{
						int actual = wordProc.PrevWordStart( doc, i );
						if( left == right )
							Assert.AreEqual( expectedForSameClass[i], actual );
						else
							Assert.AreEqual( expected[i], actual );
					}
				}
			}
			Assert.AreEqual( doc.Length, wordProc.PrevWordStart(doc, doc.Length) );

			// combining character
			doc.Text = ".a\x0300.";
			Assert.AreEqual( 1, wordProc.PrevWordStart(doc, 1) );
			Assert.AreEqual( 1, wordProc.PrevWordStart(doc, 2) );
			Assert.AreEqual( 3, wordProc.PrevWordStart(doc, 3) );

			// combining character
			doc.Text = "aa\x0300a";
			Assert.AreEqual( 0, wordProc.PrevWordStart(doc, 1) );
			Assert.AreEqual( 0, wordProc.PrevWordStart(doc, 2) );
			Assert.AreEqual( 0, wordProc.PrevWordStart(doc, 3) );
			MyAssert.Throws<ArgumentNullException>( delegate{
				wordProc.PrevWordStart( null, 0 );
			} );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				wordProc.PrevWordStart( doc, -1 );
			} );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				wordProc.PrevWordStart( doc, doc.Length+1 );
			} );

			// PrevWordEnd
			expected = new int[]{ 0, 0, 2, 2, 4 };
			expectedForSameClass = new int[]{ 0, 0, 0, 0, 4 };
			for( int left=0; left<samples.Length; left++ )
			{
				for( int right=0; right<samples.Length; right++ )
				{
					doc.Text = samples[left] + samples[right];
					for( int i=0; i<doc.Length; i++ )
					{
						int actual = wordProc.PrevWordEnd( doc, i );
						if( left == right )
							Assert.AreEqual( expectedForSameClass[i], actual );
						else
							Assert.AreEqual( expected[i], actual );
					}
				}
			}
			Assert.AreEqual( doc.Length, wordProc.PrevWordEnd(doc, doc.Length) );

			// combining character
			doc.Text = ".a\x0300.";
			Assert.AreEqual( 1, wordProc.PrevWordEnd(doc, 1) );
			Assert.AreEqual( 1, wordProc.PrevWordEnd(doc, 2) );
			Assert.AreEqual( 3, wordProc.PrevWordEnd(doc, 3) );

			// combining character
			doc.Text = "aa\x0300a";
			Assert.AreEqual( 0, wordProc.PrevWordEnd(doc, 1) );
			Assert.AreEqual( 0, wordProc.PrevWordEnd(doc, 2) );
			Assert.AreEqual( 0, wordProc.PrevWordEnd(doc, 3) );
			MyAssert.Throws<ArgumentNullException>( delegate{
				wordProc.PrevWordEnd( null, 0 );
			} );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				wordProc.PrevWordEnd( doc, -1 );
			} );
			MyAssert.Throws<ArgumentOutOfRangeException>( delegate{
				wordProc.PrevWordEnd( doc, doc.Length+1 );
			} );
		}

		[TestMethod]
		public void Kinsoku()
		{
			Document doc = new Document();
			DefaultWordProc wordProc = new DefaultWordProc();
			doc.Text = "a[(bc)]d\re\nf\r\ng\r\r";

			// no Kinsoku
			wordProc.EnableWordWrap = false;
			wordProc.EnableLineEndRestriction = false;
			wordProc.EnableLineHeadRestriction = false;
			wordProc.EnableCharacterHanging = false;
			wordProc.EnableEolHanging = false;
			Assert.AreEqual( 6, wordProc.HandleWordWrapping(doc, 6) );

			// restrict characters to start lines
			wordProc.EnableLineHeadRestriction = true;
			Assert.AreEqual( 4, wordProc.HandleWordWrapping(doc, 6) );

			// enable word wrap
			wordProc.EnableWordWrap = true;
			Assert.AreEqual( 3, wordProc.HandleWordWrapping(doc, 6) );

			// restrict characters to end lines
			wordProc.EnableLineEndRestriction = true;
			Assert.AreEqual( 1, wordProc.HandleWordWrapping(doc, 6) );

			// change the phrase in the parentheses from a word to non-word
			doc.Replace( "**", 3, 5 );
			Assert.AreEqual( 4, wordProc.HandleWordWrapping(doc, 6) );

			// hang specified characters on the end of line
			wordProc.EnableCharacterHanging = true;
			wordProc.CharsToBeHanged = (new String(wordProc.CharsToBeHanged) + ']').ToCharArray();
			Assert.AreEqual( 7, wordProc.HandleWordWrapping(doc, 6) );

			// hang EOL code (CR)
			wordProc.EnableEolHanging = false;
			Assert.AreEqual( 8, wordProc.HandleWordWrapping(doc, 8) );
			wordProc.EnableEolHanging = true;
			Assert.AreEqual( 9, wordProc.HandleWordWrapping(doc, 8) );

			// hang EOL code (LF)
			wordProc.EnableEolHanging = false;
			Assert.AreEqual( 10, wordProc.HandleWordWrapping(doc, 10) );
			wordProc.EnableEolHanging = true;
			Assert.AreEqual( 11, wordProc.HandleWordWrapping(doc, 10) );

			// hang EOL code (CR+LF)
			wordProc.EnableEolHanging = false;
			Assert.AreEqual( 12, wordProc.HandleWordWrapping(doc, 12) );
			wordProc.EnableEolHanging = true;
			Assert.AreEqual( 14, wordProc.HandleWordWrapping(doc, 12) );
			Assert.AreEqual( 14, wordProc.HandleWordWrapping(doc, 13) );

			// hang two continuing EOL codes (CR+CR)
			wordProc.EnableEolHanging = false;
			Assert.AreEqual( 15, wordProc.HandleWordWrapping(doc, 15) );
			wordProc.EnableEolHanging = true;
			Assert.AreEqual( 16, wordProc.HandleWordWrapping(doc, 15) );

			// bonus test
			Assert.AreEqual( 17, wordProc.HandleWordWrapping(doc, 16) );
			Assert.AreEqual( 17, wordProc.HandleWordWrapping(doc, 17) );
		}

		[TestMethod]
		public void KinsokuSpecial()
		{
			Document doc = new Document();
			DefaultWordProc wordProc = new DefaultWordProc();

			// limit of EOL hanging
			wordProc.EnableWordWrap = false;
			wordProc.EnableLineEndRestriction = false;
			wordProc.EnableLineHeadRestriction = false;
			wordProc.EnableCharacterHanging = false;
			wordProc.EnableEolHanging = true;
			{
				doc.Text = "\n";
				Assert.AreEqual( 1, wordProc.HandleWordWrapping(doc, 0) );
				doc.Text = "\r";
				Assert.AreEqual( 1, wordProc.HandleWordWrapping(doc, 0) );
				doc.Text = "\r\n";
				Assert.AreEqual( 2, wordProc.HandleWordWrapping(doc, 0) );
				doc.Text = "\r\n";
				Assert.AreEqual( 2, wordProc.HandleWordWrapping(doc, 1) );
			}

			// limit of char hanging
			doc.Text = "\x3002\x3002\x3002\x3002\x3002\x3002\x3002\x3002\x3002\x3002";
			wordProc.EnableWordWrap = false;
			wordProc.EnableLineEndRestriction = false;
			wordProc.EnableLineHeadRestriction = false;
			wordProc.EnableCharacterHanging = true;
			wordProc.EnableEolHanging = false;
			{
				// not applicable because of # of depth
				Assert.AreEqual( 2, wordProc.HandleWordWrapping(doc, 2) );

				// hit the limit of the content length
				Assert.AreEqual( 8, wordProc.HandleWordWrapping(doc, 8) );
			}

			// limit of line head prohibition
			doc.Text = "))))))))))";
			wordProc.EnableWordWrap = false;
			wordProc.EnableLineEndRestriction = false;
			wordProc.EnableLineHeadRestriction = true;
			wordProc.EnableCharacterHanging = false;
			wordProc.EnableEolHanging = false;
			{
				// not applicable because of # of depth
				Assert.AreEqual( 8, wordProc.HandleWordWrapping(doc, 8) );

				// hit the limit of the content length
				Assert.AreEqual( 2, wordProc.HandleWordWrapping(doc, 2) );
			}

			// limit of line end prohibition
			doc.Text = "((((((((((";
			wordProc.EnableWordWrap = false;
			wordProc.EnableLineEndRestriction = true;
			wordProc.EnableLineHeadRestriction = false;
			wordProc.EnableCharacterHanging = false;
			wordProc.EnableEolHanging = false;
			{
				// not applicable because of # of depth
				Assert.AreEqual( 8, wordProc.HandleWordWrapping(doc, 8) );

				// hit the limit of the content length
				Assert.AreEqual( 2, wordProc.HandleWordWrapping(doc, 2) );
			}

			// complex pattern
			wordProc.EnableWordWrap = false;
			wordProc.EnableLineEndRestriction = true;
			wordProc.EnableLineHeadRestriction = true;
			wordProc.EnableCharacterHanging = true;
			wordProc.EnableEolHanging = true;
			doc.Text = "a.)";
			Assert.AreEqual( 2, wordProc.HandleWordWrapping(doc, 1) );
			doc.Text = "aa.)";
			Assert.AreEqual( 1, wordProc.HandleWordWrapping(doc, 2) );
			wordProc.EnableWordWrap = true;
			doc.Text = "aa.)";
			Assert.AreEqual( 0, wordProc.HandleWordWrapping(doc, 2) );

			// once failed pattern
			doc.Text = "(\r\n)";
			wordProc.EnableEolHanging = false;
			Assert.AreEqual( 1, wordProc.HandleWordWrapping(doc, 1) );
			wordProc.EnableEolHanging = true;
			Assert.AreEqual( 3, wordProc.HandleWordWrapping(doc, 1) );
		}
	}
}
