// 2011-01-29
#if TEST
using System;

namespace Sgry.Azuki.Test
{
	static class EditHistoryTest
	{
		public static void Test()
		{
			int testNum = 0;
			Console.WriteLine( "[Test for Azuki.EditHistory]" );

			// case 1 (add actions and pop actions)
			Console.WriteLine("test {0} - case 1", testNum++);
			TestUtl.Do( Test_Add );

			// case 2 (add actions and pop some actions and get some actions)
			Console.WriteLine("test {0} - case 2", testNum++);
			TestUtl.Do( Test_Case2 );

			// line dirty state
			Console.WriteLine("test {0} - line dirty state history", testNum++);
			TestUtl.Do( Test_LineDirtyStateHistory );

			Console.WriteLine("done.");
			Console.WriteLine();
		}

		static void Test_Add()
		{
			const int len = 1024;
			Document doc = new Document();
			EditHistory history = new EditHistory();
			EditAction action;

			TestUtl.AssertEquals( null, history.GetUndoAction() );
			TestUtl.AssertEquals( false, history.CanUndo );
			TestUtl.AssertEquals( false, history.CanRedo );

			for( int i=0; i<len; i++ )
			{
				history.Add(
						new EditAction(doc, i, i.ToString(), (i+'a').ToString(), 0, 0, null)
					);
			}
			
			TestUtl.AssertEquals( true, history.CanUndo );
			TestUtl.AssertEquals( false, history.CanRedo );

			for( int i=len-1; 0<=i; i-- )
			{
				action = history.GetUndoAction();
				TestUtl.AssertEquals( "["+i+"|"+i+"|"+(i+'a')+"]", action.ToString() );
			}

			TestUtl.AssertEquals( null, history.GetUndoAction() );
			TestUtl.AssertEquals( false, history.CanUndo );
			TestUtl.AssertEquals( true, history.CanRedo );
		}

		static void Test_Case2()
		{
			const int len1 = 1024;
			const int len2 = 512;
			const int len3 = 768;
			Document doc = new Document();
			EditHistory history = new EditHistory();
			EditAction action;

			TestUtl.AssertEquals( null, history.GetUndoAction() );

			TestUtl.AssertEquals( false, history.CanUndo );
			TestUtl.AssertEquals( false, history.CanRedo );

			// add some
			for( int i=0; i<len1; i++ )
			{
				history.Add(
						new EditAction(doc, i, i.ToString(), (i+'a').ToString(), 0, 0, null)
					);
			}
			
			TestUtl.AssertEquals( true, history.CanUndo );
			TestUtl.AssertEquals( false, history.CanRedo );

			// pop some
			for( int i=len1-1; len2<=i; i-- )
			{
				action = history.GetUndoAction();
				TestUtl.AssertEquals( "["+i+"|"+i+"|"+(i+'a')+"]", action.ToString() );
			}

			TestUtl.AssertEquals( true, history.CanUndo );
			TestUtl.AssertEquals( true, history.CanRedo );

			// redo some
			for( int i=len2; i<len3; i++ )
			{
				action = history.GetRedoAction();
				TestUtl.AssertEquals( "["+i+"|"+i+"|"+(i+'a')+"]", action.ToString() );
			}

			TestUtl.AssertEquals( true, history.CanUndo );
			TestUtl.AssertEquals( true, history.CanRedo );

			// add one and ensure that it can redo no more
			history.Add(
					new EditAction(doc, len3, (len3).ToString(), (len3+'a').ToString(), 0, 0, null)
				);
			TestUtl.AssertEquals( true, history.CanUndo );
			TestUtl.AssertEquals( false, history.CanRedo );

			// pop all
			for( int i=len3; 0<=i; i-- )
			{
				action = history.GetUndoAction();
				TestUtl.AssertEquals( "["+i+"|"+i+"|"+(i+'a')+"]", action.ToString() );
			}

			TestUtl.AssertEquals( false, history.CanUndo );
			TestUtl.AssertEquals( true, history.CanRedo );
			TestUtl.AssertEquals( null, history.GetUndoAction() );
			TestUtl.AssertEquals( "[0|0|97]", history.GetRedoAction().ToString() );
		}

		static void Test_LineDirtyStateHistory()
		{
			Document doc = new Document();

			// deletion - single line
			doc.Text = "keep it\nas simple as\npossible";
			doc.ClearHistory();
			doc.Replace( "", 11, 18 );
			doc.Undo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(2) );
			doc.Redo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(1) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(2) );

			// deletion - multiple lines
			doc.Text = "keep it\nas simple as\npossible";
			doc.ClearHistory();
			doc.Replace( "", 11, 21 );
			doc.Undo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(2) );
			doc.Redo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(1) );

			// deletion - creating CR+LF
			doc.Text = "a\rb\nc";
			doc.ClearHistory();
			doc.Replace( "", 2, 3 );
			doc.Undo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(2) );
			doc.Redo();
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );

			// deletion - destroying CR+LF (1)
			doc.Text = "ab\r\nc";
			doc.ClearHistory();
			doc.Replace( "", 1, 3 );
			doc.Undo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );
			doc.Redo();
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );

			// deletion - destroying CR+LF (2)
			doc.Text = "a\r\nbc";
			doc.ClearHistory();
			doc.Replace( "", 2, 4 );
			doc.Undo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );
			doc.Redo();
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(1) );

			// insertion - creating CR+LF (1)
			doc.Text = "a\nb";
			doc.ClearHistory();
			doc.Replace( "\r", 1, 1 );
			doc.Undo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );
			doc.Redo();
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );

			// insertion - creating CR+LF (2)
			doc.Text = "a\rb";
			doc.ClearHistory();
			doc.Replace( "\n", 2, 2 );
			doc.Undo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );
			doc.Redo();
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(1) );

			// insertion - destroying CR+LF
			doc.Text = "a\r\nb";
			doc.ClearHistory();
			doc.Replace( "X", 2, 2 );
			doc.Undo();
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(1) );
			doc.Redo();
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(0) );
			TestUtl.AssertEquals( LineDirtyState.Dirty, doc.GetLineDirtyState(1) );
			TestUtl.AssertEquals( LineDirtyState.Clean, doc.GetLineDirtyState(2) );
		}
	}
}
#endif
