using System;
using System.Text;
using NUnit.Framework;

namespace Sgry.Azuki.Test
{
	[TestFixture]
	public class EditHistoryTest
	{
		[Test]
		public void Add()
		{
			const int len = 1024;
			Document doc = new Document();
			EditHistory history = new EditHistory();
			EditAction action;

			Assert.AreEqual( null, history.GetUndoAction() );
			Assert.AreEqual( false, history.CanUndo );
			Assert.AreEqual( false, history.CanRedo );

			for( int i=0; i<len; i++ )
			{
				history.Add(
						new EditAction(doc, i, i.ToString(), (i+'a').ToString(), 0, 0, null)
					);
			}

			Assert.AreEqual( true, history.CanUndo );
			Assert.AreEqual( false, history.CanRedo );

			for( int i=len-1; 0<=i; i-- )
			{
				action = history.GetUndoAction();
				Assert.AreEqual( i+"-["+i+"]+["+(i+'a')+"]", action.ToString() );
			}

			Assert.AreEqual( null, history.GetUndoAction() );
			Assert.AreEqual( false, history.CanUndo );
			Assert.AreEqual( true, history.CanRedo );
		}

		[Test]
		public void Case2()
		{
			const int len1 = 1024;
			const int len2 = 512;
			const int len3 = 768;
			Document doc = new Document();
			EditHistory history = new EditHistory();
			EditAction action;

			Assert.AreEqual( null, history.GetUndoAction() );

			Assert.AreEqual( false, history.CanUndo );
			Assert.AreEqual( false, history.CanRedo );

			// add some
			for( int i=0; i<len1; i++ )
			{
				history.Add(
						new EditAction(doc, i, i.ToString(), (i+'a').ToString(), 0, 0, null)
					);
			}

			Assert.AreEqual( true, history.CanUndo );
			Assert.AreEqual( false, history.CanRedo );

			// pop some
			for( int i=len1-1; len2<=i; i-- )
			{
				action = history.GetUndoAction();
				Assert.AreEqual( i+"-["+i+"]+["+(i+'a')+"]", action.ToString() );
			}

			Assert.AreEqual( true, history.CanUndo );
			Assert.AreEqual( true, history.CanRedo );

			// redo some
			for( int i=len2; i<len3; i++ )
			{
				action = history.GetRedoAction();
				Assert.AreEqual( i+"-["+i+"]+["+(i+'a')+"]", action.ToString() );
			}

			Assert.AreEqual( true, history.CanUndo );
			Assert.AreEqual( true, history.CanRedo );

			// add one and ensure that it can redo no more
			history.Add(
					new EditAction(doc, len3, (len3).ToString(), (len3+'a').ToString(), 0, 0, null)
				);
			Assert.AreEqual( true, history.CanUndo );
			Assert.AreEqual( false, history.CanRedo );

			// pop all
			for( int i=len3; 0<=i; i-- )
			{
				action = history.GetUndoAction();
				Assert.AreEqual( i+"-["+i+"]+["+(i+'a')+"]", action.ToString() );
			}

			Assert.AreEqual( false, history.CanUndo );
			Assert.AreEqual( true, history.CanRedo );
			Assert.AreEqual( null, history.GetUndoAction() );
			Assert.AreEqual( "0-[0]+[97]", history.GetRedoAction().ToString() );
		}

