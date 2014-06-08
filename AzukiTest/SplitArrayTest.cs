using System;
using NUnit.Framework;

namespace Sgry.Azuki.Test
{
	[TestFixture]
	public class SplitArrayTest
	{
		[Test]
		public void Init()
		{
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			Assert.AreEqual( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				Assert.Throws<AssertException>( delegate {
					chars.GetAt( x );
				} );
				Assert.Throws<AssertException>( delegate {
					chars.SetAt( '!', x );
				} );
			}
		}

		[Test]
		public void Add()
		{
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			chars.Add( 'a' );
			Assert.AreEqual( 1, chars.Count );
			Assert.AreEqual( 'a', chars.GetAt(0) );
			chars.SetAt( 'b', 0 );
			Assert.AreEqual( 'b', chars.GetAt(0) );
			Assert.Throws<AssertException>( delegate {
				chars.GetAt( 1 );
			} );
		}

		[Test]
		public void Clear()
		{
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			chars.Clear();
			Assert.AreEqual( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				Assert.Throws<AssertException>( delegate {
					chars.GetAt( x );
				} );
				Assert.Throws<AssertException>( delegate {
					chars.SetAt( '!', x );
				} );
			}

			chars.Add( "hoge".ToCharArray() );
			chars.Clear();
			Assert.AreEqual( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				Assert.Throws<AssertException>( delegate {
					chars.GetAt( x );
				} );
				Assert.Throws<AssertException>( delegate {
					chars.SetAt( '!', x );
				} );
			}
		}

		[Test]
		public void Insert_One()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );

