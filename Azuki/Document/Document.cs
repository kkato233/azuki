// file: Document.cs
// brief: Document of Azuki engine.
// author: YAMAMOTO Suguru
// update: 2009-02-07
//=========================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Color = System.Drawing.Color;
using Regex = System.Text.RegularExpressions.Regex;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	using Highlighter;

	/// <summary>
	/// The document of the Azuki editor engine.
	/// </summary>
	public class Document : IEnumerable
	{
		#region Fields
		TextBuffer _Buffer = new TextBuffer( 1024, 256 );
		SplitArray<int> _LHI = new SplitArray<int>( 64 ); // line head indexes
		EditHistory _History = new EditHistory();
		int _CaretIndex = 0;
		int _AnchorIndex = 0;
		bool _IsRecordingHistory = true;
		string _EolCode = "\r\n";
		bool _IsReadOnly = false;
		bool _IsDirty = false;
		IHighlighter _Highlighter = null;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public Document()
		{
			// initialize LHI
			_LHI.Clear();
			_LHI.Add( 0 );
		}
		#endregion

		#region States
		/// <summary>
		/// Gets or sets the flag that is true if there are any unsaved modifications.
		/// </summary>
		/// <remarks>
		/// Dirty flag is the flag that is true if there are any unsaved modifications.
		/// Although any changes occured in Azuki sets this flag true automatically,
		/// setting this flag back to false must be done manually
		/// so application is responsible to do so after saving content.
		/// </remarks>
		public bool IsDirty
		{
			get{ return _IsDirty; }
			set
			{
				bool valueChanged = (_IsDirty != value);
				
				_IsDirty = value;
				if( valueChanged )
				{
					InvokeDirtyStateChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets whether this document is recording edit actions or not.
		/// </summary>
		public bool IsRecordingHistory
		{
			get{ return _IsRecordingHistory; }
			set{ _IsRecordingHistory = value; }
		}

		/// <summary>
		/// Gets or sets whether this document is read-only or not.
		/// </summary>
		public bool IsReadOnly
		{
			get{ return _IsReadOnly; }
			set{ _IsReadOnly = value; }
		}

		/// <summary>
		/// Gets whether an available undo action exists or not.
		/// </summary>
		public bool CanUndo
		{
			get{ return _History.CanUndo; }
		}

		/// <summary>
		/// Gets whether an available REDO action exists or not.
		/// </summary>
		public bool CanRedo
		{
			get{ return _History.CanRedo; }
		}
		#endregion

		#region Selection
		/// <summary>
		/// Gets index of where the caret is at (in char-index).
		/// </summary>
		public int CaretIndex
		{
			get{ return _CaretIndex; }
		}

		/// <summary>
		/// Gets caret location by logical line/column index.
		/// </summary>
		/// <param name="lineIndex">line index of where the caret is at</param>
		/// <param name="columnIndex">column index of where the caret is at</param>
		public void GetCaretIndex( out int lineIndex, out int columnIndex )
		{
			GetLineColumnIndexFromCharIndex( _CaretIndex, out lineIndex, out columnIndex );
		}

		/// <summary>
		/// Sets caret location by logical line/column index.
		/// Note that calling this method will release selection.
		/// </summary>
		/// <param name="lineIndex">new line index of where the caret is at</param>
		/// <param name="columnIndex">new column index of where the caret is at</param>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public void SetCaretIndex( int lineIndex, int columnIndex )
		{
			if( lineIndex < 0 || columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "lineIndex or columnIndex", "index must not be negative value. (lineIndex:"+lineIndex+", columnIndex:"+columnIndex+")" );
			if( _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "too large line index was given (given:"+lineIndex+" actual line count:"+_LHI.Count+")" );
			
			int caretIndex = LineLogic.GetCharIndexFromLineColumnIndex( _Buffer, _LHI, lineIndex, columnIndex );
			SetSelection( caretIndex, caretIndex );
		}

		/// <summary>
		/// Gets index of the position where the selection starts (in char-index).
		/// </summary>
		public int AnchorIndex
		{
			get{ return _AnchorIndex; }
		}

		/// <summary>
		/// Sets selection range.
		/// </summary>
		/// <param name="anchor">new index of the selection anchor</param>
		/// <param name="caret">new index of the caret</param>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <remarks>
		/// This method sets selection range and causes
		/// <see cref="Document.SelectionChanged">Document.SelectionChanged</see> event
		/// Note that if given index is at middle of a surrogate pair,
		/// selection range will be automatically expanded to avoid dividing the pair.
		/// </remarks>
		public void SetSelection( int anchor, int caret )
		{
			if( anchor < 0 || _Buffer.Count < anchor
				|| caret < 0 || _Buffer.Count < caret )
			{
				throw new ArgumentOutOfRangeException( "'anchor' or 'caret'", "Invalid line index was given (anchor:"+anchor+", caret:"+caret+")." );
			}
			
			int oldAnchor, oldCaret;

			// if given parameters change nothing, do nothing
			if( _AnchorIndex == anchor && _CaretIndex == caret )
			{
				return;
			}

			// ensure that given index is not in middle of the surrogate pairs
			Utl.ConstrainIndex( _Buffer, ref anchor, ref caret );

			// get anchor/caret position in new text content
			oldAnchor = _AnchorIndex;
			oldCaret = _CaretIndex;

			// apply new selection
			_AnchorIndex = anchor;
			_CaretIndex = caret;

			// invoke event
			InvokeSelectionChanged( oldAnchor, oldCaret );
		}

		/// <summary>
		/// Gets range of current selection.
		/// Note that this method does not return [anchor, caret) pair but [begin, end) pair.
		/// </summary>
		/// <param name="begin">index of where the selection begins.</param>
		/// <param name="end">index of where the selection ends (selection do not includes the char at this index).</param>
		public void GetSelection( out int begin, out int end )
		{
			if( _AnchorIndex < _CaretIndex )
			{
				begin = _AnchorIndex;
				end = _CaretIndex;
			}
			else
			{
				begin = _CaretIndex;
				end = _AnchorIndex;
			}
		}
		#endregion

		#region Content Access
		internal TextBuffer InternalBuffer
		{
			get{ return _Buffer; }
		}

		/// <summary>
		/// Gets or sets currently inputted text.
		/// </summary>
		/// <remarks>
		/// Getting text content through this property
		/// will copy all characters from internal buffer
		/// to a string object and returns it.
		/// </remarks>
		public string Text
		{
			get
			{
				if( _Buffer.Count == 0 )
					return String.Empty;

				char[] text = new char[ _Buffer.Count ];
				_Buffer.GetRange( 0, _Buffer.Count, ref text );
				return new String( text );
			}
			set
			{
				if( value == null )
					value = String.Empty;

				Replace( value, 0, this.Length );
				SetSelection( 0, 0 );
			}
		}

		/// <summary>
		/// Gets a character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public char GetCharAt( int index )
		{
			if( index < 0 || _Buffer.Count <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", this.Length:"+Length+")." );

			return _Buffer.GetAt( index );
		}

		/// <summary>
		/// Gets a word at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public string GetWordAt( int index )
		{
			if( index < 0 || _Buffer.Count < index ) // index can be equal to char-count
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", this.Length:"+Length+")." );

			int begin, end;

			WordLogic.GetWordAt( this, index, out begin, out end );
			if( begin < 0 || end < 0 || end <= begin )
			{
				return String.Empty;
			}

			return GetTextInRange( begin, end );
		}

		/// <summary>
		/// Gets number of characters currently held in this document.
		/// Note that a surrogate pair will be counted as two characters.
		/// </summary>
		/// <remarks>
		/// This property is the number of characters currently held in this document.
		/// Since Azuki stores characters in form of UTF-16,
		/// surrogate pairs will not be counted as "1 character" in this property.
		/// </remarks>
		public int Length
		{
			get{ return _Buffer.Count; }
		}

		/// <summary>
		/// Gets number of the logical lines.
		/// </summary>
		/// <remarks>
		/// Through this property,
		/// number of the logical lines in this document can be retrieved.
		/// "Logical line" here means a string simply separated by EOL codes.
		/// and differs from "physical line" (a text line drawn as a graphc).
		/// To retrieve count of the logical lines,
		/// use <see cref="Sgry.Azuki.IView.LineCount">IView.LineCount</see> or
		/// <see cref="Sgry.Azuki.IUserInterface.LineCount">
		/// IUserInterface.LineCount</see> instead.
		/// </remarks>
		public int LineCount
		{
			get{ return _LHI.Count; }
		}

		/// <summary>
		/// Gets length of the logical line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetLineLength( int lineIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid line index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			int begin, end;

			// get line range
			LineLogic.GetLineRange( _Buffer, _LHI, lineIndex, out begin, out end );

			// return length
			return end - begin;
		}

		/// <summary>
		/// Gets content of the logical line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public string GetLineContent( int lineIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid line index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			int begin, end;
			char[] lineContent;

			// prepare buffer to store line content
			LineLogic.GetLineRange( _Buffer, _LHI, lineIndex, out begin, out end );
			if( end <= begin )
			{
				return String.Empty;
			}
			lineContent = new char[ end-begin ];

			// copy line content
			_Buffer.GetRange( begin, end, ref lineContent );

			return new String( lineContent );
		}

		/// <summary>
		/// Gets content of the logical line without trimming EOL code.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public string GetLineContentWithEolCode( int lineIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid line index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			int begin, end;
			char[] lineContent;

			// prepare buffer to store line content
			LineLogic.GetLineRangeWithEol( _Buffer, _LHI, lineIndex, out begin, out end );
			if( end <= begin )
			{
				return String.Empty;
			}
			lineContent = new char[ end-begin ];
			
			// copy line content
			_Buffer.GetRange( begin, end, ref lineContent );

			return new String( lineContent );
		}

		/// <summary>
		/// Gets text in the range [begin, end).
		/// Note that if given index is at middle of a surrogate pair,
		/// given range will be automatically expanded to avoid dividing the pair.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public string GetTextInRange( int begin, int end )
		{
			if( end < 0 || _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "Invalid index was given (end:"+end+", this.Length:"+Length+")." );
			if( begin < 0 || end < begin )
				throw new ArgumentOutOfRangeException( "begin", "Invalid index was given (begin:"+begin+", end:"+end+", this.Length:"+Length+")." );

			if( begin == end )
			{
				return String.Empty;
			}

			// constrain indexes to avoid dividing surrogate pair
			Utl.ConstrainIndex( _Buffer, ref begin, ref end );
			
			// retrieve a part of the content
			char[] buf = new char[end - begin];
			_Buffer.GetRange( begin, end, ref buf );
			return new String( buf );
		}

		/// <summary>
		/// Gets text in the range [ (fromLineIndex, fromColumnIndex), (toLineIndex, toColumnIndex) ).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public string GetTextInRange( int beginLineIndex, int beginColumnIndex, int endLineIndex, int endColumnIndex )
		{
			if( endLineIndex < 0 || _LHI.Count <= endLineIndex )
				throw new ArgumentOutOfRangeException( "endLineIndex", "Invalid index was given (endLineIndex:"+endLineIndex+", this.Length:"+Length+")." );
			if( beginLineIndex < 0 || endLineIndex < beginLineIndex )
				throw new ArgumentOutOfRangeException( "beginLineIndex", "Invalid index was given (beginLineIndex:"+beginLineIndex+", endLineIndex:"+endLineIndex+")." );
			if( endColumnIndex < 0 )
				throw new ArgumentOutOfRangeException( "endColumnIndex", "Invalid index was given (endColumnIndex:"+endColumnIndex+")." );
			if( beginColumnIndex < 0 )
				throw new ArgumentOutOfRangeException( "beginColumnIndex", "Invalid index was given (beginColumnIndex:"+beginColumnIndex+")." );

			int begin, end;

			// if the specified range is empty, return empty string
			if( beginLineIndex == endLineIndex && beginColumnIndex == endColumnIndex )
			{
				return String.Empty;
			}

			// prepare buffer
			begin = _LHI[beginLineIndex] + beginColumnIndex;
			end = _LHI[endLineIndex] + endColumnIndex;
			if( _Buffer.Count < end )
			{
				throw new ArgumentOutOfRangeException( "?", "Invalid index was given (calculated end:"+end+", this.Length:"+Length+")." );
			}
			if( end <= begin )
			{
				throw new ArgumentOutOfRangeException( "?", String.Format("Invalid index was given (calculated range:[{4}, {5}) / beginLineIndex:{0}, beginColumnIndex:{1}, endLineIndex:{2}, endColumnIndex:{3}", beginLineIndex, beginColumnIndex, endLineIndex, endColumnIndex, begin, end) );
			}

			// copy the portion of content
			return GetTextInRange( begin, end );
		}

		/// <summary>
		/// Gets class of the character at given index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public CharClass GetCharClass( int index )
		{
			if( Length <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", Length:"+Length+")." );

			Debug.Assert( _Buffer.GetCharClassAt(index) != CharClass.Selection, "char at index "+index+" has invalid char class." );
			return _Buffer.GetCharClassAt( index );
		}

		/// <summary>
		/// Sets class of the character at given index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public void SetCharClass( int index, CharClass klass )
		{
			if( Length <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", Length:"+Length+")." );

			_Buffer.SetCharClassAt( index, klass );
		}

		/// <summary>
		/// Replaces current selection.
		/// </summary>
		/// <exception cref="ArgumentNullException">Parameter text is null.</exception>
		public void Replace( string text )
		{
			int begin, end;

			GetSelection( out begin, out end );

			Replace( text, begin, end );
		}

		/// <summary>
		/// Replaces specified range [begin, end) of the content into the given string.
		/// </summary>
		/// <param name="text">specified range will be replaced with this text</param>
		/// <param name="begin">begin index of the range to be replaced</param>
		/// <param name="end">end index of the range to be replaced</param>
		/// <exception cref="ArgumentNullException">Parameter text is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public void Replace( string text, int begin, int end )
		{
			if( begin < 0 || _Buffer.Count < begin )
				throw new ArgumentOutOfRangeException( "begin", "Invalid index was given (begin:"+begin+", this.Length:"+Length+")." );
			if( end < begin || _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "Invalid index was given (end:"+begin+", this.Length:"+Length+")." );
			if( text == null )
				throw new ArgumentNullException( "text" );

			if( _IsReadOnly )
				return;

			string oldText = String.Empty;
			int oldAnchor, anchorDelta;
			int oldCaret, caretDelta;
			EditAction undo;

			// keep copy of the part which will be deleted by this replacement
			if( begin < end )
			{
				char[] oldChars = new char[ end-begin ];
				_Buffer.GetRange( begin, end, ref oldChars );
				oldText = new String( oldChars );
			}

			// keep copy of old caret/anchor index
			oldAnchor = _AnchorIndex;
			oldCaret = _CaretIndex;

			// delete target range
			if( begin < end )
			{
				LineLogic.LHI_Delete( _LHI, _Buffer, begin, end );
				_Buffer.Delete( begin, end );
				if( begin < _CaretIndex )
				{
					_CaretIndex -= end - begin;
					if( _CaretIndex < begin )
						_CaretIndex = begin;
				}
				if( begin < _AnchorIndex )
				{
					_AnchorIndex -= end - begin;
					if( _AnchorIndex < begin )
						_AnchorIndex = begin;
				}
			}

			// then, insert text
			if( 0 < text.Length )
			{
				LineLogic.LHI_Insert( _LHI, _Buffer, text, begin );
				_Buffer.Insert( begin, text.ToCharArray() );
				if( begin <= _CaretIndex )
				{
					_CaretIndex += text.Length;
					if( _Buffer.Count < _CaretIndex ) // _Buffer.Count? really? isn't this "end"?
						_CaretIndex = _Buffer.Count;
				}
				if( begin <= _AnchorIndex )
				{
					_AnchorIndex += text.Length;
					if( _Buffer.Count < _AnchorIndex )
						_AnchorIndex = _Buffer.Count;
				}
			}

			// calc diff of anchor/caret between old and new positions
			anchorDelta = _AnchorIndex - oldAnchor;
			caretDelta = _CaretIndex - oldCaret;

			// calc anchor/caret index in current text
			oldAnchor += anchorDelta;
			oldCaret += caretDelta;

			// stack UNDO history
			if( _IsRecordingHistory )
			{
				undo = new EditAction( this, begin, oldText, text );
				_History.Add( undo );
			}

			Debug.Assert( begin <= Length );

			// cast event
			if( _IsDirty == false )
			{
				_IsDirty = true;
				InvokeDirtyStateChanged();
			}
			InvokeContentChanged( begin, oldText, text );
			InvokeSelectionChanged( oldAnchor, oldCaret );
		}
		#endregion

		#region Editing Behavior
		/// <summary>
		/// Executes UNDO.
		/// </summary>
		public void Undo()
		{
			if( CanUndo )
			{
				EditAction action = _History.GetUndoAction();
				action.Undo();
			}
		}

		/// <summary>
		/// Clears all stacked undo actions.
		/// </summary>
		public void ClearHistory()
		{
			_History.Clear();
		}

		/// <summary>
		/// Executes REDO.
		/// </summary>
		public void Redo()
		{
			if( CanRedo )
			{
				EditAction action = _History.GetRedoAction();
				action.Redo();
			}
		}

		/// <summary>
		/// Gets or sets default EOL Code of this document.
		/// </summary>
		/// <remarks>
		/// This value will be used when an Enter key was pressed,
		/// but setting this property itself does nothing to the content.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Specified EOL code is not supported.</exception>
		public string EolCode
		{
			get{ return _EolCode; }
			set
			{
				if( value != "\r\n" && value != "\r" && value != "\n" )
					throw new InvalidOperationException( "unsupported type of EOL code was set." );
				_EolCode = value;
			}
		}
		#endregion

		#region Index Conversion
		/// <summary>
		/// Gets the index of the first char in the logical line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetLineHeadIndex( int lineIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			return _LHI[ lineIndex ];
		}

		/// <summary>
		/// Gets the index of the first char in the logical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex ) // charIndex can be char-count
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", this.Length:"+Length+")." );

			return LineLogic.GetLineHeadIndexFromCharIndex( _Buffer, _LHI, charIndex );
		}

		/// <summary>
		/// Calculates logical line index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetLineIndexFromCharIndex( int charIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex ) // charIndex can be char-count
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", this.Length:"+Length+")." );

			return LineLogic.GetLineIndexFromCharIndex( _LHI, charIndex );
		}

		/// <summary>
		/// Calculates logical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex ) // charIndex can be char-index
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", this.Length:"+Length+")." );

			LineLogic.GetLineColumnIndexFromCharIndex( _Buffer, _LHI, charIndex, out lineIndex, out columnIndex );
		}

		/// <summary>
		/// Calculates char-index from logical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );
			if( columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Invalid index was given (columnIndex:"+columnIndex+")." );

			int index;

			index = LineLogic.GetCharIndexFromLineColumnIndex( _Buffer, _LHI, lineIndex, columnIndex );
			if( _Buffer.Count < index )
			{
				// strict validation of column index is only done in debug build (for performance)
				// but exceeding buffer size may crash application so checks only that problem
				index = _Buffer.Count;
			}

			return index;
		}
		#endregion

		#region Text Search
		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Index of the first occurrence of the pattern if found, or -1 if not found.</returns>
		/// <exception cref="ArgumentException">parameter end is equal or less than parameter begin.</exception>
		/// <exception cref="ArgumentNullException">parameter value is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">parameter end is greater than character count in this document.</exception>
		public int FindNext( string value, int startIndex )
		{
			return FindNext( value, startIndex, Length, true );
		}

		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="begin">The search starting position.</param>
		/// <param name="end">The search terminating position.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Index of the first occurrence of the pattern if found, or -1 if not found.</returns>
		/// <exception cref="ArgumentException">parameter end is equal or less than parameter begin.</exception>
		/// <exception cref="ArgumentNullException">parameter value is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">parameter end is greater than character count in this document.</exception>
		public int FindNext( string value, int begin, int end, bool matchCase )
		{
			if( end < begin )
				throw new ArgumentException( "parameter end must be greater than parameter begin." );
			if( value == null )
				throw new ArgumentNullException( "value" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end must not be greater than character count. (end:"+end+", Count:"+_Buffer.Count+")" );

			return _Buffer.FindNext( value, begin, end, matchCase );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Index of the first occurrence of the pattern if found, or -1 if not found.</returns>
		/// <exception cref="ArgumentException">parameter end is equal or less than parameter begin.</exception>
		/// <exception cref="ArgumentNullException">parameter value is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">parameter end is greater than character count in this document.</exception>
		public int FindPrev( string value, int startIndex )
		{
			return FindPrev( value, 0, startIndex, true );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Index of the first occurrence of the pattern if found, or -1 if not found.</returns>
		/// <exception cref="ArgumentException">parameter end is equal or less than parameter begin.</exception>
		/// <exception cref="ArgumentNullException">parameter value is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">parameter end is greater than character count in this document.</exception>
		public int FindPrev( string value, int begin, int end, bool matchCase )
		{
			if( end < begin )
				throw new ArgumentException( "parameter end must be greater than parameter begin." );
			if( value == null )
				throw new ArgumentNullException( "value" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end must not be greater than character count. (end:"+end+", Count:"+_Buffer.Count+")" );

			return _Buffer.FindPrev( value, begin, end, matchCase );
		}

		/// <summary>
		/// Finds a text pattern by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <returns>Index of where the pattern was found or -1 if not found</returns>
		/// <remarks>
		/// <para>
		/// This method find a text pattern
		/// expressed by a regular expression in the current content.
		/// If pattern should be searched backward, set
		/// <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft">
		/// RegexOptions.RightToLeft</see>
		/// option to the <paramref name="regex"/> parameter.
		/// </para>
		/// <para>
		/// The text matching process continues for the index
		/// specified with the <paramref name="end"/> parameter
		/// and does not stop at line ends nor null-characters.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentException">parameter end is equal or less than parameter begin.</exception>
		/// <exception cref="ArgumentNullException">parameter regex is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">parameter end is greater than character count in this document.</exception>
		public int FindRegex( Regex regex, int begin, int end )
		{
			if( end < begin )
				throw new ArgumentException( "parameter end must be greater than parameter begin." );
			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end must not be greater than character count. (end:"+end+", Count:"+_Buffer.Count+")" );
			
			return _Buffer.Find( regex, begin, end );
		}
		#endregion

		#region Highlighter
		/// <summary>
		/// Gets or sets highlighter for this document.
		/// Setting null to thie property will disable highlighting.
		/// Note that setting new value to this property will not invalidate graphics.
		/// To update graphic, set value via IUserInterface.Highlighter.
		/// </summary>
		/// <seealso cref="Sgry.Azuki.IUserInterface.Highlighter">IUserInterface.Highlighter</seealso>
		public IHighlighter Highlighter
		{
			get{ return _Highlighter; }
			set
			{
				// clear all highlight information
				_Buffer.ClearCharClasses();

				// associate with new highlighter object and highlight whole content
				_Highlighter = value;
				if( _Highlighter != null )
				{
					_Highlighter.Highlight( this );
				}
			}
		}
		#endregion

		#region Events
		/// <summary>
		/// Occurs when the selection was changed.
		/// </summary>
		public event SelectionChangedEventHandler SelectionChanged;
		void InvokeSelectionChanged( int oldAnchor, int oldCaret )
		{
			if( SelectionChanged != null )
			{
				SelectionChanged(
						this,
						new SelectionChangedEventArgs(oldAnchor, oldCaret)
					);
			}
		}

		/// <summary>
		/// Occurs when the document content was changed.
		/// ContentChangedEventArgs contains the old (replaced) text,
		/// new text, and index indicating the replacement occured.
		/// </summary>
		public event ContentChangedEventHandler ContentChanged;
		void InvokeContentChanged( int index, string oldText, string newText )
		{
			if( ContentChanged != null )
				ContentChanged( this, new ContentChangedEventArgs(index, oldText, newText) );
		}

		/// <summary>
		/// Occurs when IsDirty property has changed.
		/// </summary>
		public event EventHandler DirtyStateChanged;
		void InvokeDirtyStateChanged()
		{
			if( DirtyStateChanged != null )
				DirtyStateChanged( this, EventArgs.Empty );
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets line content enumerator.
		/// </summary>
		public IEnumerator GetEnumerator()
		{
			return _Buffer.GetEnumerator();
		}

		/// <summary>
		/// Gets one character at given index.
		/// </summary>
		public char this[ int index ]
		{
			get{ return _Buffer[index]; }
		}

		/// <summary>
		/// Determines whether given char is a high surrogate or not.
		/// </summary>
		public static bool IsHighSurrogate( char ch )
		{
			return (0xd800 <= ch && ch <= 0xdbff);
		}

		/// <summary>
		/// Determines whether given char is a low surrogate or not.
		/// </summary>
		public static bool IsLowSurrogate( char ch )
		{
			return (0xdc00 <= ch && ch <= 0xdfff);
		}

		internal class Utl
		{
			public static void ConstrainIndex( TextBuffer buf, ref int anchor, ref int caret )
			{
				if( anchor < caret )
				{
					if( IsLowSurrogate(buf[anchor]) )
						anchor--;
					else if( buf[anchor] == '\n'
						&& 0 <= anchor-1 && buf[anchor-1] == '\r' )
						anchor--;
					if( caret < buf.Count && IsLowSurrogate(buf[caret]) )
						caret++;
					else if( caret < buf.Count && buf[caret] == '\n'
						&& 0 <= caret-1 && buf[caret-1] == '\r' )
						caret++;
				}
				else if( caret < anchor )
				{
					if( IsLowSurrogate(buf[caret]) )
						caret--;
					else if( buf[caret] == '\n'
						&& 0 < caret && buf[caret-1] == '\r' )
						caret--;
					if( anchor < buf.Count && IsLowSurrogate(buf[anchor]) )
						anchor++;
					else if( anchor < buf.Count && buf[anchor] == '\n'
						&& 0 <= anchor-1 && buf[anchor-1] == '\r' )
						anchor++;
				}
				else// if( anchor == caret )
				{
					if( anchor < buf.Count )
					{
						if( IsLowSurrogate(buf[anchor])
							|| (buf[anchor] == '\n' && 0 < anchor && buf[anchor-1] == '\r') )
						{
							anchor--;
							caret--;
						}
					}
				}
			}
		}
		#endregion
	}

	#region Types for Events
	/// <summary>
	/// Event handler for SelectionChanged event.
	/// </summary>
	public delegate void SelectionChangedEventHandler( object sender, SelectionChangedEventArgs e );

	/// <summary>
	/// Event information about selection change.
	/// </summary>
	public class SelectionChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public SelectionChangedEventArgs( int anchorIndex, int caretIndex )
		{
			OldAnchor = anchorIndex;
			OldCaret = caretIndex;
		}

		/// <summary>
		/// Anchor index (in current text) of the previous selection.
		/// </summary>
		public int OldAnchor;

		/// <summary>
		/// Offset from new anchor index to old anchor index
		/// (anchor index in old text content can be calculated by "OldAnchorIndex - AnchorDelta").
		/// </summary>
		public int AnchorDelta;

		/// <summary>
		/// Caret index (in current text) of the previous selection.
		/// </summary>
		public int OldCaret;

		/// <summary>
		/// Offset from new caret index to old caret index
		/// (caret index in old text content can be calculated by "OldCaretIndex - CaretDelta").
		/// </summary>
		public int CaretDelta;
	}

	/// <summary>
	/// Event handler for ContentChanged event.
	/// </summary>
	public delegate void ContentChangedEventHandler( object sender, ContentChangedEventArgs e );

	/// <summary>
	/// Event information about content change.
	/// </summary>
	public class ContentChangedEventArgs : EventArgs
	{
		int _Index;
		string _OldText, _NewText;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ContentChangedEventArgs( int index, string oldText, string newText )
		{
			_Index = index;
			_OldText = oldText;
			_NewText = newText;
		}

		/// <summary>
		/// Gets index of the position where the replacement occured.
		/// </summary>
		public int Index
		{
			get{ return _Index; }
		}

		/// <summary>
		/// Gets replaced text.
		/// </summary>
		public string OldText
		{
			get{ return _OldText; }
		}

		/// <summary>
		/// Gets newly inserted text.
		/// </summary>
		public string NewText
		{
			get{ return _NewText; }
		}
	}
	#endregion
}
