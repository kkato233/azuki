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
	public class UriMarkerTest
	{
		[TestMethod]
		public void Uri_Coverage()
		{
			Document doc = new Document();
			bool isMailAddress;

			// scheme part (first char)
			doc.Text = "";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "\xfeff";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "'";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "/";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "?";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "#";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = ":";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// scheme part (remainings)
			doc.Text = "htt/";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "htt?";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "htt#";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "htt'";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "http";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// slash-1
			doc.Text = "http:";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "http:'";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// slash-2
			doc.Text = "http:/";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "http:/'";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// authority part (first char)
			doc.Text = "http://";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "http://'";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// authority part (remainings)
			doc.Text = "http://s";
			Assert.AreEqual( 8, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http:///";
			Assert.AreEqual( 8, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s'";
			Assert.AreEqual( 8, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s/";
			Assert.AreEqual( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s?";
			Assert.AreEqual( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s#";
			Assert.AreEqual( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			// path part
			doc.Text = "http://s/";
			Assert.AreEqual( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s/'";
			Assert.AreEqual( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s/?";
			Assert.AreEqual( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s/#";
			Assert.AreEqual( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			// query part
			doc.Text = "http://s?";
			Assert.AreEqual( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s?'";
			Assert.AreEqual( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s?#";
			Assert.AreEqual( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			// fragment part
			doc.Text = "http://s#";
			Assert.AreEqual( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );

			doc.Text = "http://s#'";
			Assert.AreEqual( 9, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			Assert.AreEqual( false, isMailAddress );
		}

		[TestMethod]
		public void Uri_Char()
		{
			Document doc = new Document();
			bool isMailAddress;

			foreach( char ch in "'\"\r\n() \x3000\x3001\x3002\xfeff\xff08\xff09" )
			{
				doc.Text = "http://a" + ch;
				Assert.AreEqual( 8, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
			}
		}

		[TestMethod]
		public void MailTo_Coverage()
		{
			Document doc = new Document();
			bool isMailAddress;

			// localpart
			doc.Text = "mailto:";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:'";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:ab";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a'";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// domain (first char)
			doc.Text = "mailto:a@";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a@'";
			Assert.AreEqual( -1, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a@a";
			Assert.AreEqual( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			// domain (remainings)
			doc.Text = "mailto:a@a";
			Assert.AreEqual( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a@a'";
			Assert.AreEqual( 10, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );

			doc.Text = "mailto:a@aa";
			Assert.AreEqual( 11, UriMarker.Inst.GetUriEnd(doc, 0, out isMailAddress) );
		}
	}
}
