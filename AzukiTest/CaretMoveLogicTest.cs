using System;
using Sgry.Azuki.WinForms;
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
	public class CaretMoveLogicTest : IDisposable
	{
		AzukiControl _Azuki;

		public CaretMoveLogicTest()
		{
			_Azuki = new AzukiControl();
		}

		public void Dispose()
		{
			_Azuki.Dispose();
		}

		[TestMethod]
		public void Right()
		{
			var view = _Azuki.View as IViewInternal;

			// EOL
			_Azuki.Text = "a\rb\nc\r\nd";
			_Azuki.SetSelection( 0, 0 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 1, 1 );
			Assert.AreEqual( 2, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 2, 2 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 3, 3 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 4, 4 );
			Assert.AreEqual( 5, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 5, 5 );
			Assert.AreEqual( 7, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 7, 7 );
			Assert.AreEqual( 8, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 8, 8 );
			Assert.AreEqual( 8, CaretMoveLogic.Calc_Right(view) );

			// surrogate pair
			_Azuki.Text = "_\xd85a\xdd51_";
			_Azuki.SetSelection( 0, 0 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 1, 1 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 3, 3 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 4, 4 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(view) );

			// combined character sequence
			_Azuki.Text = "_a\x0300_";
			_Azuki.SetSelection( 0, 0 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 1, 1 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 3, 3 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(view) );
			_Azuki.SetSelection( 4, 4 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(view) );
		}

		[TestMethod]
		public void Left()
		{
			var view = _Azuki.View as IViewInternal;

			// EOL
			_Azuki.Text = "a\rb\nc\r\nd";
			_Azuki.SetSelection( 8, 8 );
			Assert.AreEqual( 7, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 7, 7 );
			Assert.AreEqual( 5, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 5, 5 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 4, 4 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 3, 3 );
			Assert.AreEqual( 2, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 2, 2 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 1, 1 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 0, 0 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(view) );

			// surrogate pair
			_Azuki.Text = "a\xd85a\xdd51b";
			_Azuki.SetSelection( 4, 4 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 3, 3 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 1, 1 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 0, 0 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(view) );

			// combined character sequence
			_Azuki.Text = "_a\x0300_";
			_Azuki.SetSelection( 4, 4 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 3, 3 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 1, 1 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(view) );
			_Azuki.SetSelection( 0, 0 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(view) );
		}

		[TestMethod]
		public void NextWord()
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
			var view = _Azuki.View as IViewInternal;

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
							int actual = CaretMoveLogic.Calc_NextWord( view );
							Assert.AreEqual( expected[x][y][i], actual );
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

		[TestMethod]
		public void PrevWord()
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
			var view = _Azuki.View as IViewInternal;

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
							int actual = CaretMoveLogic.Calc_PrevWord( view );
							Assert.AreEqual( expected[x][y][i], actual );
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
			Assert.AreEqual( 1, CaretMoveLogic.Calc_PrevWord(view) );

			// EOL code
			_Azuki.Text = "a\r\n";
			_Azuki.SetSelection( 3, 3 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_PrevWord(view) );
		}
	}
}
