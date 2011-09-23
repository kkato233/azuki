// file: Document.cs
// brief: Document of Azuki engine.
// author: YAMAMOTO Suguru
// update: 2011-09-23
//=========================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Color = System.Drawing.Color;
using Regex = System.Text.RegularExpressions.Regex;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;
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
		TextBuffer _Buffer = new TextBuffer( 4096, 1024 );
		SplitArray<int> _LHI = new SplitArray<int>( 64 ); // line head indexes
		SplitArray<LineDirtyState> _LDS = new SplitArray<LineDirtyState>( 64 ); // line dirty states
		EditHistory _History = new EditHistory();
		SelectionManager _SelMan;
		bool _IsRecordingHistory = true;
		bool _IsSuppressingDirtyStateChangedEvent = false;
		string _EolCode = "\r\n";
		bool _IsReadOnly = false;
		IHighlighter _Highlighter = null;
		IWordProc _WordProc = new DefaultWordProc();
		ViewParam _ViewParam = new ViewParam();
		DateTime _LastModifiedTime = DateTime.Now;
		object _Tag = null;
		static readonly char[] _PairBracketTable = new char[]{
			'(', ')', '{', '}', '[', ']', '<', '>',
			'\xff08', '\xff09', // full-width parenthesis
			'\xff5b', '\xff5d', // full-width curly bracket
			'\xff3b', '\xff3d', // full-width square bracket
			'\xff1c', '\xff1e', // full-width less/greater than sign
			'\x3008', '\x3009', // CJK angle bracket
			'\x300a', '\x300b', // CJK double angle bracket
			'\x300c', '\x300d', // CJK corner bracket
			'\x300e', '\x300f', // CJK white corner bracket
			'\x3010', '\x3011', // CJK black lenticular bracket
			'\x3016', '\x3017', // CJK white lenticular bracket
			'\x3014', '\x3015', // CJK tortoise shell bracket
			'\x3018', '\x3019', // CJK white tortoise shell bracket
			'\x301a', '\x301b', // CJK white square bracket
			'\xff62', '\xff63' // half-width CJK corner bracket
		};
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public Document()
		{
			_SelMan = new SelectionManager( this );

			// initialize LHI
			_LHI.Clear();
			_LHI.Add( 0 );

			// initialize LDS
			_LDS.Clear();
			_LDS.Add( 0 );
		}
		#endregion

		#region States
		/// <summary>
		/// Gets or sets whether any unsaved modifications exist or not.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property will be true if there is any unsaved modifications.
		///   Although Azuki maintains almost all modification history in itself,
		///   it cannot detect when the content was saved
		///   because saving content to file or other means is done outside of it;
		///   done by the application using Azuki.
		///   Because of this, application is responsible to set this property to False
		///   on saving content manually.
		///   </para>
		///   <para>
		///   Note that attempting to set this property True by application code
		///   will raise an InvalidOperationException.
		///   Because any document cannot be turned 'dirty' without modification,
		///   and modification by Document.Replace automatically set this property True
		///   so doing so in application code is not needed.
		///   </para>
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">
		///   True was set as a new value.
		///   - OR -
		///   Modified while grouping UNDO actions.
		/// </exception>
		public bool IsDirty
		{
			get{ return !_History.IsSavedState; }
			set
			{
				if( value == true )
					throw new InvalidOperationException( "Document.IsDirty must not be set True by application code." );

				if( _History.IsGroupingActions )
					throw new InvalidOperationException( "dirty state must not be modified while grouping UNDO actions." );

				if( _History.IsSavedState != value )
					return;

				// clean up dirty state of all modified lines
				for( int i=0; i<_LDS.Count; i++ )
				{
					if( _LDS[i] == LineDirtyState.Dirty )
					{
						_LDS[i] = LineDirtyState.Cleaned;
					}
				}

				// remember current state as lastly saved state
				_History.SetSavedState();

				// invoke event
				InvokeDirtyStateChanged();
			}
		}

		/// <summary>
		/// Gets dirty state of specified line.
		/// </summary>
		/// <param name="lineIndex">Index of the line that to get dirty state of.</param>
		/// <returns>Dirty state of the specified line.</returns>
		/// <remarks>
		///   <para>
		///   This method gets dirty state of specified line.
		///   Dirty state of lines will changed as below.
		///   </para>
		///   <list type="bullet">
		///	    <item>
		///	      If a line was not modified yet, the dirty state of the line is
		///	      <see cref="Sgry.Azuki.LineDirtyState">LineDirtyState</see>.Clean.
		///	    </item>
		///	    <item>
		///	      If a line was modified, its dirty state will be changed to
		///	      <see cref="Sgry.Azuki.LineDirtyState">LineDirtyState</see>.Dirty
		///	    </item>
		///	    <item>
		///	      Setting false to
		///	      <see cref="Sgry.Azuki.Document.IsDirty">Document.IsDirty</see>
		///	      property will set all states of modified lines to
		///	      <see cref="Sgry.Azuki.LineDirtyState">LineDirtyState</see>.Cleaned.
		///	    </item>
		///	    <item>
		///	      Calling
		///	      <see cref="Sgry.Azuki.Document.ClearHistory">Document.ClearHistory</see>
		///	      to reset all states of lines to
		///	      <see cref="Sgry.Azuki.LineDirtyState">LineDirtyState</see>.Clean.
		///	    </item>
		///   </list>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.LineDirtyState">LineDirtyState enum</seealso>
		/// <seealso cref="Sgry.Azuki.Document.IsDirty">Document.IsDirty property</seealso>
		/// <seealso cref="Sgry.Azuki.Document.ClearHistory">Document.ClearHistory method</seealso>
		public LineDirtyState GetLineDirtyState( int lineIndex )
		{
			Debug.Assert( lineIndex <= _LDS.Count );
			Debug.Assert( _LDS.Count == _LHI.Count );
			if( lineIndex < 0 || LineCount < lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "lineIndex param is "+lineIndex+" but must be positive and equal to or less than "+LineCount );

			if( _LDS.Count <= lineIndex )
			{
				return LineDirtyState.Clean;
			}
			return _LDS[lineIndex];
		}

		internal void SetLineDirtyState( int lineIndex, LineDirtyState lds )
		{
			Debug.Assert( lineIndex <= _LDS.Count );
			Debug.Assert( _LDS.Count == _LHI.Count );

			_LDS[lineIndex] = lds;
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
		/// Gets whether an available UNDO action exists or not.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets whether one or more UNDOable action exists or not.
		///   </para>
		///   <para>
		///   To execute UNDO, use <see cref="Sgry.Azuki.Document.Undo">Undo</see> method.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.Undo">Document.Undo method</seealso>
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

		/// <summary>
		/// Gets or sets the size of the internal buffer.
		/// </summary>
		/// <exception cref="System.OutOfMemoryException">There is no enough memory to expand buffer.</exception>
		public int Capacity
		{
			get{ return _Buffer.Capacity; }
			set{ _Buffer.Capacity = value; }
		}

		/// <summary>
		/// Gets the time when this document was last modified.
		/// </summary>
		public DateTime LastModifiedTime
		{
			get{ return _LastModifiedTime; }
		}

		/// <summary>
		/// Gets view specific parameters associated with this document.
		/// </summary>
		/// <remarks>
		///   <para>
		///   There are some parameters that are dependent on each document
		///   but are not parameters about document content.
		///   This property contains such parameters.
		///   </para>
		/// </remarks>
		internal ViewParam ViewParam
		{
			get{ return _ViewParam; }
		}
		#endregion

		#region Selection
		/// <summary>
		/// Gets index of where the caret is at (in char-index).
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets the index of the 'caret;' the text insertion point.
		///   </para>
		///   <para>
		///   In Azuki, selection always exists and is expressed by the range from anchor index to caret index.
		///   If there is nothing selected, it means that both anchor index and caret index is set to same value.
		///   </para>
		///   <para>
		///   To set value of anchor or caret, use
		///   <see cref="Sgry.Azuki.Document.SetSelection(int, int)">Document.SetSelection</see> method.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.AnchorIndex">Document.AnchorIndex Property</seealso>
		/// <seealso cref="Sgry.Azuki.Document.SetSelection(int, int)">Document.SetSelection Method</seealso>
		public int CaretIndex
		{
			get{ return _SelMan.CaretIndex; }
		}

		/// <summary>
		/// Gets index of the position where the selection starts (in char-index).
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets the index of the 'selection anchor;' where the selection starts.
		///   </para>
		///   <para>
		///   In Azuki, selection always exists and is expressed by the range from anchor index to caret index.
		///   If there is nothing selected, it means that both anchor index and caret index is set to same value.
		///   </para>
		///   <para>
		///   To set value of anchor or caret, use
		///   <see cref="Sgry.Azuki.Document.SetSelection(int, int)">Document.SetSelection</see> method.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.CaretIndex">Document.CaretIndex Property</seealso>
		/// <seealso cref="Sgry.Azuki.Document.SetSelection(int, int)">Document.SetSelection Method</seealso>
		public int AnchorIndex
		{
			get{ return _SelMan.AnchorIndex; }
		}

		/// <summary>
		/// Gets selection manager object associated with this document.
		/// </summary>
		internal SelectionManager SelectionManager
		{
			get{ return _SelMan; }
		}

		/// <summary>
		/// Gets caret location by logical line/column index.
		/// </summary>
		/// <param name="lineIndex">line index of where the caret is at</param>
		/// <param name="columnIndex">column index of where the caret is at</param>
		public void GetCaretIndex( out int lineIndex, out int columnIndex )
		{
			GetLineColumnIndexFromCharIndex( _SelMan.CaretIndex, out lineIndex, out columnIndex );
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
				throw new ArgumentOutOfRangeException( "lineIndex", "too large line index was given (given:"+lineIndex+", actual line count:"+_LHI.Count+")" );
			
			int caretIndex = LineLogic.GetCharIndexFromLineColumnIndex( _Buffer, _LHI, lineIndex, columnIndex );
			SetSelection( caretIndex, caretIndex );
		}

		/// <summary>
		/// Sets selection range.
		/// </summary>
		/// <param name="anchor">new index of the selection anchor</param>
		/// <param name="caret">new index of the caret</param>
		/// <exception cref="System.ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <remarks>
		///   <para>
		///   This method sets selection range and invokes
		///   <see cref="Sgry.Azuki.Document.SelectionChanged">Document.SelectionChanged</see> event.
		///   If given index is at middle of an undividable character sequence such as surrogate pair,
		///   selection range will be automatically expanded to avoid dividing the it.
		///   </para>
		///   <para>
		///   This method always selects text as a sequence of character.
		///   To select text by lines or by rectangle, use
		///   <see cref="Sgry.Azuki.Document.SetSelection(int, int, IView)">other overload</see>
		///   method instead.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.SelectionChanged">Document.SelectionChanged event</seealso>
		/// <seealso cref="Sgry.Azuki.Document.SetSelection(int, int, IView)">Document.SetSelection method (another overloaded method)</seealso>
		public void SetSelection( int anchor, int caret )
		{
			SelectionMode = TextDataType.Normal;
			SetSelection( anchor, caret, null );
		}

		/// <summary>
		/// Sets selection range.
		/// </summary>
		/// <param name="anchor">new index of the selection anchor.</param>
		/// <param name="caret">new index of the caret.</param>
		/// <param name="view">a View object to be used for calculating position/index conversion.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <exception cref="System.ArgumentNullException">Parameter 'view' is null but current SelectionMode is not TextDataType.Normal.</exception>
		/// <remarks>
		///   <para>
		///   This method sets selection range and invokes
		///   <see cref="Sgry.Azuki.Document.SelectionChanged">Document.SelectionChanged</see> event.
		///   </para>
		///   <para>
		///   How text will be selected depends on the value of current
		///   <see cref="Sgry.Azuki.Document.SelectionMode">SelectionMode</see> as below.
		///   </para>
		///   <list type="bullet">
		///	    <item>
		///	      <para>
		///	      If SelectionMode is TextDataType.Normal,
		///	      characters from <paramref name="anchor"/> to <paramref name="caret"/>
		///	      will be selected.
		///	      </para>
		///	      <para>
		///	      Note that if given index is at middle of an undividable character sequence such as surrogate pair,
		///	      selection range will be automatically expanded to avoid dividing it.
		///	      </para>
		///	    </item>
		///	    <item>
		///	      <para>
		///	      If SelectionMode is TextDataType.Line, lines between
		///	      the line containing <paramref name="anchor"/> position
		///	      and the line containing <paramref name="caret"/> position
		///	      will be selected.
		///	      </para>
		///	      <para>
		///	      Note that if caret is just at beginning of a line,
		///	      the line will not be selected.
		///	      </para>
		///	    </item>
		///	    <item>
		///	      <para>
		///	      If SelectionMode is TextDataType.Rectangle,
		///	      text covered by the rectangle which is graphically made by
		///	      <paramref name="anchor"/> position and <paramref name="caret"/> position
		///	      will be selected.
		///	      </para>
		///	    </item>
		///   </list>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.SelectionChanged">Document.SelectionChanged event</seealso>
		/// <seealso cref="Sgry.Azuki.Document.SelectionMode">Document.SelectionMode property</seealso>
		/// <seealso cref="Sgry.Azuki.TextDataType">TextDataType enum</seealso>
		public void SetSelection( int anchor, int caret, IView view )
		{
			if( anchor < 0 || _Buffer.Count < anchor )
				throw new ArgumentOutOfRangeException( "anchor", "Parameter 'anchor' is out of valid range (anchor:"+anchor+", caret:"+caret+")." );
			if( caret < 0 || _Buffer.Count < caret )
				throw new ArgumentOutOfRangeException( "caret", "Parameter 'caret' is out of valid range (anchor:"+anchor+", caret:"+caret+")." );
			if( view == null && SelectionMode != TextDataType.Normal )
				throw new ArgumentNullException( "view", "Parameter 'view' must not be null if SelectionMode is not TextDataType.Normal. (SelectionMode:"+SelectionMode+")." );

			_SelMan.SetSelection( anchor, caret, view );
		}

		/// <summary>
		/// Gets range of current selection.
		/// Note that this method does not return [anchor, caret) pair but [begin, end) pair.
		/// </summary>
		/// <param name="begin">index of where the selection begins.</param>
		/// <param name="end">index of where the selection ends (selection do not includes the char at this index).</param>
		public void GetSelection( out int begin, out int end )
		{
			_SelMan.GetSelection( out begin, out end );
		}

		/// <summary>
		/// Gets or sets text ranges selected by rectangle selection.
		/// </summary>
		/// <remarks>
		///   <para>
		///   (This property is basically for internal use only.
		///   Using this method from outside of Azuki assembly is not recommended.)
		///   </para>
		///   <para>
		///   The value of this method is an array of text indexes
		///   that is consisted with beginning index of first text range (row),
		///   ending index of first text range,
		///   beginning index of second text range,
		///   ending index of second text range and so on.
		///   </para>
		/// </remarks>
		public int[] RectSelectRanges
		{
			get{ return _SelMan.RectSelectRanges; }
			set{ _SelMan.RectSelectRanges = value; }
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
		///   <para>
		///   Getting text content through this property
		///   will copy all characters from internal buffer
		///   to a string object and returns it.
		///   </para>
		/// </remarks>
		public string Text
		{
			get
			{
				if( _Buffer.Count == 0 )
					return String.Empty;

				return new String( _Buffer.ToArray() );
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
		/// <param name="index">The word at this index will be retrieved.</param>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public string GetWordAt( int index )
		{
			int begin, end;
			return GetWordAt( index, out begin, out end );
		}

		/// <summary>
		/// Gets a word at specified index.
		/// </summary>
		/// <param name="index">The word at this index will be retrieved.</param>
		/// <param name="begin">The index of the char which starts the word.</param>
		/// <param name="end">The index of where the word ends.</param>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public string GetWordAt( int index, out int begin, out int end )
		{
			if( index < 0 || _Buffer.Count < index ) // index can be equal to char-count
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", this.Length:"+Length+")." );

			// if specified position indicates an empty line, select nothing
			if( IsEmptyLine(index) )
			{
				begin = end = index;
				return String.Empty;
			}

			// ask word processor where the word starting/ending positions are
			begin = WordProc.PrevWordStart( this, index );
			end = WordProc.NextWordEnd( this, index );
			if( begin == end )
			{
				if( Length <= end || Char.IsWhiteSpace(this[end]) )
				{
					if( 0 <= index-1 )
						begin = WordProc.PrevWordStart( this, index-1 );
					else
						begin = 0;
				}
				else
				{
					if( index+1 < Length )
						end = WordProc.NextWordEnd( this, index+1 );
					else
						end = Length;
				}
			}

			// validate result
			if( begin < 0 || end < 0 || end <= begin )
			{
				return String.Empty;
			}

			return GetTextInRange( ref begin, ref end );
		}

		/// <summary>
		/// Gets number of characters currently held in this document.
		/// Note that a surrogate pair or combining characters will be counted as two characters.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property is the number of characters currently held in this document.
		///   Since Azuki stores characters in form of UTF-16,
		///   surrogate pairs or combining characters will not be counted as
		///   "1 character" in this property.
		///   </para>
		/// </remarks>
		public int Length
		{
			get{ return _Buffer.Count; }
		}

		/// <summary>
		/// Gets number of the logical lines.
		/// </summary>
		/// <remarks>
		///   <para>
		///   Through this property,
		///   number of the logical lines in this document can be retrieved.
		///   "Logical line" here means a string simply separated by EOL codes.
		///   and differs from "screen line" (a text line drawn as a graphc).
		///   To retrieve count of the logical lines,
		///   use <see cref="Sgry.Azuki.IView.LineCount">IView.LineCount</see> or
		///   <see cref="Sgry.Azuki.IUserInterface.LineCount">
		///   IUserInterface.LineCount</see> instead.
		///   </para>
		/// </remarks>
		public int LineCount
		{
			get{ return _LHI.Count; }
		}

		/// <summary>
		/// Gets length of the logical line
		/// which contains the specified char-index.
		/// </summary>
		/// <param name="charIndex">Length of the line which contains this index will be retrieved.</param>
		/// <returns>Length of the specified logical line in character count.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <remarks>
		///   <para>
		///   This method retrieves length of logical line.
		///   Note that this method does not count EOL codes.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.GetLineLengthFromCharIndex(int,bool)">Document.GetLineLengthFromCharIndex(int, bool) method</seealso>
		public int GetLineLengthFromCharIndex( int charIndex )
		{
			return GetLineLengthFromCharIndex( charIndex, false );
		}

		/// <summary>
		/// Gets length of the logical line
		/// which contains the specified char-index.
		/// </summary>
		/// <param name="charIndex">Length of the line which contains this index will be retrieved.</param>
		/// <param name="includesEolCode">Whether EOL codes should be count or not.</param>
		/// <returns>Length of the specified logical line in character count.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <remarks>
		///   <para>
		///   This method retrieves length of logical line.
		///   Note that this method does not count EOL codes.
		///   </para>
		/// </remarks>
		public int GetLineLengthFromCharIndex( int charIndex, bool includesEolCode )
		{
			if( _Buffer.Count < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex" );

			int lineIndex = GetLineIndexFromCharIndex( charIndex );
			return GetLineLength( lineIndex, includesEolCode );
		}

		/// <summary>
		/// Gets length of the logical line.
		/// </summary>
		/// <param name="lineIndex">Index of the line of which to get the length.</param>
		/// <returns>Length of the specified line in character count.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <remarks>
		///   <para>
		///   This method retrieves length of logical line.
		///   Note that this method does not count EOL codes.
		///   </para>
		/// </remarks>
		public int GetLineLength( int lineIndex )
		{
			return GetLineLength( lineIndex, false );
		}

		/// <summary>
		/// Gets length of the logical line.
		/// </summary>
		/// <param name="lineIndex">Index of the line of which to get the length.</param>
		/// <param name="includesEolCode">Whether EOL codes should be count or not.</param>
		/// <returns>Length of the specified line in character count.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <remarks>
		///   <para>
		///   This method retrieves length of logical line.
		///   If <paramref name="includesEolCode"/> was true,
		///   this method count EOL code as line content.
		///   </para>
		/// </remarks>
		public int GetLineLength( int lineIndex, bool includesEolCode )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid line index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			int begin, end;

			// get line range
			if( includesEolCode )
				LineLogic.GetLineRangeWithEol( _Buffer, _LHI, lineIndex, out begin, out end );
			else
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
			_Buffer.CopyTo( begin, end, lineContent );

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
			_Buffer.CopyTo( begin, end, lineContent );

			return new String( lineContent );
		}

		/// <summary>
		/// Gets text in the range [begin, end).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <remarks>
		///   <para>
		///   If given index is at middle of an undividable character sequence such as surrogate pair,
		///   given range will be automatically expanded to avoid dividing the pair.
		///   </para>
		///   <para>
		///   If expanded range is needed, use
		///   <see cref="Sgry.Azuki.Document.GetTextInRange(ref int, ref int)">another overload</see>.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.GetTextInRange(ref int, ref int)">Document.GetTextInRange(ref int, ref int) method</seealso>.
		public string GetTextInRange( int begin, int end )
		{
			return GetTextInRange( ref begin, ref end );
		}

		/// <summary>
		/// Gets text in the range [begin, end).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <remarks>
		///   <para>
		///   If given index is at middle of an undividable character sequence such as surrogate pair,
		///   given range will be automatically expanded to avoid dividing the pair.
		///   </para>
		///   <para>
		///   This method returns the expanded range by setting parameter
		///   <paramref name="begin"/> and <paramref name="end"/>
		///   to actually used values.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.GetTextInRange(int, int)">Document.GetTextInRange(int, int) method</seealso>.
		public string GetTextInRange( ref int begin, ref int end )
		{
			if( end < 0 || _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "Invalid index was given (end:"+end+", this.Length:"+Length+")." );
			if( begin < 0 || end < begin )
				throw new ArgumentOutOfRangeException( "begin", "Invalid index was given (begin:"+begin+", end:"+end+", this.Length:"+Length+")." );

			if( begin == end )
			{
				return String.Empty;
			}

			// constrain indexes to avoid dividing a grapheme cluster
			Utl.ConstrainIndex( this, ref begin, ref end );
			
			// retrieve a part of the content
			char[] buf = new char[end - begin];
			_Buffer.CopyTo( begin, end, buf );
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
		/// <param name="index">The index of character which class is to be determined.</param>
		/// <returns>The class of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public CharClass GetCharClass( int index )
		{
			if( Length <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", Length:"+Length+")." );

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
			Debug.Assert( _LHI.Count == _LDS.Count, "LHI.Count("+_LHI.Count+") is not LDS.Count("+_LDS.Count+")" );
			if( begin < 0 || _Buffer.Count < begin )
				throw new ArgumentOutOfRangeException( "begin", "Invalid index was given (begin:"+begin+", this.Length:"+Length+")." );
			if( end < begin || _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "Invalid index was given (begin:"+begin+", end:"+end+", this.Length:"+Length+")." );
			if( text == null )
				throw new ArgumentNullException( "text" );

			string oldText = String.Empty;
			int oldAnchor, anchorDelta;
			int oldCaret, caretDelta;
			int newAnchor, newCaret;
			EditAction undo;
			LineDirtyStateUndoInfo ldsUndoInfo = null;
			int affectedBeginLI = -1;
			bool wasSavedState;

			// first of all, remember current dirty state of the lines
			// which will be modified by this replacement
			wasSavedState = _History.IsSavedState;
			if( _IsRecordingHistory )
			{
				ldsUndoInfo = new LineDirtyStateUndoInfo();

				// calculate range of the lines which will be affectd by this replacement
				affectedBeginLI = GetLineIndexFromCharIndex( begin );
				if( 0 < begin-1 && _Buffer[begin-1] == '\r' )
				{
					if( (0 < text.Length && text[0] == '\n')
						|| (text.Length == 0 && end < _Buffer.Count && _Buffer[end] == '\n') )
					{
						// A new CR+LF will be created by this replacement
						// so one previous line will also be affected.
						affectedBeginLI--;
					}
				}
				int affectedEndLI = GetLineIndexFromCharIndex( end );
				int affectedLineCount = affectedEndLI - affectedBeginLI + 1;
				Debug.Assert( 0 < affectedLineCount );

				// store current state of the lines as 'deleted' history
				ldsUndoInfo.LineIndex = affectedBeginLI;
				ldsUndoInfo.DeletedStates = new LineDirtyState[ affectedLineCount ];
				for( int i=0; i<affectedLineCount; i++ )
				{
					ldsUndoInfo.DeletedStates[i] = _LDS[ affectedBeginLI + i ];
				}
			}

			// keep copy of the part which will be deleted by this replacement
			if( begin < end )
			{
				char[] oldChars = new char[ end-begin ];
				_Buffer.CopyTo( begin, end, oldChars );
				oldText = new String( oldChars );
			}

			// keep copy of old caret/anchor index
			oldAnchor = newAnchor = AnchorIndex;
			oldCaret = newCaret = CaretIndex;

			// delete target range
			if( begin < end )
			{
				// manage line head indexes and delete content
				LineLogic.LHI_Delete( _LHI, _LDS, _Buffer, begin, end );
				_Buffer.RemoveRange( begin, end );

				// manage caret/anchor index
				if( begin < newCaret )
				{
					newCaret -= end - begin;
					if( newCaret < begin )
						newCaret = begin;
				}
				if( begin < newAnchor )
				{
					newAnchor -= end - begin;
					if( newAnchor < begin )
						newAnchor = begin;
				}
			}

			// then, insert text
			if( 0 < text.Length )
			{
				// manage line head indexes and insert content
				LineLogic.LHI_Insert( _LHI, _LDS, _Buffer, text, begin );
				_Buffer.Insert( begin, text.ToCharArray() );

				// manage caret/anchor index
				if( begin <= newCaret )
				{
					newCaret += text.Length;
					if( _Buffer.Count < newCaret ) // this is not "end" but "_Buffer.Count"
						newCaret = _Buffer.Count;
				}
				if( begin <= newAnchor )
				{
					newAnchor += text.Length;
					if( _Buffer.Count < newAnchor )
						newAnchor = _Buffer.Count;
				}
			}

			// calc diff of anchor/caret between old and new positions
			anchorDelta = newAnchor - oldAnchor;
			caretDelta = newCaret - oldCaret;

			// stack UNDO history
			if( _IsRecordingHistory )
			{
				undo = new EditAction( this, begin, oldText, text, oldAnchor, oldCaret, ldsUndoInfo );
				_History.Add( undo );
			}
			_LastModifiedTime = DateTime.Now;

			// convert anchor/caret index in current text
			oldAnchor += anchorDelta;
			oldCaret += caretDelta;

			// update selection
			_SelMan.AnchorIndex = newAnchor;
			_SelMan.CaretIndex = newCaret;

			// examine post assertions
			Debug.Assert( newAnchor <= Length );
			Debug.Assert( newCaret <= Length );
			Debug.Assert( _LHI.Count == _LDS.Count, "LHI.Count("+_LHI.Count+") is not LDS.Count("+_LDS.Count+")" );

			// cast event
			if( _IsSuppressingDirtyStateChangedEvent == false
				&& _History.IsSavedState != wasSavedState )
			{
				InvokeDirtyStateChanged();
			}
			InvokeContentChanged( begin, oldText, text );
			InvokeSelectionChanged( oldAnchor, oldCaret, null, true );
		}
		#endregion

		#region Marking
		/// <summary>
		/// Marks up specified text range.
		/// </summary>
		/// <param name="begin">The index of where the range begins.</param>
		/// <param name="end">The index of where the range ends.</param>
		/// <param name="markingID">ID of marking to be set.</param>
		/// <returns>Whether the operation changed previous marking data or not.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   Parameter <paramref name="begin"/> or <paramref name="end"/> is out of valid range.
		///   - OR - Parameter <paramref name="markingID"/> is out of valid range.
		///	</exception>
		/// <exception cref="System.ArgumentException">
		///   Parameter <paramref name="begin"/> is equal or greater than <paramref name="end"/>.
		///   - OR - Parameter <paramref name="markingID"/> is not registered to Marking class.
		///	</exception>
		/// <remarks>
		///   <para>
		///   This method marks up a range of text with ID of 'marking'.
		///   </para>
		///	  <para>
		///	  For detail of marking feature, please refer to the document of
		///	  <see cref="Sgry.Azuki.Marking"/> class.
		///	  </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.Unmark">Document.Unmark method</seealso>
		/// <seealso cref="Sgry.Azuki.Marking">Marking class</seealso>
		public bool Mark( int begin, int end, int markingID )
		{
			if( begin < 0 || _Buffer.Count < begin )
				throw new ArgumentOutOfRangeException( "begin", "Invalid index was given."
													   + " (begin:" + begin + ","
													   + " this.Length:" + Length + ")" );
			if( end < 0 || _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "Invalid index was given."
													   + " (end:" + end + ","
													   + " this.Length:" + Length + ")" );
			if( end < begin )
				throw new ArgumentException( "Parameter 'begin' must not be greater than 'end'."
											 + " (begin:" + begin + ", end:" + end + ")" );
			if( Marking.GetMarkingInfo(markingID) == null )
				throw new ArgumentException( "Specified marking ID is not registered."
											 + " (markingID:" + markingID + ")",
											 "markingID" );

			Debug.Assert( _Buffer.Marks.Count == this.Length, "sync failed." );

			uint bitMask;
			bool changed = false;

			// store the marking ID in form of bit mask
			bitMask = (uint)( 0x01 << markingID );
			for( int i=begin; i<end; i++ )
			{
				if( (_Buffer.Marks[i] & bitMask) == 0 )
				{
					_Buffer.Marks[i] |= (uint)bitMask;
					changed = true;
				}
			}

			return changed;
		}

		/// <summary>
		/// Removes specified type of marking information at specified range.
		/// </summary>
		/// <param name="begin">The index of where the range begins.</param>
		/// <param name="end">The index of where the range ends.</param>
		/// <param name="markingID">The ID of the marking to be removed.</param>
		/// <returns>Whether any marking data was removed or not.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   Parameter <paramref name="begin"/> or <paramref name="end"/> is out of valid range.
		///   - OR - Parameter <paramref name="markingID"/> is out of valid range.
		///	</exception>
		/// <exception cref="System.ArgumentException">
		///   Parameter <paramref name="begin"/> is equal or greater than <paramref name="end"/>.
		///   - OR - Parameter <paramref name="markingID"/> is not registered to Marking class.
		///	</exception>
		///	<remarks>
		///	  <para>
		///	  This method scans range of [<paramref name="begin"/>, <paramref name="end"/>)
		///	  and removes specified marking ID.
		///	  </para>
		///	  <para>
		///	  For detail of marking feature, please refer to the document of
		///	  <see cref="Sgry.Azuki.Marking"/> class.
		///	  </para>
		///	</remarks>
		/// <seealso cref="Sgry.Azuki.Marking">Marking class</seealso>
		/// <seealso cref="Sgry.Azuki.Document.Mark">Document.Mark method</seealso>
		public bool Unmark( int begin, int end, int markingID )
		{
			if( begin < 0 || _Buffer.Count < begin )
				throw new ArgumentOutOfRangeException( "begin", "Invalid index was given."
													   + " (begin:" + begin
													   + ", this.Length:" + Length + ")" );
			if( end < 0 || _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "Invalid index was given."
													   + " (end:" + end
													   + ", this.Length:" + Length + ")" );
			if( end < begin )
				throw new ArgumentException( "Parameter 'begin' must not be greater than 'end'."
											 + " (begin:" + begin + ", end:" + end + ")" );
			if( Marking.GetMarkingInfo(markingID) == null )
				throw new ArgumentException( "Specified marking ID is not registered."
											 + " (markingID:" + markingID + ")", "markingID" );

			Debug.Assert( _Buffer.Marks.Count == this.Length, "sync failed." );

			uint bitMask;
			bool changed = false;

			// clears bit of the marking
			bitMask = (uint)( 0x01 << markingID );
			for( int i=begin; i<end; i++ )
			{
				if( (_Buffer.Marks[i] & bitMask) != 0 )
				{
					_Buffer.Marks[i] &= (uint)( ~bitMask );
					changed = true;
				}
			}

			return changed;
		}

		/// <summary>
		/// Gets range of text part which includes specified index
		/// which is marked with specified ID.
		/// </summary>
		/// <param name="index">The text range including a character at this index will be retrieved.</param>
		/// <param name="markingID">The text range marked with this ID will be retrieved.</param>
		/// <param name="begin">When this method returns, contains the beginning index of the text range.</param>
		/// <param name="end">When this method returns, contains the ending index of the text range.</param>
		/// <returns>Whether a text range marked with specified marking ID was retrieved or not.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   Parameter <paramref name="index"/> is out of valid range.
		///   - OR - Parameter <paramref name="markingID"/> is out of valid range.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		///   Parameter <paramref name="markingID"/> is not registered in Marking class.
		/// </exception>
		/// <seealso cref="Sgry.Azuki.Marking">Marking class</seealso>
		public bool GetMarkedRange( int index, int markingID, out int begin, out int end )
		{
			if( index < 0 || _Buffer.Count <= index )
				throw new ArgumentOutOfRangeException( "index", "Specified index is out of valid range. (index:"+index+", Document.Length:"+Length+")" );
			if( Marking.GetMarkingInfo(markingID) == null )
				throw new ArgumentException( "Specified marking ID is not registered. (markingID:"+markingID+")", "markingID" );

			uint markingBitMask;

			// make bit mask
			markingBitMask = (uint)( 1 << markingID );
			if( (_Buffer.Marks[index] & markingBitMask) == 0 )
			{
				begin = index;
				end = index;
				return false;
			}

			// seek back until the marking bit was disabled
			begin = index;
			while( 0 <= begin-1
				&& (_Buffer.Marks[begin-1] & markingBitMask) != 0 )
			{
				begin--;
			}

			// seek forward until the marking bit was disabled
			end = index;
			while( end < _Buffer.Count
				&& (_Buffer.Marks[end] & markingBitMask) != 0 )
			{
				end++;
			}

			return true;
		}

		/// <summary>
		/// Gets text part marked with specified ID at specified index.
		/// </summary>
		/// <param name="index">The marked text part at this index will be retrieved.</param>
		/// <param name="markingID">The text part marked with this ID will be retrieved.</param>
		/// <returns>The text if found, otherwise null.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   Parameter <paramref name="index"/> is out of valid range.
		///   - OR - Parameter <paramref name="markingID"/> is out of valid range.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		///   Parameter <paramref name="markingID"/> is not registered in Marking class.
		/// </exception>
		public string GetMarkedText( int index, int markingID )
		{
			if( index < 0 || _Buffer.Count <= index )
				throw new ArgumentOutOfRangeException( "index", "Specified index is out of valid range. (index:"+index+", Document.Length:"+Length+")" );
			if( Marking.GetMarkingInfo(markingID) == null )
				throw new ArgumentException( "Specified marking ID is not registered. (markingID:"+markingID+")", "markingID" );

			int begin, end;
			bool found;

			// get range of the marked text
			found = GetMarkedRange( index, markingID, out begin, out end );
			if( !found )
			{
				return null;
			}

			// extract that range
			return GetTextInRange( begin, end );
		}

		/// <summary>
		/// Determine whether specified index is marked with specified marking ID or not.
		/// </summary>
		/// <param name="index">The index to examine.</param>
		/// <param name="markingID">
		///   Whether specified index is marked with this ID will be retrieved.
		///	</param>
		/// <returns>
		///   Whether a character at <paramref name="index"/> is
		///   marked with <paramref name="markingID"/> or not.
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   Parameter <paramref name="index"/> is out of valid range.
		///   - OR - Parameter <paramref name="markingID"/> is out of valid range.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		///   Parameter <paramref name="markingID"/> is not registered in Marking class.
		/// </exception>
		public bool IsMarked( int index, int markingID )
		{
			if( index < 0 || _Buffer.Count <= index )
				throw new ArgumentOutOfRangeException( "index", "Specified index is out of valid range. (index:"+index+", Document.Length:"+Length+")" );
			if( Marking.GetMarkingInfo(markingID) == null )
				throw new ArgumentException( "Specified marking ID is not registered. (markingID:"+markingID+")", "markingID" );

			uint markingBitMask = (uint)( GetMarkingBitMaskAt(index) & 0xff );
			return ( (markingBitMask >> markingID) & 0x01) != 0;
		}

		/// <summary>
		/// List up all markings at specified index and returns their IDs as an array.
		/// </summary>
		/// <param name="index">The index of the position to examine.</param>
		/// <returns>Array of marking IDs if any marking found, or an empty array if no marking found.</returns>
		/// <remarks>
		///   <para>
		///   This method does not throw exception
		///   but returns an empty array if end index of the document
		///   (index equal to length of document) was specified.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Marking">Marking class</seealso>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   Parameter <paramref name="index"/> is out of valid range.
		/// </exception>
		public int[] GetMarkingsAt( int index )
		{
			if( index < 0 || _Buffer.Count < index )
				throw new ArgumentOutOfRangeException( "index", "Specified index is out of valid range. (index:"+index+", Document.Length:"+Length+")" );

			uint markingBitMask;
			List<int> result = new List<int>( 8 );

			// if specified index is end of document, no marking will be found anyway
			if( _Buffer.Count == index )
			{
				return result.ToArray();
			}

			// get marking bit mask of specified character
			markingBitMask = _Buffer.Marks[ index ];
			if( markingBitMask == 0 )
			{
				return result.ToArray();
			}

			// create an array of marking IDs
			for( int i=0; i<=Marking.MaxID; i++ )
			{
				if( (markingBitMask & 0x01) != 0 )
				{
					result.Add( i );
				}
				markingBitMask >>= 1;
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets marking IDs at specified index as a bit mask (internal representation).
		/// </summary>
		/// <param name="index">The marking IDs put on the character at this index will be returned.</param>
		/// <returns>Bit mask represents markings which covers the character.</returns>
		/// <remarks>
		///   <para>
		///   This method gets a bit-masked integer representing
		///   which marking IDs are put on that position.
		///   </para>
		///	  <para>
		///	  For detail of marking feature, please refer to the document of
		///	  <see cref="Sgry.Azuki.Marking"/> class.
		///	  </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Marking">Marking class</seealso>
		/// <seealso cref="Sgry.Azuki.Document.GetMarkingsAt">Document.GetMarkingsAt method</seealso>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   Parameter <paramref name="index"/> is out of valid range.
		/// </exception>
		public uint GetMarkingBitMaskAt( int index )
		{
			if( index < 0 || _Buffer.Count <= index )
				throw new ArgumentOutOfRangeException( "index", "Specified index is out of valid range. (index:"+index+", Document.Length:"+Length+")" );

			return (uint)_Buffer.Marks[index];
		}

		/// <summary>
		/// Gets or sets whether URIs in this document
		/// should be marked automatically with built-in URI marker or not.
		/// </summary>
		/// <remarks>
		///   <para>
		///   Note that built-in URI marker marks URIs in document
		///   and then Azuki shows the URIs as 'looks like URI,'
		///   but (1) clicking mouse button on them, or
		///   (2) pressing keys when the caret is at middle of a URI,
		///   makes NO ACTION BY DEFAULT.
		///   To define action on such event,
		///   programmer must implement such action as a part of
		///   event handler of standard mouse event or keyboard event.
		///   Please refer to the
		///   <see cref="Sgry.Azuki.Marking">document of marking feature</see> for details.
		///   </para>
		/// </remarks>
		public bool MarksUri
		{
			get{ return _ViewParam.MarksUri; }
			set{ _ViewParam.MarksUri = value; }
		}
		#endregion

		#region Editing Behavior
		/// <summary>
		/// Begins grouping up editing actions into a single UNDO action.
		/// </summary>
		/// <remarks>
		///   <para>
		///   Call of this method creates a new group of actions in UNDO history
		///   and collect modification to this document until call of
		///   <see cref="Sgry.Azuki.Document.EndUndo">EndUndo method</see>.
		///   </para>
		///   <para>
		///   If no actions has been executed between call of BeginUndo and EndUndo,
		///   an UNDO action which do nothing will be stored in UNDO history.
		///   After call of this method, this method does nothing until EndUndo method was called
		///   so calling this method multiple times in a row happens nothing.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.EndUndo">Document.EndUndo method</seealso>
		public void BeginUndo()
		{
			_History.BeginUndo();
		}

		/// <summary>
		/// Ends grouping up editing actions.
		/// </summary>
		/// <remarks>
		///   <para>
		///   Call of this method stops grouping up editing actions.
		///   After call of this method,
		///   this method does nothing until
		///   <see cref="Sgry.Azuki.Document.BeginUndo">BeginUndo</see>.
		///   method was called.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.BeginUndo">Document.BeginUndo method</seealso>
		public void EndUndo()
		{
			_History.EndUndo();
		}

		/// <summary>
		/// Executes UNDO.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This method reverses the effect of lastly done modification to this document.
		///   If there is no UNDOable action, this method will do nothing.
		///   </para>
		///   <para>
		///   To get whether any UNDOable action exists or not,
		///   use <see cref="Sgry.Azuki.Document.CanUndo">CanUndo</see> property.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.CanUndo">Document.CanUndo property</seealso>
		public void Undo()
		{
			// first of all, stop grouping actions
			if( _History.IsGroupingActions )
				EndUndo();

			if( CanUndo == false )
				return;

			bool wasSavedState = _History.IsSavedState;

			// Get the action to be undone.
			EditAction action = _History.GetUndoAction();
			Debug.Assert( action != null );

			// Undo the action.
			// Note that an UNDO may includes multiple actions
			// so executing it may call Document.Replace multiple times.
			// Because Document.Replace also invokes DirtyStateChanged event by itself,
			// designing to make sure Document.Replace called in UNDO is rather complex.
			// So here I use a special flag to supress invoking event in Document.Replace
			// ... to make sure unnecessary events will never be invoked.
			_IsSuppressingDirtyStateChangedEvent = true;
			{
				action.Undo();
			}
			_IsSuppressingDirtyStateChangedEvent = false;

			// Invoke event if this operation
			// changes dirty state of this document
			if( _History.IsSavedState != wasSavedState )
			{
				InvokeDirtyStateChanged();
			}
		}

		/// <summary>
		/// Clears all stacked edit histories.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This method clears all editing histories for
		///   UNDO or REDO action in this document.
		///   </para>
		///   <para>
		///   Note that calling this method will not invalidate graphics.
		///   To update graphic, use IUserInterface.ClearHistory or update manually.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IUserInterface.ClearHistory">IUserInterface.ClearHistory method</seealso>
		public void ClearHistory()
		{
			_History.Clear();
			_History.SetSavedState();
			for( int i=0; i<_LDS.Count; i++ )
			{
				_LDS[i] = LineDirtyState.Clean;
			}
		}

		/// <summary>
		/// Executes REDO.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This method 'replays' the lastly UNDOed action if available.
		///   If there is no REDOable action, this method will do nothing.
		///   </para>
		/// </remarks>
		public void Redo()
		{
			// first of all, stop grouping actions
			// (this must be done before evaluating CanRedo because EndUndo changes it)
			if( _History.IsGroupingActions )
				EndUndo();

			if( CanRedo == false )
				return;

			bool wasSavedState = _History.IsSavedState;

			// Get the action to be done again.
			EditAction action = _History.GetRedoAction();
			Debug.Assert( action != null );

			// Redo the action.
			// Note that an REDO may includes multiple actions
			// so executing it may call Document.Replace multiple times.
			// Because Document.Replace also invokes DirtyStateChanged event by itself,
			// designing to make sure Document.Replace called in REDO is rather complex.
			// So here I use a special flag to supress invoking event in Document.Replace
			// ... to make sure unnecessary events will never be invoked.
			_IsSuppressingDirtyStateChangedEvent = true;
			{
				action.Redo();
			}
			_IsSuppressingDirtyStateChangedEvent = false;

			// Invoke event if this operation
			// changes dirty state of this document
			if( _History.IsSavedState != wasSavedState )
			{
				InvokeDirtyStateChanged();
			}
		}

		/// <summary>
		/// Gets or sets default EOL Code of this document.
		/// </summary>
		/// <exception cref="InvalidOperationException">Specified EOL code is not supported.</exception>
		/// <remarks>
		///   <para>
		///   This value will be used when an Enter key was pressed,
		///   but setting this property itself does nothing to the content.
		///   </para>
		/// </remarks>
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

		/// <summary>
		/// Gets or sets how to select text.
		/// </summary>
		public TextDataType SelectionMode
		{
			get{ return _SelMan.SelectionMode; }
			set{ _SelMan.SelectionMode = value; }
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
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="startIndex"/> is greater than character count in this document.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the first occurrence of the pattern for the range of
		///   [<paramref name="startIndex"/>, EOD) where EOD means the end-of-document.
		///   The text matching process continues for the document end
		///   and does not stop at line ends nor null-characters.
		///   If the search range should end before EOD,
		///   use <see cref="Sgry.Azuki.Document.FindNext(string, int, int)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   This method searches the text pattern case-sensitively.
		///   If the matching should be case-insensitively,
		///   use <see cref="Sgry.Azuki.Document.FindNext(string, int, bool)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   If parameter <paramref name="value"/> is an empty string,
		///   search result will be the range of
		///   [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindNext( string value, int startIndex )
		{
			return FindNext( value, startIndex, _Buffer.Count, true );
		}

		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="begin">The search starting position.</param>
		/// <param name="end">The search terminating position.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="begin"/> or <paramref name="end"/> is
		///   out of valid range.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the first occurrence of the pattern in the range of
		///   [<paramref name="begin"/>, <paramref name="end"/>).
		///   The text matching process continues for the document end
		///   and does not stop at line ends nor null-characters.
		///   </para>
		///   <para>
		///   This method searches the text pattern case-sensitively.
		///   If the matching should be case-insensitively,
		///   use <see cref="Sgry.Azuki.Document.FindNext(string, int, int, bool)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   If parameter <paramref name="value"/> is an empty string,
		///   search result will be the range of
		///   [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindNext( string value, int begin, int end )
		{
			return FindNext( value, begin, end, true );
		}

		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="startIndex"/> is greater than character count in this document.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the first occurrence of the pattern for the range of
		///   [<paramref name="startIndex"/>, EOD) where EOD means the end-of-document.
		///   The text matching process continues for the document end
		///   and does not stop at line ends nor null-characters.
		///   If the search range should end before EOD,
		///   use <see cref="Sgry.Azuki.Document.FindNext(string, int, int, bool)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   If <paramref name="matchCase"/> is true,
		///   the text pattern will be matched case-sensitively
		///   otherwise case will be ignored.
		///   </para>
		///   <para>
		///   If parameter <paramref name="value"/> is an empty string,
		///   search result will be the range of
		///   [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindNext( string value, int startIndex, bool matchCase )
		{
			return FindNext( value, startIndex, _Buffer.Count, matchCase );
		}

		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="begin">The search starting position.</param>
		/// <param name="end">The search terminating position.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="begin"/> or <paramref name="end"/> is
		///   out of valid range.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the first occurrence of the pattern in the range of
		///   [<paramref name="begin"/>, <paramref name="end"/>).
		///   The text matching process continues for the index specified by <paramref name="end"/> parameter
		///   and does not stop at line ends nor null-characters.
		///   </para>
		///   <para>
		///   If <paramref name="matchCase"/> is true,
		///   the text pattern will be matched case-sensitively
		///   otherwise case will be ignored.
		///   </para>
		///   <para>
		///   If parameter <paramref name="value"/> is an empty string,
		///   search result will be the range of [<paramref name="begin"/>, <paramref name="begin"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindNext( string value, int begin, int end, bool matchCase )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", this.Length:"+_Buffer.Count+")" );
			if( value == null )
				throw new ArgumentNullException( "value" );

			return _Buffer.FindNext( value, begin, end, matchCase );
		}

		/// <summary>
		/// Finds a text pattern by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentException">
		///   Parameter <paramref name="regex"/> is a Regex object with RegexOptions.RightToLeft option.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="regex"/> is null.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds a text pattern
		///   expressed by a regular expression in the range of
		///   [<paramref name="startIndex"/>, EOD) where EOD means the end-of-document.
		///   The text matching process continues for the index
		///   specified with the <paramref name="end"/> parameter
		///   and does not stop at line ends nor null-characters.
		///   If the search range should end before EOD,
		///   use <see cref="Sgry.Azuki.Document.FindNext(Regex, int, int)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft">
		///   RegexOptions.RightToLeft</see> option MUST NOT be set to
		///   the Regex object given as parameter <paramref name="regex"/>
		///   otherwise an ArgumentException will be thrown.
		///   </para>
		///   <para>
		///   If an empty string was used for a regular expression pattern,
		///   search result will be the range of [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		///   The text matching process continues for the end of document
		///   and does not stop at line ends nor null-characters.
		///   </para>
		/// </remarks>
		public SearchResult FindNext( Regex regex, int startIndex )
		{
			return FindNext( regex, startIndex, _Buffer.Count );
		}

		/// <summary>
		/// Finds a text pattern by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern to find.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentException">
		///   Parameter <paramref name="regex"/> is a Regex object with RegexOptions.RightToLeft option.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="regex"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="begin"/> or <paramref name="end"/> is out of valid range.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the first ocurrence of a pattern
		///   expressed by a regular expression in the range of
		///   [<paramref name="begin"/>, <paramref name="end"/>).
		///   The text matching process continues for the index
		///   specified with the <paramref name="end"/> parameter
		///   and does not stop at line ends nor null-characters.
		///   </para>
		///   <para>
		///   <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft">
		///   RegexOptions.RightToLeft</see> option MUST NOT be set to
		///   the Regex object given as parameter <paramref name="regex"/>
		///   otherwise an ArgumentException will be thrown.
		///   </para>
		///   <para>
		///   If an empty string was used for a regular expression pattern,
		///   search result will be the range of [<paramref name="begin"/>, <paramref name="begin"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindNext( Regex regex, int begin, int end )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", this.Length:"+_Buffer.Count+")" );
			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( (regex.Options & RegexOptions.RightToLeft) != 0 )
				throw new ArgumentException( "RegexOptions.RightToLeft option must not be set to the object 'regex'.", "regex" );

			return _Buffer.FindNext( regex, begin, end );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="startIndex"/> is out of valid range.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the last occurrence of the pattern in the range
		///   of [0, <paramref name="startIndex"/>).
		///   The text matching process continues for the document head
		///   and does not stop at line ends nor null-characters.
		///   If the search range should end before document head,
		///   use <see cref="Sgry.Azuki.Document.FindPrev(string, int, int)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   This method searches the text pattern case-sensitively.
		///   If the matching should be case-insensitively,
		///   use <see cref="Sgry.Azuki.Document.FindPrev(string, int, bool)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   If parameter <paramref name="value"/> is an empty string,
		///   search result will be the range of [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindPrev( string value, int startIndex )
		{
			return FindPrev( value, 0, startIndex, true );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="startIndex"/> is out of valid range.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the last occurrence of the pattern in the range
		///   of [0, <paramref name="startIndex"/>).
		///   The text matching process continues for the document head
		///   and does not stop at line ends nor null-characters.
		///   If the search range should end before document head,
		///   use <see cref="Sgry.Azuki.Document.FindPrev(string, int, int)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   If <paramref name="matchCase"/> is true,
		///   the text pattern will be matched case-sensitively
		///   otherwise case will be ignored.
		///   </para>
		///   <para>
		///   If parameter <paramref name="value"/> is an empty string,
		///   search result will be the range of [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindPrev( string value, int startIndex, bool matchCase )
		{
			return FindPrev( value, 0, startIndex, matchCase );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="begin"/> or <paramref name="end"/> is out of valid range.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the last occurrence of the pattern in the range
		///   of [<paramref name="begin"/>, <paramref name="end"/>).
		///   The text matching process continues for the document head
		///   and does not stop at line ends nor null-characters.
		///   If the search range should end before document head,
		///   use <see cref="Sgry.Azuki.Document.FindPrev(string, int, int)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   This method searches the text pattern case-sensitively.
		///   If the matching should be case-insensitively,
		///   use <see cref="Sgry.Azuki.Document.FindPrev(string, int, int, bool)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   If parameter <paramref name="value"/> is an empty string,
		///   search result will be the range of [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindPrev( string value, int begin, int end )
		{
			return FindPrev( value, begin, end, true );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="begin"/> or <paramref name="end"/> is out of valid range.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the last occurrence of the pattern in the range of
		///   [<paramref name="begin"/>, <paramref name="end"/>).
		///   The text matching process continues for the index specified by <paramref name="begin"/> parameter
		///   and does not stop at line ends nor null-characters.
		///   </para>
		///   <para>
		///   If <paramref name="matchCase"/> is true,
		///   the text pattern will be matched case-sensitively
		///   otherwise case will be ignored.
		///   </para>
		///   <para>
		///   If parameter <paramref name="value"/> is an empty string,
		///   search result will be a range of [<paramref name="end"/>, <paramref name="end"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindPrev( string value, int begin, int end, bool matchCase )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", this.Length:"+_Buffer.Count+")" );
			if( value == null )
				throw new ArgumentNullException( "value" );

			return _Buffer.FindPrev( value, begin, end, matchCase );
		}

		/// <summary>
		/// Finds a text pattern backward by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentException">
		///   Parameter <paramref name="regex"/> is a Regex object without RegexOptions.RightToLeft option.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="regex"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="startIndex"/> is out of valid range.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the last occurrence of a pattern
		///   expressed by a regular expression in the range of
		///   [0, <paramref name="startIndex"/>).
		///   The text matching process continues for the document head
		///   and does not stop at line ends nor null-characters.
		///   If the search range should end before EOD,
		///   use <see cref="Sgry.Azuki.Document.FindPrev(Regex, int, int)">
		///   other overload method</see>.
		///   </para>
		///   <para>
		///   <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft">
		///   RegexOptions.RightToLeft</see> option MUST be set to
		///   the Regex object given as parameter <paramref name="regex"/>
		///   otherwise an ArgumentException will be thrown.
		///   </para>
		///   <para>
		///   If an empty string was used for a regular expression pattern,
		///   search result will be a range of [<paramref name="end"/>, <paramref name="end"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindPrev( Regex regex, int startIndex )
		{
			return FindPrev( regex, 0, startIndex );
		}

		/// <summary>
		/// Finds a text pattern backward by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentException">
		///   Parameter <paramref name="regex"/> is a Regex object without RegexOptions.RightToLeft option.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		///   Parameter <paramref name="regex"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Parameter <paramref name="begin"/> or <paramref name="end"/> is out of valid range.
		/// </exception>
		/// <remarks>
		///   <para>
		///   This method finds the last occurrence of a pattern
		///   expressed by a regular expression in the range of
		///   [<paramref name="begin"/>, <paramref name="end"/>).
		///   The text matching process continues for the index
		///   specified with the <paramref name="begin"/> parameter
		///   and does not stop at line ends nor null-characters.
		///   </para>
		///   <para>
		///   <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft">
		///   RegexOptions.RightToLeft</see> option MUST be set to
		///   the Regex object given as parameter <paramref name="regex"/>
		///   otherwise an ArgumentException will be thrown.
		///   </para>
		///   <para>
		///   If an empty string was used for a regular expression pattern,
		///   search result will be a range of [<paramref name="end"/>, <paramref name="end"/>).
		///   </para>
		/// </remarks>
		public SearchResult FindPrev( Regex regex, int begin, int end )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", this.Length:"+_Buffer.Count+")" );
			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( (regex.Options & RegexOptions.RightToLeft) == 0 )
				throw new ArgumentException( "RegexOptions.RightToLeft option must be set to the object 'regex'.", "regex" );

			return _Buffer.FindPrev( regex, begin, end );
		}

		/// <summary>
		/// Finds matched bracket from specified index.
		/// </summary>
		/// <param name="index">The index to start searching matched bracket.</param>
		/// <returns>Index of the matched bracket if found. Otherwise -1.</returns>
		/// <remarks>
		///   <para>
		///   This method searches the matched bracket from specified index.
		///   If the character at specified index was not a sort of bracket,
		///   or if specified index points to a character
		///   which has no meaning on grammar (such as comment block, string literal, etc.),
		///   this method returns -1.
		///   </para>
		/// </remarks>
		public int FindMatchedBracket( int index )
		{
			return FindMatchedBracket( index, -1 );
		}

		/// <summary>
		/// Finds matched bracket from specified index.
		/// </summary>
		/// <param name="index">The index to start searching matched bracket.</param>
		/// <param name="maxSearchLength">Maximum number of characters to search matched bracket for.</param>
		/// <returns>Index of the matched bracket if found. Otherwise -1.</returns>
		/// <remarks>
		///   <para>
		///   This method searches the matched bracket from specified index.
		///   If the character at specified index was not a sort of bracket,
		///   or if specified index points to a character
		///   which has no meaning on grammar (such as comment block, string literal, etc.),
		///   this method returns -1.
		///   </para>
		/// </remarks>
		public int FindMatchedBracket( int index, int maxSearchLength )
		{
			if( index < 0 || Length < index )
				throw new ArgumentOutOfRangeException( "index" );

			char bracket, pairBracket;
			bool isOpenBracket = false;
			int depth;

			// if given index is the end position,
			// there is no char at the index so search must be fail
			if( Length == index || IsCDATA(index) )
			{
				return -1;
			}

			// get the bracket and its pair
			bracket = this[index];
			pairBracket = '\0';
			for( int i=0; i<_PairBracketTable.Length; i++ )
			{
				if( bracket == _PairBracketTable[i] )
				{
					if( (i % 2) == 0 )
					{
						// found bracket is an opener. get paired closer
						pairBracket = _PairBracketTable[i+1];
						isOpenBracket = true;
					}
					else
					{
						// found bracket is a closer. get paired opener
						pairBracket = _PairBracketTable[i-1];
						isOpenBracket = false;
					}
					break;
				}
			}
			if( pairBracket == '\0' )
			{
				return -1; // not a bracket.
			}

			// search matched one
			depth = 0;
			if( isOpenBracket )
			{
				// determine search ending position
				int limit = this.Length;
				if( 0 < maxSearchLength )
					limit = Math.Min( this.Length, index+maxSearchLength );

				// search
				for( int i=index; i<limit; i++ )
				{
					// if it is in comment or something that is not a part of "content," ignore it
					if( IsCDATA(i) )
						continue;

					if( this[i] == bracket )
					{
						// found an opener again. increment depth count
						depth++;
					}
					else if( this[i] == pairBracket )
					{
						// found an closer. decrement depth count
						depth--;
						if( depth == 0 )
						{
							return i; // depth count reset by this char; this is the pair
						}
					}
				}
			}
			else
			{
				// determine search ending position
				int limit = 0;
				if( 0 < maxSearchLength )
					limit = Math.Max( 0, index-maxSearchLength );

				// search
				for( int i=index; limit<=i; i-- )
				{
					// if it is in comment or something that is not a part of "content," ignore it
					if( IsCDATA(i) )
						continue;

					if( this[i] == bracket )
					{
						// found an closer again. increment depth count
						depth++;
					}
					else if( this[i] == pairBracket )
					{
						// found an opener. decrement depth count
						depth--;
						if( depth == 0 )
						{
							return i; // depth count reset by this char; this is the pair
						}
					}
				}
			}

			// not found
			return -1;
		}
		#endregion

		#region Highlighter and word processor
		/// <summary>
		/// Gets or sets highlighter object to highlight currently active document
		/// or null to disable highlighting.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets or sets highlighter for this document.
		///   </para>
		///   <para>
		///   Highlighter objects are used to highlight syntax of documents.
		///   They implements
		///   <see cref="Sgry.Azuki.Highlighter.IHighlighter">IHighlighter</see>
		///   interface and called
		///   <see cref="Sgry.Azuki.Highlighter.IHighlighter.Highlight(Sgry.Azuki.Document, ref int, ref int)">Highlight</see>
		///   method every time slightly after user input stopped to execute own highlighting logic.
		///   If null was set to this property, highlighting feature will be disabled.
		///   </para>
		///   <para>
		///   Azuki provides some built-in highlighters. See
		///   <see cref="Sgry.Azuki.Highlighter.Highlighters">Highlighter.Highlighters</see>
		///   class members.
		///   </para>
		///   <para>
		///   User can create and use custom highlighter object.
		///   If you want to create a keyword-based highlighter,
		///   you can extend
		///   <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter">KeywordHighlighter</see>.
		///   If you want to create not a keyword based one,
		///   create a class which implements
		///   <see cref="Sgry.Azuki.Highlighter.IHighlighter">IHighlighter</see>
		///   and write your own highlighting logic.
		///   </para>
		///   <para>
		///   Note that setting new value to this property will not invalidate graphics.
		///   To update graphic, set value via IUserInterface.Highlighter.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IUserInterface.Highlighter">IUserInterface.Highlighter</seealso>
		public IHighlighter Highlighter
		{
			get{ return _Highlighter; }
			set
			{
				// clear all highlight information
				_Buffer.ClearCharClasses();

				// associate with new highlighter object
				_Highlighter = value;

				// clear highlighter related parameters
				ViewParam.H_InvalidRangeBegin = Int32.MaxValue;
				ViewParam.H_InvalidRangeEnd = Int32.MinValue;
				ViewParam.H_ValidRangeBegin = 0;
				ViewParam.H_ValidRangeEnd = 0;
			}
		}

		/// <summary>
		/// Gets whether the character at specified index
		/// is just a data without meaning on grammar.
		/// </summary>
		/// <param name="index">The index of the character to examine.</param>
		/// <returns>Whether the character is part of a character data or not.</returns>
		/// <remarks>
		///   <para>
		///   This method gets whether the character at specified index
		///   is just a character data without meaning on grammar.
		///   'Character data' here means text data which is not a part of the grammar.
		///   Example of character data is comment or string literal in programming languages.
		///   </para>
		/// </remarks>
		public bool IsCDATA( int index )
		{
			CharClass klass;

			klass = GetCharClass( index );
			return ( klass == CharClass.AttributeValue
					|| klass == CharClass.CDataSection
					|| klass == CharClass.Character
					|| klass == CharClass.Comment
					|| klass == CharClass.DocComment
					|| klass == CharClass.Regex
					|| klass == CharClass.String
				);
		}

		/// <summary>
		/// Gets or sets word processor object which determines how Azuki handles 'words.'
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets or sets word processor object.
		///   Please refer to the document of IWordProc interface for detail.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IWordProc">IWordProc interface</seealso>
		/// <seealso cref="Sgry.Azuki.DefaultWordProc">DefaultWordProc class</seealso>
		public IWordProc WordProc
		{
			get{ return _WordProc; }
			set
			{
				if( value == null )
					value = new DefaultWordProc();
				_WordProc = value;
			}
		}
		#endregion

		#region Events
		/// <summary>
		/// Occurs when the selection was changed.
		/// </summary>
		public event SelectionChangedEventHandler SelectionChanged;
		internal void InvokeSelectionChanged( int oldAnchor, int oldCaret, int[] oldRectSelectRanges, bool byContentChanged )
		{
#			if DEBUG
			Debug.Assert( 0 <= oldAnchor );
			Debug.Assert( 0 <= oldCaret );
			if( oldRectSelectRanges != null )
			{
				Debug.Assert( oldRectSelectRanges.Length % 2 == 0 );
			}
#			endif

			if( SelectionChanged != null )
			{
				SelectionChanged(
						this,
						new SelectionChangedEventArgs(oldAnchor, oldCaret, oldRectSelectRanges, byContentChanged)
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
			Debug.Assert( 0 <= index );
			Debug.Assert( oldText != null );
			Debug.Assert( newText != null );

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

		/// <summary>
		/// Occures soon after selection mode was changed.
		/// </summary>
		public event EventHandler SelectionModeChanged;
		internal void InvokeSelectionModeChanged()
		{
			if( SelectionModeChanged != null )
			{
				SelectionModeChanged( this, EventArgs.Empty );
			}
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets or sets an object associated with this document.
		/// </summary>
		public object Tag
		{
			get{ return _Tag; }
			set{ _Tag = value; }
		}

		/// <summary>
		/// Gets index of next grapheme cluster.
		/// </summary>
		/// <param name="index">The index to start the search from.</param>
		/// <returns>The index of the character which starts next grapheme cluster.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">Parameter '<paramref name="index"/>' is out of valid range.</exception>
		/// <remarks>
		///   <para>
		///   This method searches document for a grapheme cluster
		///   from given <paramref name="index"/> forward.
		///   Note that this method always return an index greater than given '<paramref name="index"/>'.
		///   </para>
		///   <para>
		///   'Grapheme cluster' is a sequence of characters
		///   which consists one 'user perceived character'
		///   such as sequence of U+0041 and U+0300; a capital 'A' with grave (&#x0041;&#x0300;).
		///   In most cases, such sequence should not be divided unless user wishes to do so.
		///   </para>
		///   <para>
		///   This method determines an index pointing the middle of character sequences next as undividable:
		///   </para>
		///   <list type="bullet">
		///     <item>CR+LF</item>
		///     <item>Surrogate pair</item>
		///     <item>Combining character sequence</item>
		///   </list>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.PrevGraphemeClusterIndex">Document.PrevGraphemeClusterIndex method</seealso>
		/// <seealso cref="Sgry.Azuki.Document.IsNotDividableIndex(int)">Document.IsNotDividableIndex method</seealso>
		public int NextGraphemeClusterIndex( int index )
		{
			if( index < 0 || Length < index )
				throw new ArgumentOutOfRangeException( "index" );

			do
			{
				index++;
			}
			while( index < Length && IsNotDividableIndex(index) );

			return index;
		}

		/// <summary>
		/// Gets index of previous grapheme cluster.
		/// </summary>
		/// <param name="index">The index to start the search from.</param>
		/// <returns>The index of the character which starts previous grapheme cluster.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">Parameter '<paramref name="index"/>' is out of valid range.</exception>
		/// <remarks>
		///   <para>
		///   This method searches document for a grapheme cluster
		///   from given <paramref name="index"/> backward.
		///   Note that this method always return an index less than given '<paramref name="index"/>'.
		///   </para>
		///   <para>
		///   'Grapheme cluster' is a sequence of characters
		///   which consists one 'user perceived character'
		///   such as sequence of U+0041 and U+0300; a capital 'A' with grave (&#x0041;&#x0300;).
		///   In most cases, such sequence should not be divided unless user wishes to do so.
		///   </para>
		///   <para>
		///   This method determines an index pointing the middle of character sequences next as undividable:
		///   </para>
		///   <list type="bullet">
		///     <item>CR+LF</item>
		///     <item>Surrogate pair</item>
		///     <item>Combining character sequence</item>
		///   </list>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.PrevGraphemeClusterIndex">Document.PrevGraphemeClusterIndex method</seealso>
		/// <seealso cref="Sgry.Azuki.Document.IsNotDividableIndex(int)">Document.IsNotDividableIndex method</seealso>
		public int PrevGraphemeClusterIndex( int index )
		{
			if( index < 0 || Length < index )
				throw new ArgumentOutOfRangeException( "index" );

			do
			{
				index--;
			}
			while( 0 < index && IsNotDividableIndex(index) );

			return index;
		}

		/// <summary>
		/// Gets content enumerator.
		/// </summary>
		public IEnumerator GetEnumerator()
		{
			return _Buffer.GetEnumerator();
		}

		/// <summary>
		/// Gets estimated memory size used by this document.
		/// </summary>
		public int MemoryUsage
		{
			get
			{
				int usage = 0;
				usage += _Buffer.Capacity * ( sizeof(char) + sizeof(CharClass) );
				usage += _LHI.Capacity * sizeof(int);
				usage += _LDS.Capacity * sizeof(LineDirtyState);
				usage += _History.MemoryUsage;
				return usage;
			}
		}

		/// <summary>
		/// Gets one character at given index.
		/// </summary>
		public char this[ int index ]
		{
			get
			{
				Debug.Assert( 0 <= index && index < Length, "Document.this[int] needs a valid index (given index:"+index+", this.Length:"+Length+")" );
				return _Buffer[index];
			}
		}

		/// <summary>
		/// Determines whether text can not be divided at given index or not.
		/// </summary>
		/// <param name="index">The index to determine whether it points to middle of an undividable character sequence or not.</param>
		/// <returns>Whether charcter sequence can not be divided at the index or not.</returns>
		/// <remarks>
		///   <para>
		///   This method determines whether text can not be divided at given index or not.
		///   To seek document through grapheme cluster by grapheme cluster,
		///   please consider to use
		///   <see cref="Sgry.Azuki.Document.NextGraphemeClusterIndex">Document.NextGraphemeClusterIndex method</see>
		///   or
		///   <see cref="Sgry.Azuki.Document.PrevGraphemeClusterIndex">Document.PrevGraphemeClusterIndex method</see>.
		///   </para>
		///   <para>
		///   This method determines an index pointing the middle of character sequences next as undividable:
		///   </para>
		///   <para>
		///   'Grapheme cluster' is a sequence of characters
		///   which consists one 'user perceived character'
		///   such as sequence of U+0041 and U+0300; a capital 'A' with grave (&#x0041;&#x0300;).
		///   In most cases, such sequence should not be divided unless user wishes to do so.
		///   </para>
		///   <list type="bullet">
		///     <item>CR+LF</item>
		///     <item>Surrogate pair</item>
		///     <item>Combining character sequence</item>
		///   </list>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.NextGraphemeClusterIndex">Document.NextGraphemeClusterIndex method</seealso>
		/// <seealso cref="Sgry.Azuki.Document.PrevGraphemeClusterIndex">Document.PrevGraphemeClusterIndex method</seealso>
		public bool IsNotDividableIndex( int index )
		{
			if( index <= 0 || Length <= index )
				return false;

			return Document.IsNotDividableIndex( this[index-1], this[index] );
		}

		/// <summary>
		/// Determines whether text can not be divided at given index or not.
		/// </summary>
		/// <param name="text">The text to be examined.</param>
		/// <param name="index">The index to determine whether it points to middle of an undividable character sequence or not.</param>
		/// <remarks>
		///   <para>
		///   This method determines whether a string can not be divided at given index or not.
		///   This is only an utility method.
		///   Please refer to the document of
		///   <see cref="Sgry.Azuki.Document.IsNotDividableIndex(int)">Document.IsNotDividableIndex instance method</see>
		///   for detail.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.IsNotDividableIndex(int)">Document.IsNotDividableIndex method</seealso>
		public static bool IsNotDividableIndex( string text, int index )
		{
			if( text == null || index <= 0 || text.Length <= index )
				return false;

			return IsNotDividableIndex( text[index-1], text[index] );
		}

		/// <summary>
		/// Determines whether text can not be divided at given index or not.
		/// </summary>
		static bool IsNotDividableIndex( char prevCh, char ch )
		{
			if( prevCh == '\r' && ch == '\n' )
			{
				return true;
			}
			if( IsHighSurrogate(prevCh) && IsLowSurrogate(ch) )
			{
				return true;
			}
			if( IsCombiningCharacter(ch) && LineLogic.IsEolChar(prevCh) == false )
			{
				return true;
			}

			return false;
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

		/// <summary>
		/// Determines whether given character is a combining character or not.
		/// </summary>
		public bool IsCombiningCharacter( int index )
		{
			if( index < 0 || Length <= index )
				return false;

			return IsCombiningCharacter( this[index] );
		}

		/// <summary>
		/// Determines whether given character is a combining character or not.
		/// </summary>
		public static bool IsCombiningCharacter( string text, int index )
		{
			if( index < 0 || text.Length <= index )
				return false;

			return IsCombiningCharacter( text[index] );
		}

		/// <summary>
		/// Determines whether given character is a combining character or not.
		/// </summary>
		public static bool IsCombiningCharacter( char ch )
		{
			UnicodeCategory category = Char.GetUnicodeCategory( ch );
			return ( category == UnicodeCategory.NonSpacingMark
					|| category == UnicodeCategory.SpacingCombiningMark
					|| category == UnicodeCategory.EnclosingMark
				);
		}

		/// <summary>
		/// Returnes whether the index points to one of the paired matching bracket or not.
		/// Note that matching bracket position is not maintaned by Document but by UiImpl.
		/// </summary>
		internal bool IsMatchedBracket( int index )
		{
			Debug.Assert( 0 <= index && index < Length );

			if( index == ViewParam.MatchedBracketIndex2
				|| index == ViewParam.MatchedBracketIndex1 )
			{
				return true;
			}
			return false;
		}

		internal void DeleteRectSelectText()
		{
			int diff = 0;

			for( int i=0; i<RectSelectRanges.Length; i+=2 )
			{
				// recalculate range of this row
				RectSelectRanges[i] -= diff;
				RectSelectRanges[i+1] -= diff;

				// replace this row
				Debug.Assert( IsNotDividableIndex(RectSelectRanges[i]) == false );
				Debug.Assert( IsNotDividableIndex(RectSelectRanges[i+1]) == false );
				Replace( String.Empty,
						RectSelectRanges[i],
						RectSelectRanges[i+1]
					);

				// go to next row
				diff += RectSelectRanges[i+1] - RectSelectRanges[i];
			}

			// reset selection
			SetSelection( RectSelectRanges[0], RectSelectRanges[0] );
		}

		internal class Utl
		{
			public static void ConstrainIndex( Document doc, ref int anchor, ref int caret )
			{
				if( anchor < caret )
				{
					while( doc.IsNotDividableIndex(anchor) )
						anchor--;
					while( doc.IsNotDividableIndex(caret) )
						caret++;
				}
				else if( caret < anchor )
				{
					while( doc.IsNotDividableIndex(caret) )
						caret--;
					while( doc.IsNotDividableIndex(anchor) )
						anchor++;
				}
				else// if( anchor == caret )
				{
					while( doc.IsNotDividableIndex(caret) )
					{
						anchor--;
						caret--;
					}
				}
			}

			public static bool IsLineHead( Document doc, IView view, int index )
			{
				DebugUtl.Assert( doc != null );
				DebugUtl.Assert( view != null );

				if( index < 0 )
				{
					return false;
				}
				else if( index == 0 )
				{
					return true;
				}
				else if( index < doc.Length )
				{
					int lineHeadIndex = view.GetLineHeadIndexFromCharIndex( index );
					return (lineHeadIndex == index);
				}
				else
				{
					return false;
				}
			}
		}

		bool IsEmptyLine( int index )
		{
			// is the index indicates end of the document or end of a line?
			if( index == Length
				|| index < Length && LineLogic.IsEolChar(this[index]))
			{
				// is the index indicates start of the document or start of a line?
				if( index == 0
					|| 0 <= index-1 && LineLogic.IsEolChar(this[index-1]) )
				{
					return true;
				}
			}
			return false;
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
		int _OldAnchor;
		int _OldCaret;
		int[] _OldRectSelectRanges;
		bool _ByContentChanged;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public SelectionChangedEventArgs( int anchorIndex, int caretIndex, int[] oldRectSelectRanges, bool byContentChanged )
		{
			_OldAnchor = anchorIndex;
			_OldCaret = caretIndex;
			_OldRectSelectRanges = oldRectSelectRanges;
			_ByContentChanged = byContentChanged;
		}

		/// <summary>
		/// Anchor index (in current text) of the previous selection.
		/// </summary>
		public int OldAnchor
		{
			get{ return _OldAnchor; }
		}

		/// <summary>
		/// Caret index (in current text) of the previous selection.
		/// </summary>
		public int OldCaret
		{
			get{ return _OldCaret; }
		}

		/// <summary>
		/// Text ranges selected by previous rectangle selection (indexes are valid in current text.)
		/// </summary>
		public int[] OldRectSelectRanges
		{
			get{ return _OldRectSelectRanges; }
		}

		/// <summary>
		/// This value will be true if this event has been occured because the document was modified.
		/// </summary>
		public bool ByContentChanged
		{
			get{ return _ByContentChanged; }
		}
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
		int _RedrawStartIndex, _RedrawEndIndex;

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

		/// <summary>
		/// Gets or sets starting index of the range to be redrawn after this event.
		/// </summary>
		public int RedrawStartIndex
		{
			get{ return _RedrawStartIndex; }
			set{ _RedrawStartIndex = value; }
		}

		/// <summary>
		/// Gets or sets ending index of the range to be redrawn after this event.
		/// </summary>
		public int RedrawEndIndex
		{
			get{ return _RedrawEndIndex; }
			set{ _RedrawEndIndex = value; }
		}
	}
	#endregion
}
