#if TEST
using System;

namespace Sgry.Azuki.Test
{
	class DefaultWordProcTest
	{
		public static void Test()
		{
			int test_num = 0;
			Console.WriteLine( "[Test for DefaultWordProc]" );

			Console.WriteLine( "test {0} - WordDetection", ++test_num );
			TestUtl.Do( Test_WordDetection );

			Console.WriteLine( "test {0} - Kinsoku shori", ++test_num );
			TestUtl.Do( Test_Kinsoku );

			Console.WriteLine( "test {0} - Kinsoku shori (special cases)", ++test_num );
			TestUtl.Do( Test_KinsokuSpecial );

			Console.WriteLine( "done." );
			Console.WriteLine();
		}

		static void Test_WordDetection()
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
							TestUtl.AssertEquals( expectedForSameClass[i], actual );
						else
							TestUtl.AssertEquals( expected[i], actual );
					}
				}
			}
			TestUtl.AssertEquals( doc.Length, wordProc.NextWordStart(doc, doc.Length) );

			try{ wordProc.NextWordStart(null, 0); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentNullException>(ex); }

			try{ wordProc.NextWordStart(doc, -1); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

			try{ wordProc.NextWordStart(doc, doc.Length+1); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

			// NextWordEnd
			expected = new int[]{ 2, 2, 4, 4, 4 };
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
							TestUtl.AssertEquals( expectedForSameClass[i], actual );
						else
							TestUtl.AssertEquals( expected[i], actual );
					}
				}
			}
			TestUtl.AssertEquals( doc.Length, wordProc.NextWordEnd(doc, doc.Length) );

			try{ wordProc.NextWordEnd(null, 0); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentNullException>(ex); }

			try{ wordProc.NextWordEnd(doc, -1); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

			try{ wordProc.NextWordEnd(doc, doc.Length+1); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
							TestUtl.AssertEquals( expectedForSameClass[i], actual );
						else
							TestUtl.AssertEquals( expected[i], actual );
					}
				}
			}
			TestUtl.AssertEquals( doc.Length, wordProc.PrevWordStart(doc, doc.Length) );

			try{ wordProc.PrevWordStart(null, 0); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentNullException>(ex); }

			try{ wordProc.PrevWordStart(doc, -1); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

			try{ wordProc.PrevWordStart(doc, doc.Length+1); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

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
							TestUtl.AssertEquals( expectedForSameClass[i], actual );
						else
							TestUtl.AssertEquals( expected[i], actual );
					}
				}
			}
			TestUtl.AssertEquals( doc.Length, wordProc.PrevWordEnd(doc, doc.Length) );

			try{ wordProc.PrevWordEnd(null, 0); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentNullException>(ex); }

			try{ wordProc.PrevWordEnd(doc, -1); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }

			try{ wordProc.PrevWordEnd(doc, doc.Length+1); throw new Exception(); }
			catch( Exception ex ){ TestUtl.AssertType<ArgumentOutOfRangeException>(ex); }
		}

		static void Test_Kinsoku()
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
			TestUtl.AssertEquals( 6, wordProc.HandleWordWrapping(doc, 6) );

			// restrict characters to start lines
			wordProc.EnableLineHeadRestriction = true;
			TestUtl.AssertEquals( 4, wordProc.HandleWordWrapping(doc, 6) );

			// enable word wrap
			wordProc.EnableWordWrap = true;
			TestUtl.AssertEquals( 3, wordProc.HandleWordWrapping(doc, 6) );

			// restrict characters to end lines
			wordProc.EnableLineEndRestriction = true;
			TestUtl.AssertEquals( 1, wordProc.HandleWordWrapping(doc, 6) );

			// change the phrase in the parentheses from a word to non-word
			doc.Replace( "**", 3, 5 );
			TestUtl.AssertEquals( 4, wordProc.HandleWordWrapping(doc, 6) );

			// hang specified characters on the end of line
			wordProc.EnableCharacterHanging = true;
			wordProc.CharsToBeHanged = (new String(wordProc.CharsToBeHanged) + ']').ToCharArray();
			TestUtl.AssertEquals( 7, wordProc.HandleWordWrapping(doc, 6) );

			// hang EOL code (CR)
			wordProc.EnableEolHanging = false;
			TestUtl.AssertEquals( 8, wordProc.HandleWordWrapping(doc, 8) );
			wordProc.EnableEolHanging = true;
			TestUtl.AssertEquals( 9, wordProc.HandleWordWrapping(doc, 8) );

			// hang EOL code (LF)
			wordProc.EnableEolHanging = false;
			TestUtl.AssertEquals( 10, wordProc.HandleWordWrapping(doc, 10) );
			wordProc.EnableEolHanging = true;
			TestUtl.AssertEquals( 11, wordProc.HandleWordWrapping(doc, 10) );

			// hang EOL code (CR+LF)
			wordProc.EnableEolHanging = false;
			TestUtl.AssertEquals( 12, wordProc.HandleWordWrapping(doc, 12) );
			wordProc.EnableEolHanging = true;
			TestUtl.AssertEquals( 14, wordProc.HandleWordWrapping(doc, 12) );
			TestUtl.AssertEquals( 14, wordProc.HandleWordWrapping(doc, 13) );

			// hang two continuing EOL codes (CR+CR)
			wordProc.EnableEolHanging = false;
			TestUtl.AssertEquals( 15, wordProc.HandleWordWrapping(doc, 15) );
			wordProc.EnableEolHanging = true;
			TestUtl.AssertEquals( 16, wordProc.HandleWordWrapping(doc, 15) );

			// bonus test
			TestUtl.AssertEquals( 17, wordProc.HandleWordWrapping(doc, 16) );
			TestUtl.AssertEquals( 17, wordProc.HandleWordWrapping(doc, 17) );
		}

		static void Test_KinsokuSpecial()
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
				TestUtl.AssertEquals( 1, wordProc.HandleWordWrapping(doc, 0) );
				doc.Text = "\r";
				TestUtl.AssertEquals( 1, wordProc.HandleWordWrapping(doc, 0) );
				doc.Text = "\r\n";
				TestUtl.AssertEquals( 2, wordProc.HandleWordWrapping(doc, 0) );
				doc.Text = "\r\n";
				TestUtl.AssertEquals( 2, wordProc.HandleWordWrapping(doc, 1) );
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
				TestUtl.AssertEquals( 2, wordProc.HandleWordWrapping(doc, 2) );

				// hit the limit of the content length
				TestUtl.AssertEquals( 8, wordProc.HandleWordWrapping(doc, 8) );
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
				TestUtl.AssertEquals( 8, wordProc.HandleWordWrapping(doc, 8) );

				// hit the limit of the content length
				TestUtl.AssertEquals( 2, wordProc.HandleWordWrapping(doc, 2) );
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
				TestUtl.AssertEquals( 8, wordProc.HandleWordWrapping(doc, 8) );

				// hit the limit of the content length
				TestUtl.AssertEquals( 2, wordProc.HandleWordWrapping(doc, 2) );
			}

			// complex pattern
			wordProc.EnableWordWrap = false;
			wordProc.EnableLineEndRestriction = true;
			wordProc.EnableLineHeadRestriction = true;
			wordProc.EnableCharacterHanging = true;
			wordProc.EnableEolHanging = true;
			doc.Text = "a.)";
			TestUtl.AssertEquals( 2, wordProc.HandleWordWrapping(doc, 1) );
			doc.Text = "aa.)";
			TestUtl.AssertEquals( 1, wordProc.HandleWordWrapping(doc, 2) );
			wordProc.EnableWordWrap = true;
			doc.Text = "aa.)";
			TestUtl.AssertEquals( 0, wordProc.HandleWordWrapping(doc, 2) );

			// once failed pattern
			doc.Text = "(\r\n)";
			wordProc.EnableEolHanging = false;
			TestUtl.AssertEquals( 1, wordProc.HandleWordWrapping(doc, 1) );
			wordProc.EnableEolHanging = true;
			TestUtl.AssertEquals( 3, wordProc.HandleWordWrapping(doc, 1) );
		}
	}
}
#endif
