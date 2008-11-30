// file: EditAction.cs
// brief: Recorded editing action for UNDO/REDO.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-05-08
//=========================================================
using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// History object for UNDO/REDO that keeps information about one text replacement action.
	/// Note that all text editing action can be described as a replacement
	/// so this is the only undo object used in Azuki.
	/// </summary>
	class EditAction
	{
		Document _Document;
		int _Index;
		string _DeletedText;
		string _InsertedText;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="doc">document that the replacement has occured</param>
		/// <param name="index">index indicatating where the replacement has occured</param>
		/// <param name="deletedText">deleted text by the replacement</param>
		/// <param name="insertedText">inserted text by the replacement</param>
		internal EditAction( Document doc, int index, string deletedText, string insertedText )
		{
			_Document = doc;
			_Index = index;
			_DeletedText = deletedText;
			_InsertedText = insertedText;
		}

		/// <summary>
		/// UNDO this replacement action.
		/// </summary>
		public void Undo()
		{
			Debug.Assert( _Index == 0 || _Index <= _Document.Length, "Invalid state; _Index:"+_Index+", _Document.Length:"+_Document.Length );

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
			Debug.Assert( _Index == 0 || _Index <= _Document.Length, "Invalid state; _Index:"+_Index+", _Document.Length:"+_Document.Length );

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

#		if DEBUG
		/// <summary>ToString for debug :)</summary>
		public override string ToString()
		{
			return "[" + _Index + "|" + _DeletedText + "|" + _InsertedText + "]";
		}
#		endif
	}
}
