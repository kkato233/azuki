// file: EditAction.cs
// brief: Recorded editing action for UNDO/REDO.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2011-02-05
//=========================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// History object for UNDO/REDO that keeps information about one text replacement action.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Note that all text editing action can be described as a replacement
	/// so this is the only undo object used in Azuki.
	/// </para>
	/// </remarks>
	class EditAction
	{
		#region Fields
		Document _Document;
		int _Index;
		string _DeletedText;
		string _InsertedText;
		int _OldAnchorIndex;
		int _OldCaretIndex;
		LineDirtyStateUndoInfo _LdsUndoInfo;
		EditAction _Next = null;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="doc">document that the replacement has occured</param>
		/// <param name="index">index indicatating where the replacement has occured</param>
		/// <param name="deletedText">deleted text by the replacement</param>
		/// <param name="insertedText">inserted text by the replacement</param>
		/// <param name="oldAnchorIndex">index of the selection anchor at when the replacement has occured</param>
		/// <param name="oldCaretIndex">index of the caret at when the replacement has occured</param>
		/// <param name="ldsUndoInfo">line dirty states before the replacement</param>
		public EditAction( Document doc, int index, string deletedText, string insertedText, int oldAnchorIndex, int oldCaretIndex, LineDirtyStateUndoInfo ldsUndoInfo )
		{
			_Document = doc;
			_Index = index;
			_DeletedText = deletedText;
			_InsertedText = insertedText;
			_OldAnchorIndex = oldAnchorIndex;
			_OldCaretIndex = oldCaretIndex;
			_LdsUndoInfo = ldsUndoInfo;
		}
		#endregion

		#region Operation
		/// <summary>
		/// UNDO this replacement action.
		/// </summary>
		public void Undo()
		{
			EditAction action;

			// UNDO all chained history
			action = this;
			do
			{
				action.ExecuteUndo();
				action = action.Next;
			}
			while( action != null );
		}
		
		void ExecuteUndo()
		{
			// if this is a dummy action, do nothing
			if( _Document == null )
				return;

			Debug.Assert( _Index <= _Document.Length, "Invalid state; _Index:"+_Index+", _Document.Length:"+_Document.Length );

			// execute UNDO actions during stopping to record actions.
			bool wasRecordingHistory = _Document.IsRecordingHistory;
			_Document.IsRecordingHistory = false;
			{
				// release selection to ensure that the graphic will be properly updated.
				// because UNDO may cause some cases which is not supported by
				// invalidation logic of Azuki
				_Document.SetSelection( _Index, _Index );

				// set text
				_Document.Replace( _DeletedText, _Index, _Index+_InsertedText.Length );

				// set selection
				_Document.SetSelection( _OldAnchorIndex, _OldCaretIndex );

				// set line dirty state
				if( _LdsUndoInfo != null )
				{
					// Note that by executing Document.Replace, number of LDS entries is
					// already restored. Just overwriting to old state does the work.
					Debug.Assert( 0 < _LdsUndoInfo.DeletedStates.Length );
					for( int i=0; i<_LdsUndoInfo.DeletedStates.Length; i++ )
					{
						_Document.SetLineDirtyState(
								_LdsUndoInfo.LineIndex + i,
								_LdsUndoInfo.DeletedStates[i]
							);
					}
				}
			}
			_Document.IsRecordingHistory = wasRecordingHistory;
		}

		/// <summary>
		/// REDO this replacement action.
		/// </summary>
		public void Redo()
		{
			Stack<EditAction> reversedChain = new Stack<EditAction>( 32 );
			EditAction action;

			// reverse history chain
			action = this;
			do
			{
				reversedChain.Push( action );
				action = action.Next;
			}
			while( action != null );

			// REDO all histories in reversed order
			do
			{
				action = reversedChain.Pop();
				action.ExecuteRedo();
			}
			while( 0 < reversedChain.Count );
		}

		void ExecuteRedo()
		{
			// if this is a dummy action, do nothing
			if( _Document == null )
				return;

			Debug.Assert( _Index <= _Document.Length, "Invalid state; _Index:"+_Index+", _Document.Length:"+_Document.Length );

			// execute REDO actions during stopping to record actions.
			_Document.IsRecordingHistory = false;
			{
				// release selection to ensure the graphic will properly be updated
				// because REDO may cause some cases which is not supported by
				// invalidation logic of Azuki
				_Document.SetSelection( _Index, _Index );

				// set selection
				_Document.SetSelection( _OldAnchorIndex, _OldCaretIndex );

				// set text
				_Document.Replace( _InsertedText, _Index, _Index+_DeletedText.Length );

				// set line dirty state
				// (since effect to LDS by REDOing is same as normal text replacement does,
				// no special action is needed here)
			}
			_Document.IsRecordingHistory = true;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets the text deleted by this action.
		/// </summary>
		public string DeletedText
		{
			get{ return _DeletedText; }
		}

		/// <summary>
		/// Gets the text newly inserted by this action.
		/// </summary>
		public string InsertedText
		{
			get{ return _InsertedText; }
		}

		/// <summary>
		/// Changed action.
		/// </summary>
		public EditAction Next
		{
			get{ return _Next; }
			set{ _Next = value; }
		}
		#endregion

#		if DEBUG
		/// <summary>ToString for debug :)</summary>
		public override string ToString()
		{
			System.Text.StringBuilder text = new System.Text.StringBuilder( 64 );
			EditAction action = this;

			do
			{
				text.Append( action._Index );
				if( action._DeletedText != null && 0 < action._DeletedText.Length )
				{
					text.AppendFormat( null, "-[{0}]",
							action._DeletedText
							.Substring( 0, Math.Min(16, action._DeletedText.Length) )
							.Replace("\r", "<CR>")
							.Replace("\n", "<LF>")
						);
				}
				if( action._InsertedText != null && 0 < action._InsertedText.Length )
				{
					text.AppendFormat( null, "+[{0}]",
							action._InsertedText
							.Substring( 0, Math.Min(16, action._InsertedText.Length) )
							.Replace("\r", "<CR>")
							.Replace("\n", "<LF>")
						);
				}
				text.Append( '_' );

				action = action.Next;
			}
			while( action != null );

			text.Length--;
			return text.ToString();
		}
#		endif
	}

	class LineDirtyStateUndoInfo
	{
		public int LineIndex;
		public LineDirtyState[] DeletedStates = null;

		#region Debug code
#		if DEBUG
		/// <summary>ToString for debug</summary>
		public override string ToString()
		{
			System.Text.StringBuilder text = new System.Text.StringBuilder( 64 );

			text.Append( LineIndex );
			if( 0 < DeletedStates.Length )
			{
				text.Append( "-{" + ToChar(DeletedStates[0]) );
				for( int i=1; i<DeletedStates.Length; i++ )
				{
					text.Append( "," + ToChar(DeletedStates[i]) );
				}
				text.Append( '}' );
			}

			return text.ToString();
		}
		char ToChar( LineDirtyState lds )
		{
			switch( lds )
			{
				case LineDirtyState.Clean:	return 'C';
				case LineDirtyState.Dirty:	return 'D';
				default:					return 'c';
			}
		}
#		endif
		#endregion
	}
}