		[Test]
		public void GroupUndoRedo()
		{
			// end before begin
			{
				EditHistory history = new EditHistory();
				history.EndUndo();
				history.EndUndo();
			}

			// double call of begin
			{
				EditHistory history = new EditHistory();
				history.BeginUndo();
				history.BeginUndo();
				history.EndUndo();
			}

			// double call of end
			{
				EditHistory history = new EditHistory();
				history.BeginUndo();
				history.EndUndo();
				history.EndUndo();
			}

			// grouping actions
			{
				Document doc = new Document();
				doc.Replace( "a", 0, 0 );
				doc.BeginUndo();
				doc.Replace( "b", 1, 1 );
				doc.Replace( "c", 2, 2 );
				doc.EndUndo();
				doc.Replace( "d", 3, 3 );

				Assert.AreEqual( "abcd", doc.Text );
				doc.Undo();
				Assert.AreEqual( "abc", doc.Text );
				doc.Undo();
				Assert.AreEqual( "a", doc.Text );
				doc.Undo();
				Assert.AreEqual( "", doc.Text );
				doc.Undo();
				Assert.AreEqual( "", doc.Text );
				doc.Redo();
				Assert.AreEqual( "a", doc.Text );
				doc.Redo();
				Assert.AreEqual( "abc", doc.Text );
				doc.Redo();
				Assert.AreEqual( "abcd", doc.Text );
				doc.Redo();
				Assert.AreEqual( "abcd", doc.Text );
			}

			// undo during grouping actions
			{
				Document doc = new Document();
				doc.Replace( "abc", 0, 0 );
				doc.BeginUndo();
				doc.Replace( "X", 1, 1 );
				Assert.AreEqual( "aXbc", doc.Text );
				doc.Replace( "Y", 2, 2 );
				Assert.AreEqual( "aXYbc", doc.Text );
				doc.Undo();
				Assert.AreEqual( "abc", doc.Text );
				doc.EndUndo();
				Assert.AreEqual( "abc", doc.Text );
			}

			// redo during grouping actions
			{
				Document doc = new Document();
				doc.Replace( "abc", 0, 0 );
				doc.Replace( "X", 1, 1 );
				Assert.AreEqual( "aXbc", doc.Text );
				doc.Undo();
				Assert.AreEqual( "abc", doc.Text );
				doc.BeginUndo();
				doc.Redo();
				Assert.AreEqual( "aXbc", doc.Text );
				doc.EndUndo();
				Assert.AreEqual( "aXbc", doc.Text );
			}
			{
				Document doc = new Document();
				doc.Replace( "abc", 0, 0 );
				doc.Replace( "X", 1, 1 );
				Assert.AreEqual( "aXbc", doc.Text );
				doc.Undo();
				Assert.AreEqual( "abc", doc.Text );
				doc.BeginUndo();
				doc.Replace( "Y", 1, 1 );
				doc.Redo();
				Assert.AreEqual( "aYbc", doc.Text );
				doc.EndUndo();
				Assert.AreEqual( "aYbc", doc.Text );
			}

			// Nested call of group undo
			{
				Document doc = new Document();
				doc.Replace( "a", doc.Length, doc.Length );
				doc.BeginUndo();
				doc.Replace( "b", doc.Length, doc.Length );
				doc.BeginUndo();
				doc.Replace( "c", doc.Length, doc.Length );
				doc.EndUndo();
				doc.Replace( "d", doc.Length, doc.Length );
				doc.EndUndo();
				Assert.AreEqual( "abcd", doc.Text );
				doc.Undo();
				Assert.AreEqual( "a", doc.Text );
				doc.Undo();
				Assert.AreEqual( "", doc.Text );
				doc.Undo();
				Assert.AreEqual( "", doc.Text );
				doc.Redo();
				Assert.AreEqual( "a", doc.Text );
				doc.Redo();
				Assert.AreEqual( "abcd", doc.Text );
				doc.Redo();
				Assert.AreEqual( "abcd", doc.Text );
			}
		}

		[Test]
		public void DocumentDirtyState()
		{
			// dirty state
			{
				Document doc = new Document();
				Assert.AreEqual( false, doc.IsDirty );

				doc.Replace( "a", 0, 0 );
				Assert.AreEqual( true, doc.IsDirty );
				Assert.AreEqual( "a", doc.Text );

				doc.IsDirty = false;
				Assert.AreEqual( false, doc.IsDirty );
				Assert.AreEqual( "a", doc.Text );

				doc.BeginUndo();
				Assert.AreEqual( false, doc.IsDirty );
				Assert.AreEqual( "a", doc.Text );

				doc.Replace( "b", 1, 1 );
				Assert.AreEqual( true, doc.IsDirty );
				Assert.AreEqual( "ab", doc.Text );

				doc.Replace( "c", 2, 2 );
				Assert.AreEqual( true, doc.IsDirty );
				Assert.AreEqual( "abc", doc.Text );

				doc.EndUndo();
				Assert.AreEqual( true, doc.IsDirty );
				Assert.AreEqual( "abc", doc.Text );

				doc.Undo();
				Assert.AreEqual( false, doc.IsDirty );
				Assert.AreEqual( "a", doc.Text );

				doc.Undo();
				Assert.AreEqual( true, doc.IsDirty );
				Assert.AreEqual( "", doc.Text );

				doc.Redo();
				Assert.AreEqual( false, doc.IsDirty );
				Assert.AreEqual( "a", doc.Text );

				doc.Undo();
				doc.Replace( "a", 0, 0 );
				Assert.AreEqual( true, doc.IsDirty );
				Assert.AreEqual( "a", doc.Text );
			}

			// change IsDirty flag while recording group UNDO
			{
				Document doc = new Document();
				doc.BeginUndo();
				Assert.Throws<InvalidOperationException>( delegate{
					doc.IsDirty = true;
				} );
			}

			// special case; BeginUndo at initial state
			// ([*] goes exceptional 'if' code in EditHistory.IsSavedState)
			{
				Document doc = new Document();
				Assert.AreEqual( false, doc.IsDirty );
				doc.BeginUndo();
				Assert.AreEqual( false, doc.IsDirty );
				doc.Replace( "a", 0, 0 );
				Assert.AreEqual( true, doc.IsDirty ); // [*]
				doc.EndUndo();
				Assert.AreEqual( true, doc.IsDirty );
			}
		}

