#if TEST
using System;
using Sgry.Azuki.WinForms;

namespace Sgry.Azuki.Test
{
	class CaretMoveLogicTest
	{
		static AzukiControl _Azuki;

		public static void Test()
		{
			using( _Azuki = new AzukiControl() )
			{
				int test_num = 0;
				Console.WriteLine( "[Test for CaretMoveLogic]" );

				Console.WriteLine( "test {0} - Right", ++test_num );
				TestUtl.Do( Test_Right );

				Console.WriteLine( "test {0} - Left", ++test_num );
				TestUtl.Do( Test_Left );

				Console.WriteLine( "test {0} - NextWord", ++test_num );
				TestUtl.Do( Test_NextWord );

				Console.WriteLine( "test {0} - PrevWord", ++test_num );
				TestUtl.Do( Test_PrevWord );

				Console.WriteLine( "done." );
				Console.WriteLine();
			}
		}

		static void Test_Right()
		{
			// EOL
			_Azuki.Text = "a\rb\nc\r\nd";
			_Azuki.SetSelection( 0, 0 );
			TestUtl.AssertEquals( 1, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 1, 1 );
			TestUtl.AssertEquals( 2, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 2, 2 );
			TestUtl.AssertEquals( 3, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 3, 3 );
			TestUtl.AssertEquals( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 4, 4 );
			TestUtl.AssertEquals( 5, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 5, 5 );
			TestUtl.AssertEquals( 7, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 7, 7 );
			TestUtl.AssertEquals( 8, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 8, 8 );
			TestUtl.AssertEquals( 8, CaretMoveLogic.Calc_Right(_Azuki.View) );

			// surrogate pair
			_Azuki.Text = "_\xd85a\xdd51_";
			_Azuki.SetSelection( 0, 0 );
			TestUtl.AssertEquals( 1, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 1, 1 );
			TestUtl.AssertEquals( 3, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 3, 3 );
			TestUtl.AssertEquals( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 4, 4 );
			TestUtl.AssertEquals( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );

			// combined character sequence
			_Azuki.Text = "_a\x0300_";
			_Azuki.SetSelection( 0, 0 );
			TestUtl.AssertEquals( 1, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 1, 1 );
			TestUtl.AssertEquals( 3, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 3, 3 );
			TestUtl.AssertEquals( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.SetSelection( 4, 4 );
			TestUtl.AssertEquals( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );
		}

		static void Test_Left()
		{
			// EOL
			_Azuki.Text = "a\rb\nc\r\nd";
			_Azuki.SetSelection( 8, 8 );
			TestUtl.AssertEquals( 7, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 7, 7 );
			TestUtl.AssertEquals( 5, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 5, 5 );
			TestUtl.AssertEquals( 4, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 4, 4 );
			TestUtl.AssertEquals( 3, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 3, 3 );
			TestUtl.AssertEquals( 2, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 2, 2 );
			TestUtl.AssertEquals( 1, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 1, 1 );
			TestUtl.AssertEquals( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 0, 0 );
			TestUtl.AssertEquals( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );

			// surrogate pair
			_Azuki.Text = "a\xd85a\xdd51b";
			_Azuki.SetSelection( 4, 4 );
			TestUtl.AssertEquals( 3, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 3, 3 );
			TestUtl.AssertEquals( 1, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 1, 1 );
			TestUtl.AssertEquals( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 0, 0 );
			TestUtl.AssertEquals( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );

			// combined character sequence
			_Azuki.Text = "_a\x0300_";
			_Azuki.SetSelection( 4, 4 );
			TestUtl.AssertEquals( 3, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 3, 3 );
			TestUtl.AssertEquals( 1, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 1, 1 );
			TestUtl.AssertEquals( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.SetSelection( 0, 0 );
			TestUtl.AssertEquals( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );
		}

		static void Test_NextWord()
		{
			string[][] samples = new string[][] {
				new string[]{"aaaa",           "aa11",           "aa,,",           "aa\x3042\x3042",           "aa\x30a2\x30a2",           "aa\x963f\x963f",           "aa\n\n",           "aa  "          },
				new string[]{"11aa",           "1111",           "11,,",           "11\x3042\x3042",           "11\x30a2\x30a2",           "11\x963f\x963f",           "11\n\n",           "11  "          },
				new string[]{",,aa",           ",,11",           ",,,,",           ",,\x3042\x3042",           ",,\x30a2\x30a2",           ",,\x963f\x963f",           ",,\n\n",           ",,  "          },
				new string[]{"\x3042\x3042aa", "\x3042\x304211", "\x3042\x3042,,", "\x3042\x3042\x3042\x3042", "\x3042\x3042\x30a2\x30a2", "\x3042\x3042\x963f\x963f", "\x3042\x3042\n\n", "\x3042\x3042  "},
				new string[]{"\x30a2\x30a2aa", "\x30a2\x30a211", "\x30a2\x30a2,,", "\x30a2\x30a2\x3042\x3042", "\x30a2\x30a2\x30a2\x30a2", "\x30a2\x30a2\x963f\x963f", "\x30a2\x30a2\n\n", "\x30a2\x30a2  "},
				new string[]{"\x963f\x963faa", "\x963f\x963f11", "\x963f\x963f,,", "\x963f\x963f\x3042\x3042", "\x963f\x963f\x30a2\x30a2", "\x963f\x963f\x963f\x963f", "\x963f\x963f\n\n", "\x963f\x963f  "},
				new string[]{"\n\naa",         "\n\n11",         "\n\n,,",         "\n\n\x3042\x3042",         "\n\n\x30a2\x30a2",         "\n\n\x963f\x963f",         "\n\n\n\n",         "\n\n  "        },
				new string[]{"  aa",           "  11",           "  ,,",           "  \x3042\x3042",           "  \x30a2\x30a2",           "  \x963f\x963f",           "  \n\n",           "    "          }
			};

			// NextWord
			int[] s = new int[]{4,4,4,4,4}; // same class
			int[] d = new int[]{2,2,4,4,4}; // different class
			int[] l = new int[]{2,2,3,4,4}; // latter half is \n\n
			int[] f = new int[]{1,2,4,4,4}; // former half is \n\n
			int[] b = new int[]{1,2,3,4,4}; // both half are \n\n
			int[] w = new int[]{4,4,4,4,4}; // latter half is whitespace
			int[][][] expected = new int[][][] {
				new int[][]{ s, d, d, d, d, d, l, w },
				new int[][]{ d, s, d, d, d, d, l, w },
				new int[][]{ d, d, s, d, d, d, l, w },
				new int[][]{ d, d, d, s, d, d, l, w },
				new int[][]{ d, d, d, d, s, d, l, w },
				new int[][]{ d, d, d, d, d, s, l, w },
				new int[][]{ f, f, f, f, f, f, b, f },
				new int[][]{ d, d, d, d, d, d, l, s }
			};

			for( int x=0; x<samples.Length; x++ )
			{
				for( int y=0; y<samples[x].Length; y++ )
				{
					_Azuki.Text = samples[x][y];
					for( int i=0; i<_Azuki.TextLength; i++ )
					{
						try
						{
							_Azuki.SetSelection( i, i );
							int actual = CaretMoveLogic.Calc_NextWord( _Azuki.View );
							TestUtl.AssertEquals( expected[x][y][i], actual );
						}
						catch( AssertException ex )
						{
							Console.Error.WriteLine( "### x={0}, y={1}, i={2}, Azuki.Text=[{3}]", x, y, i, _Azuki.Text );
							throw ex;
						}
					}
				}
			}
		}

		static void Test_PrevWord()
		{
			string[][] samples = new string[][] {
				new string[]{"aaaa",           "aa11",           "aa,,",           "aa\x3042\x3042",           "aa\x30a2\x30a2",           "aa\x963f\x963f",           "aa\n\n",           "aa  "          },
				new string[]{"11aa",           "1111",           "11,,",           "11\x3042\x3042",           "11\x30a2\x30a2",           "11\x963f\x963f",           "11\n\n",           "11  "          },
				new string[]{",,aa",           ",,11",           ",,,,",           ",,\x3042\x3042",           ",,\x30a2\x30a2",           ",,\x963f\x963f",           ",,\n\n",           ",,  "          },
				new string[]{"\x3042\x3042aa", "\x3042\x304211", "\x3042\x3042,,", "\x3042\x3042\x3042\x3042", "\x3042\x3042\x30a2\x30a2", "\x3042\x3042\x963f\x963f", "\x3042\x3042\n\n", "\x3042\x3042  "},
				new string[]{"\x30a2\x30a2aa", "\x30a2\x30a211", "\x30a2\x30a2,,", "\x30a2\x30a2\x3042\x3042", "\x30a2\x30a2\x30a2\x30a2", "\x30a2\x30a2\x963f\x963f", "\x30a2\x30a2\n\n", "\x30a2\x30a2  "},
				new string[]{"\x963f\x963faa", "\x963f\x963f11", "\x963f\x963f,,", "\x963f\x963f\x3042\x3042", "\x963f\x963f\x30a2\x30a2", "\x963f\x963f\x963f\x963f", "\x963f\x963f\n\n", "\x963f\x963f  "},
				new string[]{"\n\naa",         "\n\n11",         "\n\n,,",         "\n\n\x3042\x3042",         "\n\n\x30a2\x30a2",         "\n\n\x963f\x963f",         "\n\n\n\n",         "\n\n  "        },
				new string[]{"  aa",           "  11",           "  ,,",           "  \x3042\x3042",           "  \x30a2\x30a2",           "  \x963f\x963f",           "  \n\n",           "    "          }
			};

			int[] s = new int[]{0,0,0,0,0}; // same class
			int[] d = new int[]{0,0,0,2,2}; // different class
			int[] l = new int[]{0,0,0,2,3}; // latter half is \n\n
			int[] f = new int[]{0,0,1,2,2}; // former half is \n\n
			int[] b = new int[]{0,0,1,2,3}; // both half are \n\n
			int[] w = new int[]{0,0,0,0,0}; // latter half is whitespace
			int[][][] expected = new int[][][] {
				new int[][]{ s, d, d, d, d, d, l, w },
				new int[][]{ d, s, d, d, d, d, l, w },
				new int[][]{ d, d, s, d, d, d, l, w },
				new int[][]{ d, d, d, s, d, d, l, w },
				new int[][]{ d, d, d, d, s, d, l, w },
				new int[][]{ d, d, d, d, d, s, l, w },
				new int[][]{ f, f, f, f, f, f, b, f },
				new int[][]{ d, d, d, d, d, d, l, s }
			};

			for( int x=0; x<samples.Length; x++ )
			{
				for( int y=0; y<samples[x].Length; y++ )
				{
					_Azuki.Text = samples[x][y];
					for( int i=0; i<_Azuki.TextLength; i++ )
					{
						try
						{
							_Azuki.SetSelection( i, i );
							int actual = CaretMoveLogic.Calc_PrevWord( _Azuki.View );
							TestUtl.AssertEquals( expected[x][y][i], actual );
						}
						catch( AssertException ex )
						{
							Console.Error.WriteLine( "### x={0}, y={1}, i={2}, Azuki.Text=[{3}]", x, y, i, _Azuki.Text );
							throw ex;
						}
					}
				}
			}

			// EOL code
			_Azuki.Text = "a\r";
			_Azuki.SetSelection( 2, 2 );
			TestUtl.AssertEquals( 1, CaretMoveLogic.Calc_PrevWord(_Azuki.View) );

			// EOL code
			_Azuki.Text = "a\r\n";
			_Azuki.SetSelection( 3, 3 );
			TestUtl.AssertEquals( 1, CaretMoveLogic.Calc_PrevWord(_Azuki.View) );
		}
	}
}
#endif
