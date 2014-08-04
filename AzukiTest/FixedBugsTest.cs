using System;
using System.Text;
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
	public class FixedBugsTest
	{
		[TestMethod]
		public void Forum28741()
		{
			StringBuilder textChanged_IsDirty = new StringBuilder();
			StringBuilder textChanged_CanUndo = new StringBuilder();
			StringBuilder textChanged_CanRedo = new StringBuilder();
			StringBuilder doc_CC_IsDirty = new StringBuilder();
			StringBuilder doc_CC_CanUndo = new StringBuilder();
			StringBuilder doc_CC_CanRedo = new StringBuilder();
			StringBuilder doc_DSC_IsDirty = new StringBuilder();
			StringBuilder doc_DSC_CanUndo = new StringBuilder();
			StringBuilder doc_DSC_CanRedo = new StringBuilder();

			using( AzukiControl azuki = new AzukiControl() )
			{
				azuki.TextChanged += delegate( object sender, EventArgs e ) {
					textChanged_IsDirty.Append( azuki.Document.IsDirty ? '1' : '0' );
					textChanged_CanUndo.Append( azuki.Document.CanUndo ? '1' : '0' );
					textChanged_CanRedo.Append( azuki.Document.CanRedo ? '1' : '0' );
				};
				azuki.Document.ContentChanged += delegate( object sender, ContentChangedEventArgs e ) {
					doc_CC_IsDirty.Append( azuki.Document.IsDirty ? '1' : '0' );
					doc_CC_CanUndo.Append( azuki.Document.CanUndo ? '1' : '0' );
					doc_CC_CanRedo.Append( azuki.Document.CanRedo ? '1' : '0' );
				};
				azuki.Document.DirtyStateChanged += delegate( object sender, EventArgs e ) {
					doc_DSC_IsDirty.Append( azuki.Document.IsDirty ? '1' : '0' );
					doc_DSC_CanUndo.Append( azuki.Document.CanUndo ? '1' : '0' );
					doc_DSC_CanRedo.Append( azuki.Document.CanRedo ? '1' : '0' );
				};

				// input a character
				azuki.HandleTextInput( "a" );
				azuki.HandleTextInput( "b" );
				azuki.Undo();
				azuki.Undo();

				// check
				Assert.AreEqual( "1110", textChanged_IsDirty.ToString() );
				Assert.AreEqual( "1110", textChanged_CanUndo.ToString() );
				Assert.AreEqual( "0011", textChanged_CanRedo.ToString() );
				Assert.AreEqual( "1110", doc_CC_IsDirty.ToString() );
				Assert.AreEqual( "1110", doc_CC_CanUndo.ToString() );
				Assert.AreEqual( "0011", doc_CC_CanRedo.ToString() );
				Assert.AreEqual( "10", doc_DSC_IsDirty.ToString() );
				Assert.AreEqual( "10", doc_DSC_CanUndo.ToString() );
				Assert.AreEqual( "01", doc_DSC_CanRedo.ToString() );
			}
		}
	}
}
