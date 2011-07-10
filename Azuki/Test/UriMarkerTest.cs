#if TEST
using System;

namespace Sgry.Azuki.Test
{
	class UriMarkerTest
	{
		public static void Test()
		{
			int test_num = 0;
			Console.WriteLine( "[Test for UriMarker]" );

			Console.WriteLine( "test {0} - URI (coverage)", ++test_num );
			TestUtl.Do( Test_Uri_Coverage );

			Console.WriteLine( "test {0} - URI (character validation)", ++test_num );
			TestUtl.Do( Test_Uri_Char );

			Console.WriteLine( "test {0} - MailTo (coverage)", ++test_num );
			TestUtl.Do( Test_MailTo_Coverage );

			Console.WriteLine( "done." );
			Console.WriteLine();
		}

		static void Test_Uri_Coverage()
		{
			Document doc = new Document();
			bool isMailAddress;

			// scheme part (first char)
			doc.Text = "";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "\xfeff";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "'";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "/";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "?";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "#";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = ":";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// scheme part (remainings)
			doc.Text = "htt/";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "htt?";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "htt#";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "htt'";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "http";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// slash-1
			doc.Text = "http:";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "http:'";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// slash-2
			doc.Text = "http:/";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "http:/'";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// authority part (first char)
			doc.Text = "http://";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "http://'";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// authority part (remainings)
			doc.Text = "http://s";
			TestUtl.AssertEquals( 8, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http:///";
			TestUtl.AssertEquals( 8, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s'";
			TestUtl.AssertEquals( 8, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s/";
			TestUtl.AssertEquals( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s?";
			TestUtl.AssertEquals( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s#";
			TestUtl.AssertEquals( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			// path part
			doc.Text = "http://s/";
			TestUtl.AssertEquals( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s/'";
			TestUtl.AssertEquals( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s/?";
			TestUtl.AssertEquals( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s/#";
			TestUtl.AssertEquals( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			// query part
			doc.Text = "http://s?";
			TestUtl.AssertEquals( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s?'";
			TestUtl.AssertEquals( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s?#";
			TestUtl.AssertEquals( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			// fragment part
			doc.Text = "http://s#";
			TestUtl.AssertEquals( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );

			doc.Text = "http://s#'";
			TestUtl.AssertEquals( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			TestUtl.AssertEquals( false, isMailAddress );
		}

		static void Test_Uri_Char()
		{
			Document doc = new Document();
			bool isMailAddress;

			foreach( char ch in "'\"\r\n() \x3000\x3001\x3002\xfeff\xff08\xff09" )
			{
				doc.Text = "http://a" + ch;
				TestUtl.AssertEquals( 8, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			}
		}

		static void Test_MailTo_Coverage()
		{
			Document doc = new Document();
			bool isMailAddress;

			// localpart
			doc.Text = "mailto:";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:'";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:ab";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a'";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// domain (first char)
			doc.Text = "mailto:a@";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a@'";
			TestUtl.AssertEquals( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a@a";
			TestUtl.AssertEquals( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// domain (remainings)
			doc.Text = "mailto:a@a";
			TestUtl.AssertEquals( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a@a'";
			TestUtl.AssertEquals( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a@aa";
			TestUtl.AssertEquals( 11, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
		}
	}
}
#endif
