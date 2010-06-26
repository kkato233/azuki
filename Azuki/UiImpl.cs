// file: UiImpl.cs
// brief: User interface logic that independent from platform.
// author: YAMAMOTO Suguru
// update: 2010-06-26
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
		const int HighlightInterval1 = 250;
#		if PocketPC
		const int HighlightInterval2 = 500;
#		else
		const int HighlightInterval2 = 350;
#		endif
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

		// X coordinate of this also be used as a flag to determine
		// whether the mouse button is down or not.
		Point _MouseDownVirPos = new Point( Int32.MinValue, 0 );
		bool _MouseDragging = false;

		Thread _HighlighterThread;
		bool _ShouldBeHighlighted = false;
		int _DirtyRangeBegin = -1;
		int _DirtyRangeEnd = -1;
		#endregion

		#region Init / Dispose
		public UiImpl( IUserInterface ui )
		{
			_UI = ui;
			_View = new PropView( ui );

			_HighlighterThread = new Thread( HighlighterThreadProc );
			_HighlighterThread.Priority = ThreadPriority.BelowNormal;
			_HighlighterThread.Start();
		}

#		if DEBUG
		~UiImpl()
		{
			// dispose highlighter
			if( _HighlighterThread != null )
			{
				bool timedOut = !( _HighlighterThread.Join(1000) );
				if( timedOut )
				{
					_HighlighterThread.Abort();
				}
				_HighlighterThread = null;
			}
		}
