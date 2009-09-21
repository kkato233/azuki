// file: EditAction.cs
// brief: Recorded editing action for UNDO/REDO.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2009-09-21
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
		public EditAction( Document doc, int index, string deletedText, string insertedText )
		{
			_Document = doc;
			_Index = index;
			_DeletedText = deletedText;
			_InsertedText = insertedText;
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
			_Document.IsRecordingHistory = false;
			{
				// release selection to ensure the graphic will properly be updated
				// because UNDO may cause some cases which is not supported by
				// invalidation logic of Azuki
				_Document.SetSelection( _Index, _Index );

				// replace back to old text
				_Document.Replace( _DeletedText, _Index, _Index+_InsertedText.Length );

				// recover old selection
				_Document.SetSelection( _Index, _Index+_DeletedText.Length );
			}
			_Document.IsRecordingHistory = true;
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

				// replace back to old text
				_Document.Replace( _InsertedText, _Index, _Index+_DeletedText.Length );

				// recover old selection
				_Document.SetSelection( _Index, _Index+_InsertedText.Length );
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
			return "[" + _Index + "|" + _DeletedText + "|" + _InsertedText + "]";
		}
#		endif
	}
}
