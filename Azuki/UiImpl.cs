// file: UiImpl.cs
// brief: User interface logic that independent from platform.
//=========================================================
using System;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	using IHighlighter = Highlighter.IHighlighter;

	/// <summary>
	/// User interface logic that independent from platform.
	/// </summary>
	class UiImpl : IDisposable
	{
		#region Fields
		/// <summary>
		/// Width of default caret graphic.
		/// </summary>
		public const int DefaultCaretWidth = 2;
		const int MaxMatchedBracketSearchLength = 2048;
		internal const int HighlightDelay = 200; // 200[ms]
		IUserInterface _UI;
		View _View = null;
		Document _Document = null;
		ViewType _ViewType = ViewType.Proportional;
		bool _IsDisposed = false;

		IDictionary< uint, ActionProc > _KeyMap = new Dictionary< uint, ActionProc >( 32 );
		AutoIndentHook _AutoIndentHook = null;
		bool _IsOverwriteMode = false;
		bool _UsesTabForIndent = true;
		bool _ConvertsFullWidthSpaceToSpace = false;
		bool _UsesStickyCaret = false;
		bool _IsSingleLineMode = false;

		// X coordinate of this also be used as a flag to determine
		// whether the mouse button is down or not.
		Point _MouseDownVirPos = new Point( Int32.MinValue, 0 );
		bool _MouseDragging = false;
		bool _MouseDragEditing = false;
		Timer _MouseDragEditDelayTimer = null;
		#endregion

		#region Init / Dispose
		public UiImpl( IUserInterface ui )
		{
			_UI = ui;
			_View = new PropView( ui );

			_UI.LineDrawing += UriMarker.Inst.UI_LineDrawing;
		}

		public void Dispose()
		{
			// uninstall document event handlers
			if( Document != null )
			{
				UninstallDocumentEventHandlers( Document );
			}

			// dispose view
			_UI.LineDrawing -= UriMarker.Inst.UI_LineDrawing;
			if( _View != null )
			{
				_View.Dispose();
				_View = null;
			}

			// set disposed flag on
			_IsDisposed = true;
		}
		#endregion

		#region State
		/// <summary>
		/// Gets whether cut action can be executed or not.
		/// </summary>
		public bool CanCut
		{
			get
			{
				if( Document.IsReadOnly )
					return false;
				else
					return CanCopy;
			}
		}

		/// <summary>
		/// Gets whether copy action can be executed or not.
		/// </summary>
		public bool CanCopy
		{
			get
			{
				int begin, end;

				Document.GetSelection( out begin, out end );
				if( begin != end )
				{
					return true; // one or more characters are selected
				}
				else if( UserPref.CopyLineWhenNoSelection )
				{
					return true; // nothing selected
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Gets whether paste action can be executed or not.
		/// </summary>
		public bool CanPaste
		{
			get
			{
				TextDataType dataType;
				string text;

				// always false in read-only mode
				if( Document.IsReadOnly )
					return false;

				// get text from clipboard
				text = Plat.Inst.GetClipboardText( out dataType );

				// there is no text available, paste cannot be done
				return (text != null);
			}
		}
		#endregion

		#region View and Document
		public Document Document
		{
			get{ return _Document; }
			set
			{
				Debug.Assert( _IsDisposed == false );
				if( value == null )
					throw new ArgumentNullException();

				Document prevDoc = _Document;

				// uninstall event handlers
				if( _Document != null )
				{
					UninstallDocumentEventHandlers( Document );
				}

				// replace document
				_Document = value;

				// install event handlers
				InstallDocumentEventHandlers( value );

				// delegate to View
				View.HandleDocumentChanged( prevDoc );

				// redraw graphic
				_UI.Invalidate();
				_UI.UpdateCaretGraphic();
				_UI.UpdateScrollBarRange();
			}
		}

		/// <summary>
		/// Gets or sets the associated view object.
		/// </summary>
		public View View
		{
			get{ return _View; }
		}

		/// <summary>
		/// Gets or sets type of the view.
		/// View type determines how to render text content.
		/// </summary>
		public ViewType ViewType
		{
			get{ return _ViewType; }
			set
			{
				Debug.Assert( _IsDisposed == false );
				View oldView = View;

				// switch to new view object
				switch( value )
				{
					case ViewType.WrappedProportional:
						_View = new PropWrapView( View );
						break;
					//case ViewType.Proportional:
					default:
						_View = new PropView( View );
						break;
				}
				_ViewType = value;

				// dispose old view object
				if( oldView != null )
				{
					oldView.Dispose();
				}

				// re-install event handlers
				// (AzukiControl's event handler MUST be called AFTER view's one)
				if( Document != null )
				{
					UninstallDocumentEventHandlers( Document );
					InstallDocumentEventHandlers( Document );
				}

				// tell new view object that the document object was changed
				View.HandleDocumentChanged( Document );

				// refresh GUI
				_UI.Invalidate();
				if( Document != null )
				{
					_UI.UpdateCaretGraphic();
					_UI.UpdateScrollBarRange();
				}
			}
		}
		#endregion

		#region Behavior and Modes
		/// <summary>
		/// Gets or sets whether the input character overwrites the character at where the caret is on.
		/// </summary>
		public bool IsOverwriteMode
		{
			get{ return _IsOverwriteMode; }
			set
			{
				Debug.Assert( _IsDisposed == false );
				_IsOverwriteMode = value;
				_UI.UpdateCaretGraphic();
				_UI.InvokeOverwriteModeChanged();
			}
		}

		/// <summary>
		/// Gets or sets whether tab characters are used for indentation, instead of space characters.
		/// </summary>
		public bool UsesTabForIndent
		{
			get{ return _UsesTabForIndent; }
			set
			{
				Debug.Assert( _IsDisposed == false );
				_UsesTabForIndent = value;
			}
		}

		/// <summary>
		/// Gets or sets whether to automatically convert
		/// an input full-width space to a space.
		/// </summary>
		public bool ConvertsFullWidthSpaceToSpace
		{
			get{ return _ConvertsFullWidthSpaceToSpace; }
			set
			{
				Debug.Assert( _IsDisposed == false );
				_ConvertsFullWidthSpaceToSpace = value;
			}
		}

		/// <summary>
		/// Gets or sets whether caret behavior is 'sticky' or not.
		/// </summary>
		public bool UsesStickyCaret
		{
			get{ return _UsesStickyCaret; }
			set{ _UsesStickyCaret = value; }
		}

		/// <summary>
		/// Gets or sets whether the content should be limited to a single line.
		/// </summary>
		public bool IsSingleLineMode
		{
			get{ return _IsSingleLineMode; }
			set{ _IsSingleLineMode = value; }
		}

		/// <summary>
		/// Gets or sets whether URIs in the active document
		/// should be marked automatically with built-in URI marker or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Note that built-in URI marker marks URIs in document
		/// and then Azuki shows the URIs as 'looks like URI,'
		/// but (1) clicking mouse button on them, or
		/// (2) pressing keys when the caret is at middle of a URI,
		/// makes NO ACTION BY DEFAULT.
		/// To define action on such event,
		/// programmer must implement such action as a part of
		/// event handler of standard mouse event or keyboard event.
		/// </para>
		/// </remarks>
		public bool MarksUri
		{
			get{ return _Document.MarksUri; }
			set
			{
				_Document.MarksUri = value;

				// force mark URIs on drawing area
				// by invalidating whole area and invoking owner draw events
				_UI.Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets hook delegate to execute auto-indentation.
		/// If null, auto-indentation will not be performed.
		/// </summary>
		/// <seealso cref="AutoIndentHooks">AutoIndentHooks</seealso>
		public AutoIndentHook AutoIndentHook
		{
			get{ return _AutoIndentHook; }
			set
			{
				Debug.Assert( _IsDisposed == false );
				_AutoIndentHook = value;
			}
		}
		#endregion

		#region Key Handling
		public ActionProc GetKeyBind( uint keyCode )
		{
			Debug.Assert( _IsDisposed == false );
			ActionProc proc;

			if( _KeyMap.TryGetValue(keyCode, out proc) == true )
			{
				return proc;
			}
			else
			{
				return null;
			}
		}

		public void SetKeyBind( uint keyCode, ActionProc action )
		{
			Debug.Assert( _IsDisposed == false );

			// remove specified key code from dictionary anyway
			_KeyMap.Remove( keyCode );

			// if it's not null, regist the action
			if( action != null )
			{
				_KeyMap.Add( keyCode, action );
			}
		}

		internal bool IsKeyBindDefined( uint keyCode )
		{
			Debug.Assert( _IsDisposed == false );
			return _KeyMap.ContainsKey( keyCode );
		}

		public void ClearKeyBind()
		{
			Debug.Assert( _IsDisposed == false );
			_KeyMap.Clear();
		}

		/// <summary>
		/// Handles key down event.
		/// </summary>
		public void HandleKeyDown( uint keyData )
		{
			Debug.Assert( _IsDisposed == false );

			ActionProc action = GetKeyBind( keyData );
			if( action != null )
			{
				action( _UI );
			}
		}
		
		/// <summary>
		/// Handles translated character input event.
		/// </summary>
		internal void HandleKeyPress( char ch )
		{
			HandleTextInput( ch.ToString() );
		}

		/// <summary>
		/// Handles text input event.
		/// </summary>
		/// <exception cref="System.InvalidOperationException">This object is already disposed.</exception>
		/// <exception cref="System.ArgumentNullException">Parameter 'text' is null.</exception>
		internal void HandleTextInput( string text )
		{
			if( _IsDisposed )
				throw new InvalidOperationException( "This "+this.GetType().Name+" object is already disposed." );
			if( text == null )
				throw new ArgumentNullException( "text" );

			int newCaretIndex;
			Document doc = Document;
			int selBegin, selEnd;
			StringBuilder input = new StringBuilder( Math.Max(64, text.Length) );
			IGraphics g = null;

			// if in read only mode, just notify and return 
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// ignore if input is an empty text
			if( text.Length == 0 )
			{
				return;
			}

			try
			{
				g = _UI.GetIGraphics();

				// begin grouping UNDO action
				doc.BeginUndo();

				// clear rectangle selection
				if( doc.RectSelectRanges != null )
				{
					doc.DeleteRectSelectText();
				}

				// handle input characters
				foreach( char ch in text )
				{
					// try to use hook delegate
					if( _AutoIndentHook != null
						&& _AutoIndentHook(_UI, ch) == true )
					{
						// Do nothing if this was handled by the hook
						continue;
					}

					// execute built-in hook logic
					if( LineLogic.IsEolChar(ch) )
					{
						// if an EOL code was found, stop consuming and discard
						// following inputs
						if( IsSingleLineMode )
						{
							break;
						}

						// change all EOL code in the text
						input.Append( doc.EolCode );
					}
					else if( ch == '\t' && _UsesTabForIndent == false )
					{
						string spaces = GetTabEquivalentSpaces(_UI,
															   doc.CaretIndex);
						if( spaces == "" )
						{
							// When the caret position is close to next tab
							// stop, an empty string will be the equivalent to
							// a tab. In this case we should think as if the
							// caret is just on the next tab stop.
							for( int i=0; i<View.TabWidth; i++ )
								spaces += ' ';
						}
						input.Append( spaces );
					}
					else if( ch == '\x3000' && ConvertsFullWidthSpaceToSpace )
					{
						input.Append( '\x0020' );
					}
					else
					{
						// remember this character
						input.Append( ch );
					}
				}

				// calculate new caret position
				doc.GetSelection( out selBegin, out selEnd );
				newCaretIndex = selBegin + input.Length;

				// calc replacement target range
				if( IsOverwriteMode
					&& selBegin == selEnd && selEnd+1 < doc.Length
					&& LineLogic.IsEolChar(doc[selBegin]) != true )
				{
					selEnd++;
				}

				// replace selection to input char
				doc.Replace( input.ToString(), selBegin, selEnd );
				doc.SetSelection( newCaretIndex, newCaretIndex );

				// set desired column
				if( UsesStickyCaret == false )
				{
					_View.SetDesiredColumn( g );
				}

				// update graphic
				_View.ScrollToCaret( g );
				//NO_NEED//_View.Invalidate( xxx ); // Doc_ContentChanged will do invalidation well.
			}
			finally
			{
				doc.EndUndo();
				input.Length = 0;
				if( g != null )
				{
					g.Dispose();
				}
			}
		}
		#endregion

		#region Highlighter
		/// <summary>
		/// Gets or sets highlighter for currently active document.
		/// Setting null to this property will disable highlighting.
		/// </summary>
		public IHighlighter Highlighter
		{
			get
			{
				if( Document == null )
					return null;
				else
					return Document.Highlighter;
			}
			set
			{
				Debug.Assert( _IsDisposed == false );
				if( Document == null )
					return;
				
				// switch document's highlighter
				Document.Highlighter = value;

				// then, invalidate view's whole area
				_View.Invalidate();
			}
		}

		internal void ExecHighlighter()
		{
			if( _UI.Document == null )
				return;

			int dirtyBegin, dirtyEnd;
			Document doc = _UI.Document;
			ViewParam param = doc.ViewParam;

			// do nothing unless the document needs to be highlighted
			if( doc.ViewParam.H_IsInvalid == false )
			{
				return;
			}

			// determine where to start and where to end highlighting
			dirtyBegin = Math.Max( 0, param.H_InvalidRangeBegin );
			dirtyEnd = Math.Min( Math.Max(dirtyBegin, param.H_InvalidRangeEnd),
								 doc.Length );
			if( dirtyEnd <= dirtyBegin )
			{
				// If characters at the end of documents was removed, or if 
				// highlighter executed while the document was completely new.
				return;
			}

			// clear the invalid range
			param.H_InvalidRangeBegin = Int32.MaxValue;
			param.H_InvalidRangeEnd = 0;
			param.H_IsInvalid = false;

			// do nothing if no highlighter was set to the active document
			if( doc.Highlighter == null )
			{
				return;
			}

			// highlight
			doc.Highlighter.Highlight( doc, ref dirtyBegin, ref dirtyEnd );

			// remember highlighted range of text
			param.H_ValidRangeBegin = dirtyBegin;
			param.H_ValidRangeEnd = dirtyEnd;
			//DO_NOT//param.H_InvalidRangeBegin = something;
			//DO_NOT//param.H_InvalidRangeEnd = something;

			// then, refresh view
			View.Invalidate( dirtyBegin, dirtyEnd );
		}
		#endregion

		#region Other
		/// <summary>
		/// Gets number of characters currently selected.
		/// </summary>
		/// <returns>Number of characters currently selected.</returns>
		/// <remarks>
		/// <para>
		/// This method gets number of characters currently selected,
		/// properly even if the selection mode is rectangle selection.
		/// </para>
		/// <para>
		/// Note that the difference between the end of selection and the beginning of selection
		/// is not a number of selected characters if they are selected by rectangle selection.
		/// </para>
		/// </remarks>
		public int GetSelectedTextLength()
		{
			Debug.Assert( _IsDisposed == false );

			int count;

			if( Document.RectSelectRanges != null )
			{
				// Get number of characters in each line of the rectangle
				count = 0;
				for( int i=0; i<Document.RectSelectRanges.Length; i+=2 )
				{
					// get this row content
					count += Document.RectSelectRanges[i+1]
						- Document.RectSelectRanges[i];
				}

				return count;
			}
			else
			{
				int begin, end;
				Document.GetSelection( out begin, out end );
				return end - begin;
			}
		}

		/// <summary>
		/// Gets currently selected text.
		/// </summary>
		/// <returns>Currently selected text.</returns>
		/// <remarks>
		/// <para>
		/// This method gets currently selected text.
		/// </para>
		/// <para>
		/// If current selection is rectangle selection,
		/// return value will be a string that are consisted with selected partial lines (rows)
		/// joined with CR+LF.
		/// </para>
		/// </remarks>
		public string GetSelectedText()
		{
			return GetSelectedText( "\r\n" );
		}

		/// <summary>
		/// Gets currently selected text.
		/// </summary>
		/// <returns>Currently selected text.</returns>
		/// <remarks>
		/// <para>
		/// This method gets currently selected text.
		/// </para>
		/// <para>
		/// If current selection is rectangle selection,
		/// return value will be a string that are consisted with selected partial lines (rows)
		/// joined with specified string.
		/// </para>
		/// </remarks>
		public string GetSelectedText( string separator )
		{
			Debug.Assert( _IsDisposed == false );

			if( Document.RectSelectRanges != null )
			{
				StringBuilder text = new StringBuilder();

				// get text in the rect
				for( int i=0; i<Document.RectSelectRanges.Length; i+=2 )
				{
					// get this row content
					string row = Document.GetTextInRange(
							Document.RectSelectRanges[i],
							Document.RectSelectRanges[i+1]
						);
					text.Append( row + separator );
				}

				return text.ToString();
			}
			else
			{
				int begin, end;
				Document.GetSelection( out begin, out end );
				return Document.GetTextInRange( begin, end );
			}
		}
		#endregion

		#region UI Event
		public void HandlePaint( Rectangle clipRect )
		{
			if( _IsDisposed )
				return;

			// draw view graphic
			using( IGraphics g = _UI.GetIGraphics() )
			{
				_View.Paint( g, clipRect );
			}

			// If any characters which were not highlighted after last edit were drawn,
			// expand invalid range to cover the chracters
			// so that the highlighter thread can highlight them on next run
			if( Document != null && Document.Highlighter != null )
			{
				ViewParam vp = Document.ViewParam;
				int end = GetIndexOfLastVisibleCharacter();
				if( vp.H_ValidRangeEnd < end )
				{
					Debug.Assert( 0 <= vp.H_InvalidRangeBegin );
					vp.H_InvalidRangeBegin = Math.Min( vp.H_InvalidRangeBegin,
													   vp.H_ValidRangeEnd );
					vp.H_InvalidRangeEnd = Math.Max( vp.H_InvalidRangeEnd,
													 end );
					vp.H_IsInvalid = true;
					_UI.RescheduleHighlighting();
				}
			}
		}

		public void HandleGotFocus()
		{
			if( _IsDisposed )
				return;

			Debug.Assert( View != null );
			View.HandleGotFocus();
		}

		public void HandleLostFocus()
		{
			if( _IsDisposed )
				return;

			Debug.Assert( View != null );
			View.HandleLostFocus();
			ClearDragState( null );
		}
		#endregion

		#region Mouse Handling
		internal void HandleMouseUp( IMouseEventArgs e )
		{
			if( _IsDisposed )
				return;

			Point pos = e.Location;

			lock( this )
			{
				if( _MouseDragEditing )
				{
					// mouse button was raised during drag-editing
					// so move originally selected text to where the cursor is at
					HandleMouseUp_OnDragEditing( e );
				}
				else if( _MouseDragEditDelayTimer != null )
				{
					// mouse button was raised before entering drag-editing mode.
					// just set caret where the cursor is at
					_MouseDragEditDelayTimer.Dispose();
					View.ScreenToVirtual( ref pos );
					int targetIndex = View.GetIndexFromVirPos( pos );
					Document.SetSelection( targetIndex, targetIndex );
				}
			}
			ClearDragState( pos );
		}

		void HandleMouseUp_OnDragEditing( IMouseEventArgs e )
		{
			int targetIndex;
			Point pos = e.Location;

			// do nothing if the document is read-only
			if( Document.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// calculate target position where the selected text is moved to
			View.ScreenToVirtual( ref pos );
			targetIndex = View.GetIndexFromVirPos( pos );

			// move text
			Document.BeginUndo();
			try
			{
				int begin, end;
				string selText;

				// remove current selection
				Document.GetSelection( out begin, out end );
				selText = Document.GetTextInRange( begin, end );
				Document.Replace( "" );
				if( end <= targetIndex )
					targetIndex -= selText.Length;
				else if( begin <= targetIndex )
					targetIndex = begin;
				/*NO_NEED//
				else
					targetIndex = targetIndex;
				*/

				// insert new text
				Document.Replace( selText, targetIndex, targetIndex );
				Document.SetSelection( targetIndex, targetIndex + selText.Length );
			}
			finally
			{
				Document.EndUndo();
			}
		}

		internal void HandleMouseDown( IMouseEventArgs e )
		{
			if( _IsDisposed )
				return;

			using( IGraphics g = _UI.GetIGraphics() )
			{
				Point pos = e.Location;
				bool onLineNumberArea = false;

				// if mouse-down coordinate is out of window, this is not a normal event so ignore this
				if( pos.X < 0 || pos.Y < 0 )
				{
					return;
				}

				// check whether the mouse position is on the line number area or not
				if( pos.X < View.XofLeftMargin )
				{
					onLineNumberArea = true;
				}

				// remember mouse down screen position and convert it to virtual view's coordinate
				View.ScreenToVirtual( ref pos );
				_MouseDownVirPos = pos;

				if( e.ButtonIndex == 0 ) // left click
				{
					int clickedIndex;
					bool onSelectedText;

					// calculate index of clicked character
					clickedIndex = View.GetIndexFromVirPos( g, pos );

					// determine whether the character is selected or not
					onSelectedText = Document.SelectionManager.IsInSelection(
							clickedIndex
						);

					// set selection
					if( onLineNumberArea )
					{
						//--- line selection ---
						_UI.SelectionMode = TextDataType.Line;
						if( e.Shift )
						{
							//--- expanding line selection ---
							// expand selection to one char next of clicked position
							// (if caret is at head of a line,
							// the line will not be selected by SetSelection.)
							int newCaretIndex = clickedIndex;
							if( newCaretIndex+1 < Document.Length )
							{
								newCaretIndex++;
							}
							Document.SetSelection( Document.AnchorIndex, newCaretIndex, View );
						}
						else
						{
							//--- setting line selection ---
							Document.SetSelection( clickedIndex, clickedIndex, View );
						}
					}
					else if( e.Shift )
					{
						//--- expanding selection ---
						_UI.SelectionMode = (e.Control) ? TextDataType.Words : TextDataType.Normal;
						Document.SetSelection(
								Document.SelectionManager.OriginalAnchorIndex,
								clickedIndex, View
							);
					}
					else if( e.Alt )
					{
						//--- rectangle selection ---
						_UI.SelectionMode = TextDataType.Rectangle;
						Document.SetSelection( clickedIndex, clickedIndex, View );
					}
					else if( e.Control )
					{
						//--- expanding selection ---
						_UI.SelectionMode = TextDataType.Words;
						Document.SetSelection( clickedIndex, clickedIndex, View );
					}
					else if( onSelectedText
						&& Document.RectSelectRanges == null ) // currently dragging rectangle selection is out of support
					{
						//--- starting timer to wait small delay of drag-editing ---
						Debug.Assert( _MouseDragEditDelayTimer == null );
						_MouseDragEditDelayTimer = new Timer(
								_MouseDragEditDelayTimer_Tick, null, 500, Timeout.Infinite
							);
					}
					else
					{
						//--- setting caret ---
						Document.SetSelection( clickedIndex, clickedIndex );
					}
					View.SetDesiredColumn( g );
					View.ScrollToCaret( g );
				}
			}
		}

		internal void HandleDoubleClick( IMouseEventArgs e )
		{
			if( _IsDisposed )
				return;
			if( e.Location.X < 0 || e.Location.Y < 0 )
				return;

			// remember mouse down screen position and convert it to virtual view's coordinate
			_MouseDownVirPos = e.Location;

			// select a word there if it is in the text area
			if( _View.TextAreaRectangle.Contains(_MouseDownVirPos) )
			{
				int clickedIndex;
				Point pos = e.Location;

				View.ScreenToVirtual( ref pos );
				clickedIndex = View.GetIndexFromVirPos( pos );
				_UI.SelectionMode = TextDataType.Words;
				Document.SetSelection( clickedIndex, clickedIndex, View );
			}
		}

		internal void HandleMouseMove( IMouseEventArgs e )
		{
			if( _IsDisposed )
				return;

			Point pos = e.Location;

			// update mouse cursor graphic
			ResetCursorGraphic( pos );

			// if mouse button was not down yet, do nothing
			if( _MouseDownVirPos.X == Int32.MinValue )
			{
				return;
			}

			// make sure that these coordinates are positive value
			pos.X = Math.Max( 0, pos.X );
			pos.Y = Math.Max( 0, pos.Y );
			View.ScreenToVirtual( ref pos );

			// if it was slight movement, ignore
			if( _MouseDragging == false )
			{
				int xOffset = Math.Abs( pos.X - _MouseDownVirPos.X );
				int yOffset = Math.Abs( pos.Y - _MouseDownVirPos.Y );
				if( xOffset <= Plat.Inst.DragSize.Width
					&& yOffset <= Plat.Inst.DragSize.Height )
				{
					return;
				}
			}
			_MouseDragging = true;

			// do drag action
			using( IGraphics g = _UI.GetIGraphics() )
			{
				lock( this )
				{
					if( _MouseDragEditing )
					{
						//--- dragging selected text ---
						int index;
						Rectangle rect = new Rectangle();
						Point alignedPos;

						// calculate position of the char below the mouse cursor
						index = View.GetIndexFromVirPos( pos );
						alignedPos = View.GetVirPosFromIndex( index );
						View.VirtualToScreen( ref alignedPos );

						// display caret graphic at where
						// if text was droped in current state
						rect.Location = alignedPos;
						rect.Height = View.LineHeight;
						rect.Width = DefaultCaretWidth;
						_UI.UpdateCaretGraphic( rect );
					}
					else if( e.ButtonIndex == 0 ) // left button
					{
						if( _MouseDragEditDelayTimer != null )
						{
							//--- cursor was moved before entering drag-editing delay ---
							// stop waiting for the delay and enter drag-editing immediately
							_MouseDragEditDelayTimer.Dispose();
							_MouseDragEditDelayTimer = null;
							_MouseDragEditing = true;
						}
						else
						{
							// expand selection to the character under the cursor
							HandleMouseMove_ExpandSelection( g, pos );
						}
					}
				}
			}
		}

		void HandleMouseMove_ExpandSelection( IGraphics g, Point cursorVirPos )
		{
			Debug.Assert( g != null );
			int curPosIndex;

			// calc index of where the mouse pointer is on
			curPosIndex = View.GetIndexFromVirPos( cursorVirPos );
			if( curPosIndex == -1 )
			{
				return;
			}

			// expand selection to there
			if( _UI.SelectionMode == TextDataType.Rectangle )
			{
				//--- rectangle selection ---
				// expand selection to the point
				Document.SetSelection( Document.AnchorIndex, curPosIndex, View );
			}
			else if( _UI.SelectionMode == TextDataType.Line )
			{
				//--- line selection ---
				// expand selection to one char next of clicked position
				// (if caret is at head of a line,
				// the line will not be selected by SetSelection.)
				int newCaretIndex = curPosIndex;
				if( newCaretIndex+1 < Document.Length )
				{
					newCaretIndex++;
				}
				Document.SetSelection( Document.AnchorIndex, newCaretIndex, View );
			}
			else if( _UI.SelectionMode == TextDataType.Words )
			{
				//--- word selection ---
				Document.SetSelection(
						Document.SelectionManager.OriginalAnchorIndex,
						curPosIndex, View
					);
			}
			else
			{
				//--- normal selection ---
				// expand selection to the point if it was different from previous index
				if( curPosIndex != Document.CaretIndex )
				{
					Document.SetSelection( Document.AnchorIndex, curPosIndex );
				}
			}
			View.SetDesiredColumn( g );
			View.ScrollToCaret( g );
		}

		void ClearDragState( Nullable<Point> cursorScreenPos )
		{
			_MouseDownVirPos.X = Int32.MinValue;
			_MouseDragging = false;
			_UI.SelectionMode = TextDataType.Normal;
			lock( this )
			{
				if( _MouseDragEditDelayTimer != null )
				{
					_MouseDragEditDelayTimer.Dispose();
					_MouseDragEditDelayTimer = null;
				}
				_MouseDragEditing = false;
			}
			_UI.UpdateCaretGraphic();
			ResetCursorGraphic( cursorScreenPos );
		}

		public void ResetCursorGraphic( Nullable<Point> cursorScreenPos )
		{
#			if !PocketPC
			// check state
			bool onLineNumberArea = false;
			bool onHRulerArea = false;
			bool onSelectedText = false;
			MouseCursor? cursorType = null;
			if( cursorScreenPos != null )
			{
				int index;

				if( cursorScreenPos.Value.X < View.XofLeftMargin )
				{
					onLineNumberArea = true;
				}
				else if( cursorScreenPos.Value.Y < View.YofTopMargin )
				{
					onHRulerArea = true;
				}
				else if( Document.RectSelectRanges == null )
				{
					Point virPos = cursorScreenPos.Value;
					View.ScreenToVirtual( ref virPos );
					index = View.GetIndexFromVirPos( virPos );

					onSelectedText = Document.SelectionManager.IsInSelection( index );
					if( onSelectedText == false )
					{
						foreach( int id in Document.GetMarkingsAt(index) )
						{
							MarkingInfo info = Marking.GetMarkingInfo( id );
							if( info.MouseCursor != MouseCursor.IBeam )
							{
								cursorType = info.MouseCursor;
							}
						}
					}
				}
			}

			// set cursor graphic
			if( _MouseDragEditing )
			{
				_UI.SetCursorGraphic( MouseCursor.DragAndDrop );
			}
			else if( _UI.SelectionMode == TextDataType.Rectangle )
			{
				_UI.SetCursorGraphic( MouseCursor.Arrow );
			}
			else if( onLineNumberArea )
			{
				_UI.SetCursorGraphic( MouseCursor.Arrow );
			}
			else if( onHRulerArea )
			{
				_UI.SetCursorGraphic( MouseCursor.Arrow );
			}
			else if( onSelectedText )
			{
				_UI.SetCursorGraphic( MouseCursor.Arrow );
			}
			else if( cursorType.HasValue )
			{
				_UI.SetCursorGraphic( cursorType.Value );
			}
			else
			{
				_UI.SetCursorGraphic( MouseCursor.IBeam );
			}
#			endif
		}

		void _MouseDragEditDelayTimer_Tick( object param )
		{
			lock( this )
			{
				Debug.Assert( _MouseDragEditing == (_MouseDragEditDelayTimer == null) );
				_MouseDragEditing = true;
				_MouseDragEditDelayTimer.Dispose();
				_MouseDragEditDelayTimer = null;
				_UI.SetCursorGraphic( MouseCursor.DragAndDrop );
			}
		}
		#endregion

		#region Event Handlers
		void InstallDocumentEventHandlers( Document doc )
		{
			Debug.Assert( _IsDisposed == false );
			doc.SelectionChanged += Doc_SelectionChanged;
			doc.ContentChanged += Doc_ContentChanged;
			doc.DirtyStateChanged += Doc_DirtyStateChanged;
			_UI.LineDrawing += doc.WatchPatternMarker.UI_LineDrawing;
		}

		void UninstallDocumentEventHandlers( Document doc )
		{
			Debug.Assert( _IsDisposed == false );
			doc.SelectionChanged -= Doc_SelectionChanged;
			doc.ContentChanged -= Doc_ContentChanged;
			doc.DirtyStateChanged -= Doc_DirtyStateChanged;
			_UI.LineDrawing -= doc.WatchPatternMarker.UI_LineDrawing;
		}

		void Doc_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			Debug.Assert( _IsDisposed == false );

			// delegate to view object
			View.HandleSelectionChanged( sender, e );

			// update caret graphic
			_UI.UpdateCaretGraphic();

			// update matched bracket positions unless this event was caused by ContentChanged event
			// (event handler of ContentChanged already handles this)
			if( e.ByContentChanged == false )
			{
				UpdateMatchedBracketPosition();
			}

			// send event to component users
			_UI.InvokeCaretMoved();
		}

		public void Doc_ContentChanged( object sender, ContentChangedEventArgs e )
		{
			Debug.Assert( _IsDisposed == false );

			// delegate to marker objects
			if( _Document.MarksUri )
			{
				UriMarker.Inst.HandleContentChanged( this, e );
			}
			_Document.WatchPatternMarker.HandleContentChanged( this, e );

			// delegate to view object
			View.HandleContentChanged( sender, e );

			// redraw caret graphic
			_UI.UpdateCaretGraphic();

			// update matched bracket positions
			if( e.Index + e.OldText.Length < _Document.ViewParam.IndexOfBracketBeforeCaret )
			{
				_Document.ViewParam.IndexOfBracketBeforeCaret =
					_Document.ViewParam.IndexOfBracketBeforeCaret - e.OldText.Length + e.NewText.Length;
			}
			else if( e.Index <= _Document.ViewParam.IndexOfBracketBeforeCaret )
			{
				_Document.ViewParam.IndexOfBracketBeforeCaret = e.Index + e.NewText.Length;
			}
			if( e.Index + e.OldText.Length < _Document.ViewParam.IndexOfMatchedBracketBeforeCaret )
			{
				_Document.ViewParam.IndexOfMatchedBracketBeforeCaret =
					_Document.ViewParam.IndexOfMatchedBracketBeforeCaret - e.OldText.Length + e.NewText.Length;
			}
			else if( e.Index <= _Document.ViewParam.IndexOfMatchedBracketBeforeCaret )
			{
				_Document.ViewParam.IndexOfMatchedBracketBeforeCaret = e.Index + e.NewText.Length;
			}
			UpdateMatchedBracketPosition();

			// update range of scroll bars
			_UI.UpdateScrollBarRange();

			// update range of text which should be highlighted
			ViewParam param = Document.ViewParam;
			if( e.Index < param.H_InvalidRangeBegin )
			{
				param.H_InvalidRangeBegin = e.Index;
			}
			if( param.H_InvalidRangeEnd < e.Index + e.NewText.Length )
			{
				param.H_InvalidRangeEnd = e.Index + e.NewText.Length;
			}
			param.H_IsInvalid = true;

			// update range of text which should NOT be highlighted until this document was modified
			param.H_ValidRangeEnd = e.Index;
			if( param.H_ValidRangeEnd <= param.H_ValidRangeBegin )
			{
				param.H_ValidRangeBegin = param.H_ValidRangeEnd;
			}

			// start (reset) the timer to run a highlighter after a moment
			_UI.RescheduleHighlighting();
		}

		public void Doc_DirtyStateChanged( object sender, EventArgs e )
		{
			Debug.Assert( _IsDisposed == false );

			// delegate to view object
			View.HandleDirtyStateChanged( sender, e );
		}

		void UpdateMatchedBracketPosition()
		{
			// Reset matched bracket positions
			int oldIbbc = _Document.ViewParam.IndexOfBracketBeforeCaret;
			int oldImbbc = _Document.ViewParam.IndexOfMatchedBracketBeforeCaret;
			_Document.ViewParam.IndexOfBracketBeforeCaret = -1;
			_Document.ViewParam.IndexOfMatchedBracketBeforeCaret = -1;
			if( View.HighlightsMatchedBracket == false )
			{
				return;
			}

			// Find matched brackets
			int newIbbc = _Document.CaretIndex;
			int newImbbc = _Document.FindMatchedBracket( newIbbc, MaxMatchedBracketSearchLength );

			// Update matched bracket positions and graphics
			if( (0 <= newImbbc) != (0 <= oldImbbc) // ON --> OFF, OFF --> ON
				|| (0 <= newIbbc && 0 <= newImbbc) ) // ON --> ON
			{
				// Erase old matched bracket highlight
				if( 0 <= oldIbbc && oldIbbc+1 <= Document.Length )
					View.Invalidate( oldIbbc, oldIbbc+1 );
				if( 0 <= oldImbbc && oldImbbc+1 <= Document.Length )
					View.Invalidate( oldImbbc, oldImbbc+1 );

				// Reraw new matched bracket highlight
				if( 0 <= newIbbc && newIbbc+1 <= Document.Length )
					View.Invalidate( newIbbc, newIbbc+1 );
				if( 0 <= newImbbc && newImbbc+1 <= Document.Length )
					View.Invalidate( newImbbc, newImbbc+1 );

				// Update matched bracket positions
				_Document.ViewParam.IndexOfMatchedBracketBeforeCaret = newImbbc;
				if( 0 <= newImbbc )
					_Document.ViewParam.IndexOfBracketBeforeCaret = newIbbc;
			}
		}
		#endregion

		#region Utilitites
		int GetIndexOfLastVisibleCharacter()
		{
			int lastDrawnLineIndex;
			int visibleLineCount;
			int index;

			// calculate line-index of last visible line
			visibleLineCount = View.VisibleSize.Height / View.LineSpacing;
			lastDrawnLineIndex = View.FirstVisibleLine + visibleLineCount + 1;
			if( View.LineCount <= lastDrawnLineIndex )
			{
				lastDrawnLineIndex = View.LineCount - 1;
			}

			// calculate end index of the line
			if( lastDrawnLineIndex+1 < View.LineCount )
			{
				index = View.GetLineHeadIndex( lastDrawnLineIndex+1 );
			}
			else
			{
				index = Document.Length;
			}

			// return it
			Debug.Assert( 0 <= index && index <= Document.Length );
			return index;
		}

		/// <summary>
		/// Makes an array of spaces which is equivalent of a tab character
		/// in case of inserting it to the position indicated by parameter
		/// 'index'.
		/// </summary>
		internal static string GetTabEquivalentSpaces( IUserInterface ui,
													   int index )
		{
			Document doc = ui.Document;
			View view = (View)ui.View;
			StringBuilder spaces = new StringBuilder( 32 );

			// Calculate next tab stop
			Point insertPos = view.GetVirPosFromIndex( index );
			int nextTabStop = view.NextTabStopX( insertPos.X );

			// make padding spaces
			int spaceCount = (nextTabStop - insertPos.X) / view.SpaceWidthInPx;
			for( int i=0; i<spaceCount; i++ )
			{
				spaces.Append( ' ' );
			}

			return spaces.ToString();
		}

		/// <summary>
		/// Generates appropriate padding characters that fills the gap between
		/// the target position and actual line-end position.
		/// </summary>
		internal static string GetNeededPaddingChars( IUserInterface ui,
													  Point targetVirPos,
													  bool alignTabStop )
		{
			StringBuilder paddingChars;
			int targetIndex;
			Point lineLastCharPos;
			int rightMostTabStopX;
			int neededTabCount = 0;
			int neededSpaceCount;
			IView view = ui.View;

			// calculate the position of the nearest character in line
			// (this will be at end of the line)
			targetIndex = view.GetIndexFromVirPos( targetVirPos );
			lineLastCharPos = view.GetVirPosFromIndex( targetIndex );
			if( targetVirPos.X <= lineLastCharPos.X + view.SpaceWidthInPx )
			{
				return ""; // no padding is needed
			}

			// calculate right most tab stop at left of the target position
			rightMostTabStopX = targetVirPos.X
								- (targetVirPos.X % view.TabWidthInPx);
			if( alignTabStop )
			{
				// to align position to tab stop,
				// set target position to the right most tab stop.
				targetVirPos.X = rightMostTabStopX;
			}

			// calculate how many tabs are needed
			if( ui.UsesTabForIndent )
			{
				int xMax; // x-corrdinate of available right most tab stop
				xMax = lineLastCharPos.X
					   - (lineLastCharPos.X % view.TabWidthInPx);
				neededTabCount = (targetVirPos.X - xMax) / view.TabWidthInPx;
			}

			// calculate how many spaces are needed
			if( 0 < neededTabCount )
			{
				neededSpaceCount = (targetVirPos.X - rightMostTabStopX)
								   / view.SpaceWidthInPx;
			}
			else
			{
				neededSpaceCount = (targetVirPos.X - lineLastCharPos.X)
								   / view.SpaceWidthInPx;
			}

			// pad tabs
			paddingChars = new StringBuilder();
			for( int i=0; i<neededTabCount; i++ )
			{
				paddingChars.Append( '\t' );
			}

			// pad spaces
			for( int i=0; i<neededSpaceCount; i++ )
			{
				paddingChars.Append( ' ' );
			}

			return paddingChars.ToString();
		}
		#endregion
	}
}