#		endif

		public void Dispose()
		{
			// uninstall document event handlers
			if( Document != null )
			{
				UninstallDocumentEventHandlers( Document );
			}

			// dispose view
			if( _View != null )
			{
				_View.Dispose();
				_View = null;
			}

			// set disposed flag on
			_IsDisposed = true;
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
				if( Document != null )
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
						// if this char was handled by the hook, do nothing for this char.
						continue;
					}

					// execute built-in hook logic
					if( LineLogic.IsEolChar(ch) )
					{
						// change all EOL code in the text should changed to the
						input.Append( doc.EolCode );
					}
					else if( ch == '\t' && _UsesTabForIndent == false )
					{
						Point caretPos;
						Point nextTabStopPos;
						
						// get x-coord of caret index
						doc.GetSelection( out selBegin, out selEnd );
						caretPos = View.GetVirPosFromIndex( selBegin );

						// calc next tab stop
						// ([*] When distance of the caret and next tab stop is narrower than a space width,
						// no padding chars will be made and 'nothing will happen.'
						// To avoid such case, here we add an extra space width
						// before calculating next tab stop.)
						nextTabStopPos = caretPos;
						nextTabStopPos.X += View.SpaceWidthInPx - 1; // [*]
						nextTabStopPos.X += View.TabWidthInPx;
						nextTabStopPos.X -= (nextTabStopPos.X % View.TabWidthInPx);

						// make padding spaces
						int spaceCount = (nextTabStopPos.X - caretPos.X) / View.SpaceWidthInPx;
						for( int i=0; i<spaceCount; i++ )
						{
							input.Append( ' ' );
						}
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
				_View.SetDesiredColumn();

				// update graphic
				_View.ScrollToCaret();
				//NO_NEED//_View.Invalidate( xxx ); // Doc_ContentChanged will do invalidation well.
			}
			finally
			{
				doc.EndUndo();
				input.Length = 0;
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

		void HighlighterThreadProc()
		{
			int dirtyBegin, dirtyEnd;
			Document doc;

			while( _IsDisposed == false )
			{
				// wait while the content is untouched
				while( _ShouldBeHighlighted == false )
				{
					Thread.Sleep( HighlightInterval1 );
					if( _IsDisposed )
					{
						return; // quit ASAP
					}
				}
				_ShouldBeHighlighted = false;

				// wait a moment and check if the flag is still up
				Thread.Sleep( HighlightInterval2 );
				if( _ShouldBeHighlighted != false || _UI.Document == null )
				{
					// flag was set up while this thread are sleeping.
					// skip this time.
					continue;
				}

				// if no highlighter was set to current active document, do nothing.
				doc = _UI.Document;
				if( doc.Highlighter == null )
				{
					continue;
				}

				// determine where to start highlighting
				dirtyBegin = Math.Max( 0, _DirtyRangeBegin );
				dirtyEnd = Math.Max( dirtyBegin, _DirtyRangeEnd );
				if( doc.Length < dirtyEnd )
				{
					dirtyEnd = doc.Length;
				}

				// highlight and refresh view
				try
				{
					doc.Highlighter.Highlight( doc, ref dirtyBegin, ref dirtyEnd );
					View.Invalidate( dirtyBegin, dirtyEnd );
				}
				catch( Exception ex )
				{
					// exit if the exception is ThreadAbortException
					if( ex is ThreadAbortException )
					{
						break;
					}

					// For example, contents could be shorten just during highlighting
					// because Azuki design does not lock buffers for thread safety.
					// It is very hard to take care of such cases in highlighters (including user-made ones)
					// so here I trap any exception (except ThreadAbortException)
					// and invalidate whole view in that case.
					View.Invalidate();
				}

				// prepare for next loop
				_DirtyRangeBegin = -1;
				_DirtyRangeEnd = -1;
			}
		}
		#endregion

		#region Other
		/// <summary>
		/// Gets currently selected text.
		/// </summary>
		/// <returns>Currently selected text.</returns>
		/// <remarks>
		/// This method gets currently selected text.
		/// If current selection is rectangle selection,
		/// return value will be a text that are consisted with selected partial lines (rows)
		/// joined with CR-LF.
		/// </remarks>
		public string GetSelectedText()
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
					text.Append( row + "\r\n" );
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

			_View.Paint( clipRect );
		}

		public void HandleLostFocus()
		{
			if( _IsDisposed )
				return;

			ClearDragState();
		}

		internal void HandleMouseUp( int buttonIndex, Point pos, bool shift, bool ctrl, bool alt, bool win )
		{
			if( _IsDisposed )
				return;

			ClearDragState();
		}

		internal void HandleMouseDown( int buttonIndex, Point pos, bool shift, bool ctrl, bool alt, bool win )
		{
			if( _IsDisposed )
				return;

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

			if( buttonIndex == 0 ) // left click
			{
				int index;

				// calculate index of clicked character
				index = View.GetIndexFromVirPos( pos );

				// set selection
				if( onLineNumberArea )
				{
					//--- line selection ---
					_UI.SelectionMode = TextDataType.Line;
					if( shift )
					{
						//--- expanding line selection ---
						// expand selection to one char next of clicked position
						// (if caret is at head of a line,
						// the line will not be selected by SetSelection.)
						int newCaretIndex = index;
						if( newCaretIndex+1 < Document.Length )
						{
							newCaretIndex++;
						}
						Document.SetSelection( Document.AnchorIndex, newCaretIndex, View );
					}
					else
					{
						//--- setting line selection ---
						Document.SetSelection( index, index, View );
					}
				}
				else if( shift )
				{
					//--- expanding selection ---
					Document.SetSelection( Document.AnchorIndex, index );
				}
				else if( alt )
				{
					//--- rectangle selection ---
					_UI.SelectionMode = TextDataType.Rectangle;
					Document.SetSelection( index, index, View );
				}
				else
				{
					//--- setting caret ---
					Document.SetSelection( index, index );
				}
				View.SetDesiredColumn();
				View.ScrollToCaret();
			}
		}

		internal void HandleDoubleClick( int buttonIndex, Point pos, bool shift, bool ctrl, bool alt, bool win )
		{
			if( _IsDisposed )
				return;

			int index;
			int begin, end;

			// convert screen coordinate to virtual view coordinate
			pos.X = Math.Max( 0, pos.X );
			pos.Y = Math.Max( 0, pos.Y );
			View.ScreenToVirtual( ref pos );

			// get range of a word at clicked location
			index = View.GetIndexFromVirPos( pos );
			Document.GetWordAt( index, out begin, out end );
			if( end <= begin )
			{
				return;
			}

			// select the word.
			// (because Azuki's invalidation logic only supports
			// selection change by keyboard commands,
			// emulate as if this selection was done by keyboard.
			Document.SetSelection( begin, begin ); // select caret to the head of the word
			Document.SetSelection( begin, end ); // then, expand selection to the end of it
		}

		internal void HandleMouseMove( int buttonIndex, Point pos, bool shift, bool ctrl, bool alt, bool win )
		{
			if( _IsDisposed )
				return;

			int xOffset, yOffset;

			// if mouse button was not down, ignore
			if( _MouseDownVirPos.X == Int32.MinValue )
				return;

			// make sure that these coordinates are positive value
			pos.X = Math.Max( 0, pos.X );
			pos.Y = Math.Max( 0, pos.Y );
			View.ScreenToVirtual( ref pos );

			// if it was slight movement, ignore
			if( _MouseDragging == false )
			{
				xOffset = Math.Abs( pos.X - _MouseDownVirPos.X );
				yOffset = Math.Abs( pos.Y - _MouseDownVirPos.Y );
				if( xOffset <= View.DragThresh && yOffset <= View.DragThresh )
				{
					return;
				}
			}
			_MouseDragging = true;

			// dragging with left button?
			if( buttonIndex == 0 )
			{
				int curPosIndex;

				// calc index of where the mouse pointer is on
				curPosIndex = View.GetIndexFromVirPos( pos );
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
				else
				{
					//--- normal selection ---
					// expand selection to the point if it was different from previous index
					if( curPosIndex != Document.CaretIndex )
					{
						Document.SetSelection( Document.AnchorIndex, curPosIndex );
					}
				}
				View.SetDesiredColumn();
				View.ScrollToCaret();
			}
		}

		void ClearDragState()
		{
			_MouseDownVirPos.X = Int32.MinValue;
			_MouseDragging = false;
			_UI.SelectionMode = TextDataType.Normal;
		}
		#endregion

		#region Event Handlers
		void InstallDocumentEventHandlers( Document doc )
		{
			Debug.Assert( _IsDisposed == false );
			doc.SelectionChanged += Doc_SelectionChanged;
			doc.ContentChanged += Doc_ContentChanged;
			doc.DirtyStateChanged += Doc_DirtyStateChanged;
		}

		void UninstallDocumentEventHandlers( Document doc )
		{
			Debug.Assert( _IsDisposed == false );
			doc.SelectionChanged -= Doc_SelectionChanged;
			doc.ContentChanged -= Doc_ContentChanged;
			doc.DirtyStateChanged -= Doc_DirtyStateChanged;
		}

		void Doc_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			Debug.Assert( _IsDisposed == false );

			// delegate to view object
			View.HandleSelectionChanged( sender, e );

			// update caret graphic
			_UI.UpdateCaretGraphic();

			// send event to component users
			_UI.InvokeCaretMoved();
		}

		public void Doc_ContentChanged( object sender, ContentChangedEventArgs e )
		{
			Debug.Assert( _IsDisposed == false );

			// delegate to view object
			View.HandleContentChanged( sender, e );

			// redraw caret graphic
			_UI.UpdateCaretGraphic();
			
			// update range of scroll bars
			_UI.UpdateScrollBarRange();

			// set flag to start highlighting
			if( _DirtyRangeBegin == -1 || e.Index < _DirtyRangeBegin )
			{
				_DirtyRangeBegin = e.Index;
			}
			if( _DirtyRangeEnd == -1 || _DirtyRangeEnd < e.Index + e.NewText.Length )
			{
				_DirtyRangeEnd = e.Index + e.NewText.Length;
			}
			_ShouldBeHighlighted = true;
		}

		public void Doc_DirtyStateChanged( object sender, EventArgs e )
		{
			Debug.Assert( _IsDisposed == false );

			Document doc = (Document)sender;

			// delegate to view object
			View.HandleDirtyStateChanged( sender, e );
		}
		#endregion

		#region Utilitites
		/// <summary>
		/// Generates appropriate padding characters
		/// that fills the gap between the target position and actual line-end position.
		/// </summary>
		internal static string GetNeededPaddingChars( IUserInterface ui, Point targetVirPos, bool alignTabStop )
		{
			StringBuilder paddingChars;
			int targetIndex;
			Point lineLastCharPos;
			int rightMostTabStopX;
			int neededTabCount = 0;
			int neededSpaceCount;

			// calculate the position of the nearest character in line
			// (this will be at end of the line)
			targetIndex = ui.View.GetIndexFromVirPos( targetVirPos );
			lineLastCharPos = ui.View.GetVirPosFromIndex( targetIndex );
			if( targetVirPos.X <= lineLastCharPos.X + ui.View.SpaceWidthInPx )
			{
				return ""; // no padding is needed
			}

			// calculate right most tab stop at left of the target position
			rightMostTabStopX = targetVirPos.X - (targetVirPos.X % ui.View.TabWidthInPx);
			if( alignTabStop )
			{
				// to align position to tab stop,
				// set target position to the right most tab stop.
				targetVirPos.X = rightMostTabStopX;
			}

			// calculate how many tabs are needed
			if( ui.UsesTabForIndent )
			{
				int availableRightMostTabStopX
					= lineLastCharPos.X - (lineLastCharPos.X % ui.View.TabWidthInPx);
				neededTabCount = (targetVirPos.X - availableRightMostTabStopX) / ui.View.TabWidthInPx;
			}

			// calculate how many spaces are needed
			if( 0 < neededTabCount )
			{
				neededSpaceCount = (targetVirPos.X - rightMostTabStopX) / ui.View.SpaceWidthInPx;
			}
			else
			{
				neededSpaceCount = (targetVirPos.X - lineLastCharPos.X) / ui.View.SpaceWidthInPx;
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
