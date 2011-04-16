// 2011-04-17
#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using Sgry.Azuki.WinForms;

namespace Sgry.Azuki.Test
{
	static class FixedBugsTest
	{
		public static void Test()
		{
			int testNum = 0;
			Console.WriteLine( "[Test of already fixed bugs]" );

			// 
			Console.WriteLine("test {0} - Forum 28741", testNum++);
			TestUtl.Do( Test_Forum28741 );

			Console.WriteLine("done.");
			Console.WriteLine();
		}

		static void Test_Forum28741()
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
				TestUtl.AssertEquals( "1110", textChanged_IsDirty.ToString() );
				TestUtl.AssertEquals( "1110", textChanged_CanUndo.ToString() );
				TestUtl.AssertEquals( "0011", textChanged_CanRedo.ToString() );
				TestUtl.AssertEquals( "1110", doc_CC_IsDirty.ToString() );
				TestUtl.AssertEquals( "1110", doc_CC_CanUndo.ToString() );
				TestUtl.AssertEquals( "0011", doc_CC_CanRedo.ToString() );
				TestUtl.AssertEquals( "10", doc_DSC_IsDirty.ToString() );
				TestUtl.AssertEquals( "10", doc_DSC_CanUndo.ToString() );
				TestUtl.AssertEquals( "01", doc_DSC_CanRedo.ToString() );
			}
		}
	}
}
#endif