		[Test]
		public void Test_LineDirtyState()
		{
			Document doc = new Document();

			// deletion - single line
			doc.Text = "keep it\nas simple as\npossible";
			doc.ClearHistory();
			doc.Replace( "", 11, 18 );
			doc.Undo();
			Assert.AreEqual( "CCC", MakeLdsStr(doc) );
			doc.Redo();
			Assert.AreEqual( "CDC", MakeLdsStr(doc) );

			// deletion - multiple lines
			doc.Text = "keep it\nas simple as\npossible";
			doc.ClearHistory();
			doc.Replace( "", 11, 21 );
			doc.Undo();
			Assert.AreEqual( "CCC", MakeLdsStr(doc) );
			doc.Redo();
			Assert.AreEqual( "CD", MakeLdsStr(doc) );

			// deletion - creating CR+LF
			doc.Text = "a\rb\nc";
			doc.ClearHistory();
			doc.Replace( "", 2, 3 );
			doc.Undo();
			Assert.AreEqual( "CCC", MakeLdsStr(doc) );
			doc.Redo();
			Assert.AreEqual( "DC", MakeLdsStr(doc) );

			// deletion - destroying CR+LF (1)
			doc.Text = "ab\r\nc";
			doc.ClearHistory();
			doc.Replace( "", 1, 3 );
			doc.Undo();
			Assert.AreEqual( "CC", MakeLdsStr(doc) );
			doc.Redo();
			Assert.AreEqual( "DC", MakeLdsStr(doc) );

			// deletion - destroying CR+LF (2)
			doc.Text = "a\r\nbc";
			doc.ClearHistory();
			doc.Replace( "", 2, 4 );
			doc.Undo();
			Assert.AreEqual( "CC", MakeLdsStr(doc) );
			doc.Redo();
			Assert.AreEqual( "DD", MakeLdsStr(doc) );

			// insertion - creating CR+LF (1)
			doc.Text = "a\nb";
			doc.ClearHistory();
			doc.Replace( "\r", 1, 1 );
			doc.Undo();
			Assert.AreEqual( "CC", MakeLdsStr(doc) );
			doc.Redo();
			Assert.AreEqual( "DC", MakeLdsStr(doc) );

			// insertion - creating CR+LF (2)
			doc.Text = "a\rb";
			doc.ClearHistory();
			doc.Replace( "\n", 2, 2 );
			doc.Undo();
			Assert.AreEqual( "CC", MakeLdsStr(doc) );
			doc.Redo();
			Assert.AreEqual( "DD", MakeLdsStr(doc) );

			// insertion - destroying CR+LF
			doc.Text = "a\r\nb";
			doc.ClearHistory();
			doc.Replace( "X", 2, 2 );
			doc.Undo();
			Assert.AreEqual( "CC", MakeLdsStr(doc) );
			doc.Redo();
			Assert.AreEqual( "DDC", MakeLdsStr(doc) );
		}

		#region Utilities
		static string MakeLdsStr( Document doc )
		{
			StringBuilder buf = new StringBuilder( 32 );

			for( int i=0; i<doc.LineCount; i++ )
			{
				switch( doc.GetLineDirtyState(i) )
				{
					case LineDirtyState.Clean:		buf.Append( 'C' ); break;
					case LineDirtyState.Cleaned:	buf.Append( 'S' ); break;
					default:						buf.Append( 'D' ); break;
				}
			}

			return buf.ToString();
		}
		#endregion
	}
}