			// control-char
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 3, '\0' );
			Assert.AreEqual( "hog\0epiyo", ToString(sary) );

			// before head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			Assert.Throws<AssertException>( delegate {
				sary.Insert( -1, 'G' );
			} );

			// head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, 'G' );
			Assert.AreEqual( 9, sary.Count );
			Assert.AreEqual( "Ghogepiyo", ToString(sary) );

			// middle
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 4, 'G' );
			Assert.AreEqual( 9, sary.Count );
			Assert.AreEqual( "hogeGpiyo", ToString(sary) );

			// end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 8, 'G' );
			Assert.AreEqual( 9, sary.Count );
			Assert.AreEqual( "hogepiyoG", ToString(sary) );

			// after end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			Assert.Throws<AssertException>( delegate {
				sary.Insert( 9, 'G' );
			} );
		}

		[Test]
		public void Insert_Array()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );

			// null array
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			Assert.Throws<AssertException>( delegate {
				sary.Insert( 0, null );
			} );

			// empty array
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, "".ToCharArray() );
			Assert.AreEqual( "hogepiyo", ToString(sary) );

			// before head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			Assert.Throws<AssertException>( delegate {
				sary.Insert( -1, "FOO".ToCharArray() );
			} );

			// head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, "FOO".ToCharArray() );
			Assert.AreEqual( 11, sary.Count );
			Assert.AreEqual( "FOOhogepiyo", ToString(sary) );

			// middle
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 4, "FOO".ToCharArray() );
			Assert.AreEqual( 11, sary.Count );
			Assert.AreEqual( "hogeFOOpiyo", ToString(sary) );

			// end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 8, "FOO".ToCharArray() );
			Assert.AreEqual( 11, sary.Count );
			Assert.AreEqual( "hogepiyoFOO", ToString(sary) );

			// after end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			Assert.Throws<AssertException>( delegate {
				sary.Insert( 9, "FOO".ToCharArray() );
			} );
		}

		[Test]
		public void Replace()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );

			// replace position
			{
				// before head
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				Assert.Throws<AssertException>( delegate {
					sary.Replace( -1, "000".ToCharArray(), 0, 2 );
				} );

				// head
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				sary.Replace( 0, "000".ToCharArray(), 0, 2 );
				Assert.AreEqual( "00gepiyo", ToString(sary) );

				// middle
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				sary.Replace( 4, "000".ToCharArray(), 0, 2 );
				Assert.AreEqual( "hoge00yo", ToString(sary) );

				// end
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				sary.Replace( 6, "000".ToCharArray(), 0, 2 );
				Assert.AreEqual( "hogepi00", ToString(sary) );

				// after end
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				Assert.Throws<AssertException>( delegate {
					sary.Replace( 7, "000".ToCharArray(), 0, 2 );
				} );
				Assert.Throws<AssertException>( delegate {
					sary.Replace( 8, "000".ToCharArray(), 0, 2 );
				} );
			}

			// value array
			{
				// giving null
				Assert.Throws<AssertException>( delegate {
					sary.Replace( 0, null, 0, 1 );
				} );

				// empty array
				sary.Replace( 0, "".ToCharArray(), 0, 0 );
				Assert.AreEqual( "hogepiyo", ToString(sary) );

				// empty range
				sary.Replace( 0, "000".ToCharArray(), 0, 0 );
				Assert.AreEqual( "hogepiyo", ToString(sary) );

				// invalid range (reversed)
				Assert.Throws<AssertException>( delegate {
					sary.Replace( 0, "000".ToCharArray(), 1, 0 );
				} );

				// invalid range (before head)
				Assert.Throws<AssertException>( delegate {
					sary.Replace( 0, "000".ToCharArray(), -1, 0 );
				} );

				// invalid range (after head)
				Assert.Throws<AssertException>( delegate {
					sary.Replace( 0, "000".ToCharArray(), 3, 4 );
				} );
			}
		}

		[Test]
		public void RemoveRange()
		{
			const string InitData = "hogepiyo";
			SplitArray<char> chars = new SplitArray<char>( 5, 8 );

			// case 2 (moving gap to buffer head)
			chars.Add( InitData.ToCharArray() );
			chars.RemoveAt( 2 );
			Assert.AreEqual( 7, chars.Count );
			Assert.AreEqual( "hoepiyo", ToString(chars) );
			Assert.Throws<AssertException>( delegate {
				chars.GetAt( 7 );
			} );
			
			// case 1 (moving gap to buffer end)
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 5, 7 );
			Assert.AreEqual( 6, chars.Count );
			Assert.AreEqual( "hogepo", ToString(chars) );
			Assert.Throws<AssertException>( delegate {
				chars.GetAt( 6 );
			} );
			
			// before head to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			Assert.Throws<AssertException>( delegate {
				chars.RemoveRange( -1, 2 );
			} );
			
			// head to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 0, 2 );
			Assert.AreEqual( "gepiyo", ToString(chars) );
			
			// middle to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 2, 5 );
			Assert.AreEqual( "hoiyo", ToString(chars) );
			
			// middle to end
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 5, 8 );
			Assert.AreEqual( "hogep", ToString(chars) );
			
			// middle to after end
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			Assert.Throws<AssertException>( delegate {
				chars.RemoveRange( 5, 9 );
			} );
		}

		[Test]
		public void CopyTo()
		{
			const string initBufContent = "123456";
			SplitArray<char> sary = new SplitArray<char>( 5, 8 );
			char[] buf;
			sary.Insert( 0, "hogepiyo".ToCharArray() );

			// before head to middle
			buf = initBufContent.ToCharArray();
			Assert.Throws<AssertException>( delegate {
				sary.CopyTo( -1, 5, buf );
			} );

			// begin to middle
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 0, 5, buf );
			Assert.AreEqual( "hogep6", new String(buf) );

			// middle to middle
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 1, 5, buf );
			Assert.AreEqual( "ogep56", new String(buf) );

			// middle to end
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 5, 8, buf );
			Assert.AreEqual( "iyo456", new String(buf) );

			// end to after end
			buf = initBufContent.ToCharArray();
			Assert.Throws<AssertException>( delegate {
				sary.CopyTo( 5, 9, buf );
			} );

			// Range before the gap
			MoveGap( sary, 2 );
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 0, 1, buf );
			Assert.AreEqual( "h23456", new String(buf) );

			// Range including the gap
			MoveGap( sary, 2 );
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 0, 4, buf );
			Assert.AreEqual( "hoge56", new String(buf) );

			// Range after the gap
			MoveGap( sary, 2 );
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 4, 8, buf );
			Assert.AreEqual( "piyo56", new String(buf) );
		}

		[Test]
		public void BinarySearch()
		{
			SplitArray<int> ary = new SplitArray<int>( 4 );

			ary.Clear();
			Assert.AreEqual( -1, ary.BinarySearch(1234) );

			ary.Clear();
			ary.Add( 3 );
			Assert.AreEqual( ~(0), ary.BinarySearch(2) );
			Assert.AreEqual(  (0), ary.BinarySearch(3) );
			Assert.AreEqual( ~(1), ary.BinarySearch(4) );

			ary.Clear();
			ary.Add( 1, 3 );
			Assert.AreEqual( ~(0), ary.BinarySearch(0) );
			Assert.AreEqual(  (0), ary.BinarySearch(1) );
			Assert.AreEqual( ~(1), ary.BinarySearch(2) );
			Assert.AreEqual(  (1), ary.BinarySearch(3) );
			Assert.AreEqual( ~(2), ary.BinarySearch(4) );

			SplitArray<System.Drawing.Point> points = new SplitArray<System.Drawing.Point>( 4 );
			points.Add( new System.Drawing.Point() );
			Assert.Throws<ArgumentException>( delegate {
				points.BinarySearch(new System.Drawing.Point(1,1));
			} );
		}

		static string ToString( SplitArray<char> sary )
		{
			return new string( sary.ToArray() );
		}

		static void MoveGap( SplitArray<char> sary, int index )
		{
			sary.Insert( index, new char[]{} );
		}
	}
}
