// file: UiImpl.cs
// brief: User interface logic that independent from platform.
// author: YAMAMOTO Suguru
// update: 2009-04-18
//=========================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	using IHighlighter = Highlighter.IHighlighter;

	/// <summary>
	/// User inteface logic that independent from platform.
	/// </summary>
	class UiImpl : IDisposable
	{
		#region Fields
#		if PocketPC
		const int HighlightInterval = 500;
#		else
		const int HighlightInterval = 350;
#		endif
		IUserInterface _UI;
		View _View = null;
		ViewType _ViewType = ViewType.Propotional;

		IDictionary< uint, ActionProc > _KeyMap = new Dictionary< uint, ActionProc >( 32 );
		AutoIndentHook _AutoIndentHook = null;
		bool _IsOverwriteMode = false;
		bool _ConvertsTabToSpaces = false;
		bool _ConvertsFullWidthSpaceToSpace = false;

		Point _MouseDownPos = new Point( -1, 0 ); // this X coordinate also be used as a flag to determine whether the mouse button is down or not
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
			Debug.Assert( _View == null, ""+GetType()+"("+GetHashCode()+") was destroyed but not disposed." );
		}
#		endif

		public void Dispose()
		{
			_HighlighterThread.Abort();
			_HighlighterThread = null;

			// uninstall document event handlers
			Document.SelectionChanged -= Doc_SelectionChanged;
			Document.ContentChanged -= Doc_ContentChanged;

			// dispose view
			_View.Dispose();
			_View = null;
		}
		#endregion

		#region View and Document
		public Document Document
		{
			get{ return View.Document; }
			set
			{
				if( value == null )
					throw new ArgumentNullException();

				// uninstall event handlers
				if( View.Document != null
					&& View.Document != value )
				{
					View.Document.SelectionChanged -= Doc_SelectionChanged;
					View.Document.ContentChanged -= Doc_ContentChanged;
				}

				// replace document
				View.Document = value;

				// install event handlers
				View.Document.SelectionChanged += Doc_SelectionChanged;
				View.Document.ContentChanged += Doc_ContentChanged;

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
			set{ _View = value; }
		}
		
		/// <summary>
		/// Gets or sets type of the view.
		/// View type determine how to render text content.
		/// </summary>
		public ViewType ViewType
		{
			get{ return _ViewType; }
			set
			{
				View oldView = View;

				// switch to new view object
				switch( value )
				{
					case ViewType.WrappedPropotional:
						View = new PropWrapView( View );
						break;
					//case ViewType.Propotional:
					default:
						View = new PropView( View );
						break;
				}
				_ViewType = value;

				// dispose using view object
				oldView.Dispose();

				// re-install event handlers
				// (AzukiControl's event handler MUST be called AFTER view's one)
				if( Document != null )
				{
					Document.ContentChanged -= Doc_ContentChanged;
					Document.ContentChanged += Doc_ContentChanged;
					Document.SelectionChanged -= Doc_SelectionChanged;
					Document.SelectionChanged += Doc_SelectionChanged;
				}

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

		#region Behavior
		/// <summary>
		/// Gets or sets whether the input character overwrites the character at where the caret is on.
		/// </summary>
		public bool IsOverwriteMode
		{
			get{ return _IsOverwriteMode; }
			set
			{
				_IsOverwriteMode = value;
				_UI.UpdateCaretGraphic();
			}
		}

		/// <summary>
		/// Gets or sets whether to automatically convert
		/// an input tab character to equivalent amount of spaces.
		/// </summary>
		public bool ConvertsTabToSpaces
		{
			get{ return _ConvertsTabToSpaces; }
			set{ _ConvertsTabToSpaces = value; }
		}

		/// <summary>
		/// Gets or sets whether to automatically convert
		/// an input full-width space to a space.
		/// </summary>
		public bool ConvertsFullWidthSpaceToSpace
		{
			get{ return _ConvertsFullWidthSpaceToSpace; }
			set{ _ConvertsFullWidthSpaceToSpace = value; }
		}

		/// <summary>
		/// Gets or sets hook delegate to execute auto-indentation.
		/// If null, auto-indentation will not be performed.
		/// </summary>
		/// <seealso cref="AutoIndentHooks">AutoIndentHooks</seealso>
		public AutoIndentHook AutoIndentHook
		{
			get{ return _AutoIndentHook; }
			set{ _AutoIndentHook = value; }
		}
		#endregion

		#region Key Handling
		public ActionProc GetKeyBind( uint keyCode )
		{
			try
			{
				return _KeyMap[ keyCode ];
			}
			catch( KeyNotFoundException )
			{
				return null;
			}
		}

		public void SetKeyBind( uint keyCode, ActionProc action )
		{
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
			return _KeyMap.ContainsKey( keyCode );
		}

		public void ClearKeyBind()
		{
			_KeyMap.Clear();
		}

		/// <summary>
		/// Handles translated character input event.
		/// </summary>
		internal void HandleKeyPress( char ch )
		{
			string str = null;
			int newCaretIndex;
			Document doc = Document;
			int selBegin, selEnd;

			// just notify and return if in read only mode
			if( Document.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// try to use hook delegate
			if( _AutoIndentHook != null
				&& _AutoIndentHook(Document, ch) == true )
			{
				goto update;
			}

			// make string to be inserted
			doc.GetSelection( out selBegin, out selEnd );
			if( LineLogic.IsEolChar(ch) )
			{
				str = doc.EolCode;
			}
			else if( ch == '\t' && _ConvertsTabToSpaces )
			{
				int spaceCount = NextTabStop( selBegin ) - selBegin;
				str = String.Empty;
				for( int i=0; i<spaceCount; i++ )
				{
					str += ' ';
				}
			}
			else if( ch == '\x3000' && _ConvertsFullWidthSpaceToSpace )
			{
				str = "\x0020";
			}
			else
			{
				str = ch.ToString();
			}
			newCaretIndex = selBegin + str.Length;

			// calc replacement target range
			if( IsOverwriteMode
				&& selBegin == selEnd && selEnd+1 < doc.Length
				&& LineLogic.IsEolChar(doc[selBegin]) != true )
			{
				selEnd++;
			}

			// replace selection to input char
			doc.Replace( str, selBegin, selEnd );
			doc.SetSelection( newCaretIndex, newCaretIndex );

		update:
			// set desired column
			_View.SetDesiredColumn();

			// update graphic
			_View.ScrollToCaret();
			//NO_NEED//_View.Invalidate( xxx ); // Doc_ContentChanged will do invalidation well.
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

			while( true )
			{
				// wait while the flag is down
				while( _ShouldBeHighlighted == false )
				{
					Thread.Sleep( HighlightInterval );
				}
				_ShouldBeHighlighted = false;

				// wait a moment and check if the flag is still up
				Thread.Sleep( HighlightInterval );
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

		#region UI Event
		public void HandleKeyDown( uint keyData )
		{
			ActionProc action = GetKeyBind( keyData );
			if( action != null )
			{
				action( _UI );
			}
		}

		public void HandlePaint( Rectangle clipRect )
		{
			_View.Paint( clipRect );
		}

		internal void HandleMouseDown( int buttonIndex, Point pos, bool shift, bool ctrl, bool alt, bool win )
		{
			// if mouse-down coordinate is out of window, this is not a normal event so ignore this
			if( pos.X < 0 || pos.Y < 0 )
			{
				return;
			}

			// remember mouse down screen position and convert it to virtual view's coordinate
			_MouseDownPos = pos;
			View.ScreenToVirtual( ref pos );

			if( buttonIndex == 0 ) // left click
			{
				if( shift )
				{
					int index = View.GetIndexFromVirPos( pos );
					Document.SetSelection( Document.AnchorIndex, index );
				}
				else
				{
					int index = View.GetIndexFromVirPos( pos );
					Document.SetSelection( index, index );
				}
				View.SetDesiredColumn();
				View.ScrollToCaret();
			}
		}

		internal void HandleMouseUp( int buttonIndex, Point pos, bool shift, bool ctrl, bool alt, bool win )
		{
			_MouseDownPos.X = -1;
			_MouseDragging = false;
		}

		internal void HandleDoubleClick( int buttonIndex, Point pos, bool shift, bool ctrl, bool alt, bool win )
		{
			int index;
			int begin, end;

			// convert screen coordinate to virtual view coordinate
			pos.X = Math.Max( 0, pos.X );
			pos.Y = Math.Max( 0, pos.Y );
			View.ScreenToVirtual( ref pos );

			// get range of a word at clicked location
			index = View.GetIndexFromVirPos( pos );
			WordLogic.GetWordAt( Document, index, out begin, out end );
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
			// if mouse button was not down, ignore
			if( _MouseDownPos.X < 0 )
				return;

			// make sure that these coordinates are positive value
			pos.X = Math.Max( 0, pos.X );
			pos.Y = Math.Max( 0, pos.Y );

			// if the movement is very slightly, ignore
			if( _MouseDragging == false )
			{
				int xOffset = Math.Abs( pos.X - _MouseDownPos.X );
				int yOffset = Math.Abs( pos.Y - _MouseDownPos.Y );
				if( View.DragThresh < xOffset || View.DragThresh < yOffset )
				{
					_MouseDragging = true;
				}
				else
				{
					return;
				}
			}

			// dragging with left button?
			if( buttonIndex == 0 )
			{
				View.ScreenToVirtual( ref pos );

				// calc index of where the mouse pointer is on
				int index = View.GetIndexFromVirPos( pos );
				if( index == -1 || index == Document.CaretIndex )
				{
					return; // failed to get index or same as previous index
				}

				// expand selection to there
				Document.SetSelection( Document.AnchorIndex, index );
				View.SetDesiredColumn();
				View.ScrollToCaret();
			}
		}
		#endregion

		#region Event Handlers
		void Doc_SelectionChanged( object sender, EventArgs e )
		{
			// update caret graphic
			_UI.UpdateCaretGraphic();

			// send event to component users
			((Windows.AzukiControl)_UI).InvokeCaretMoved();
		}

		public void Doc_ContentChanged( object sender, ContentChangedEventArgs e )
		{
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
		#endregion

		#region Utilitites
		int NextTabStop( int index )
		{
			return ((index / _View.TabWidth) + 1) * _View.TabWidth;
		}
		#endregion
	}
}
