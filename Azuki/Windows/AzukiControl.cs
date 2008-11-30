// file: AzukiControl.cs
// brief: User interface for Windows platform (both Desktop and CE).
// author: YAMAMOTO Suguru
// update: 2008-11-30
//=========================================================
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Sgry.Azuki.Windows
{
	using IHighlighter = Highlighter.IHighlighter;

	/// <summary>
	/// Azuki user interface for Windows.Forms framework
	/// (.NET Compact Framework compatible).
	/// </summary>
	public class AzukiControl : Control, IUserInterface
	{
		#region Types, Constants and Fields
		const int DefaultCaretWidth = 2;
		static int _ScrollBarWidth = 0;
		
		delegate void InvalidateProc1();
		delegate void InvalidateProc2( Rectangle rect );
		
		UiImpl _Impl;
		Size _CaretSize = new Size( DefaultCaretWidth, 10 );
		bool _AcceptsReturn = true;
		bool _AcceptsTab = true;
		bool _ShowsHScrollBar = true;
#		if !PocketPC
		bool _UseCtrlTabToMoveFocus = true;
#		endif
		int _WheelPos = 0;
		
		InvalidateProc1 _invalidateProc1 = null;
		InvalidateProc2 _invalidateProc2 = null;
		
		IntPtr _OriginalWndProcObj = IntPtr.Zero;
		WinApi.WNDPROC _CustomWndProcObj = null;
		#endregion
		
		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public AzukiControl()
		{
			// check platform
			try
			{
				// if this is a build for CF and called on FF platform,
				// this must throw DllNotFoundException.
				WinApi.IsKeyDown( Keys.A );
			}
			catch( DllNotFoundException ex )
			{
				throw new PlatformNotSupportedException( "Not supported platform", ex );
			}

			// rewrite window procedure at first
			// (force to create window by accessing Handle property)
			IntPtr dummy = this.Handle;
			RewriteWndProc();
			
			// generate core implementation
			_Impl = new UiImpl( this );

#			if !PocketPC
			base.Cursor = Cursors.IBeam;
#			endif

			// set default value for each scroll bar
			// (setting scroll bar range forces the window to have style of WS_VSCROLL/WS_HSCROLL)
			WinApi.SetScrollRange( Handle, false, 0, 1, 1 );
			WinApi.SetScrollRange( Handle, true, 0, 1, 1 );
			
			this.Font = base.Font;
			WinApi.CreateCaret( Handle, _CaretSize );
			WinApi.SetCaretPos( 0, 0 );

			// install GUI event handlers
			HandleDestroyed += Control_Destroyed;
			//DO_NOT//Paint += Control_Paint;
			KeyDown += Control_KeyDown;
			KeyPress += Control_KeyPress;
			GotFocus += Control_GotFocus;
			LostFocus += Control_LostFocus;
			Resize += Control_Resized;

			// setup document event handler
			Document = new Document();
			ViewType = ViewType.Propotional; // (setting ViewType installs document event handlers)

			// setup default keybind
			ResetKeyBind();

			// calculate scrollbar width
			using( ScrollBar sb = new VScrollBar() )
			{
				_ScrollBarWidth = sb.Width;
			}
		}

		void Control_Destroyed( object sender, EventArgs e )
		{
			_Impl.Dispose();

			// destroy caret
			WinApi.DestroyCaret();
		}
		#endregion

		#region IUserInterface - Associated View and Document
		/// <summary>
		/// Gets or sets the document which is the current editing target.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public Document Document
		{
			get{ return _Impl.Document; }
			set{ _Impl.Document = value; }
		}

		/// <summary>
		/// Gets the associated view object.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public View View
		{
			get{ return _Impl.View; }
		}

		/// <summary>
		/// Gets or sets type of the view.
		/// View type determine how to render text content.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(ViewType.Propotional)]
		[Description("Specify how to draw text content. Wrapped propotional view shows text wrapped within the width specified as ViewWidth property. Propotional view do not wrap text but draws faster.")]
#		endif
		public ViewType ViewType
		{
			get{ return _Impl.ViewType; }
			set{ _Impl.ViewType = value; }
		}
		#endregion

		#region IUserInterface - KeyBind
		/// <summary>
		/// Reset keybind to default.
		/// </summary>
		public void ResetKeyBind()
		{
			_Impl.ClearKeyBind();

			SetKeyBind( Keys.Right, Actions.MoveRight );
			SetKeyBind( Keys.Left, Actions.MoveLeft );
			SetKeyBind( Keys.Down, Actions.MoveDown );
			SetKeyBind( Keys.Up, Actions.MoveUp );
			SetKeyBind( Keys.Right|Keys.Control, Actions.MoveToNextWord );
			SetKeyBind( Keys.Left|Keys.Control, Actions.MoveToPrevWord );
			SetKeyBind( Keys.Home, Actions.MoveToLineHeadSmart );
			SetKeyBind( Keys.End, Actions.MoveToLineEnd );
			SetKeyBind( Keys.PageDown, Actions.MovePageDown );
			SetKeyBind( Keys.PageUp, Actions.MovePageUp );
			SetKeyBind( Keys.Home|Keys.Control, Actions.MoveToFileHead );
			SetKeyBind( Keys.End|Keys.Control, Actions.MoveToFileEnd );

			SetKeyBind( Keys.Right|Keys.Shift, Actions.SelectToRight );
			SetKeyBind( Keys.Left|Keys.Shift, Actions.SelectToLeft );
			SetKeyBind( Keys.Down|Keys.Shift, Actions.SelectToDown );
			SetKeyBind( Keys.Up|Keys.Shift, Actions.SelectToUp );
			SetKeyBind( Keys.Right|Keys.Shift|Keys.Control, Actions.SelectToNextWord );
			SetKeyBind( Keys.Left|Keys.Shift|Keys.Control, Actions.SelectToPrevWord );
			SetKeyBind( Keys.Home|Keys.Shift, Actions.SelectToLineHeadSmart );
			SetKeyBind( Keys.End|Keys.Shift, Actions.SelectToLineEnd );
			SetKeyBind( Keys.PageDown|Keys.Shift, Actions.SelectToPageDown );
			SetKeyBind( Keys.PageUp|Keys.Shift, Actions.SelectToPageUp );
			SetKeyBind( Keys.Home|Keys.Control|Keys.Shift, Actions.SelectToFileHead );
			SetKeyBind( Keys.End|Keys.Control|Keys.Shift, Actions.SelectToFileEnd );
			
			SetKeyBind( Keys.Back, Actions.BackSpace );
			SetKeyBind( Keys.Back|Keys.Control, Actions.BackSpaceWord );
			SetKeyBind( Keys.Delete, Actions.Delete );
			SetKeyBind( Keys.Delete|Keys.Control, Actions.DeleteWord );
			SetKeyBind( Keys.A|Keys.Control, Actions.SelectAll );
			SetKeyBind( Keys.V|Keys.Control, Actions.Paste );
			SetKeyBind( Keys.C|Keys.Control, Actions.Copy );
			SetKeyBind( Keys.X|Keys.Control, Actions.Cut );
			SetKeyBind( Keys.Z|Keys.Control, Actions.Undo );
			SetKeyBind( Keys.Z|Keys.Control|Keys.Shift, Actions.Redo );
			SetKeyBind( Keys.Y|Keys.Control, Actions.Redo );

			SetKeyBind( Keys.Insert, Actions.ToggleOverwriteMode );
			SetKeyBind( Keys.F5, Actions.Refresh );

#			if !PocketPC
			SetKeyBind( Keys.Up|Keys.Control, Actions.ScrollUp );
			SetKeyBind( Keys.Down|Keys.Control, Actions.ScrollDown );
#			else
			SetKeyBind( Keys.Up|Keys.Control, Actions.MovePageUp );
			SetKeyBind( Keys.Down|Keys.Control, Actions.MovePageDown );
#			endif
		}

		/// <summary>
		/// Gets an action which is already associated with given key.
		/// If no action was associate with given key, returns null.
		/// </summary>
		/// <param name="keyCode">key code</param>
		public ActionProc GetKeyBind( uint keyCode )
		{
			return _Impl.GetKeyBind( keyCode );
		}

		/// <summary>
		/// Gets an action which is already associated with given key.
		/// If no action was associate with given key, returns null.
		/// </summary>
		/// <param name="keyCode">key code</param>
		public ActionProc GetKeyBind( Keys keyCode )
		{
			return _Impl.GetKeyBind( (uint)keyCode );
		}

		/// <summary>
		/// Sets or removes key-bind entry.
		/// Note that giving null to action will remove the key-bind.
		/// </summary>
		/// <param name="keyCode">key code to set/remove new action</param>
		/// <param name="action">action to be associated or null in case of removing key-bind.</param>
		public void SetKeyBind( uint keyCode, ActionProc action )
		{
			_Impl.SetKeyBind( keyCode, action );
		}

		/// <summary>
		/// Sets or removes key-bind entry.
		/// Note that giving null to action will remove the key-bind.
		/// </summary>
		/// <param name="keyCode">key code to set/remove new action</param>
		/// <param name="action">action to be associated or null in case of removing key-bind.</param>
		public void SetKeyBind( Keys keyCode, ActionProc action )
		{
			SetKeyBind( (uint)keyCode, action );
		}
		#endregion

		#region IUserInterface - Appearance
		/// <summary>
		/// Updates size and position of the caret graphic.
		/// </summary>
		public void UpdateCaretGraphic()
		{
			if( Document == null )
				throw new InvalidOperationException( "Document was not set yet." );

#			if !PocketPC
			if( DesignMode )
				return;
#			endif
			if( Focused == false )
				return;

			// calculate caret size
			_CaretSize.Width = Utl.CalcOverwriteCaretWidth( Document, View, CaretIndex, IsOverwriteMode );

			// calculate caret position and show/hide caret
			Point newCaretPos = GetPositionFromIndex( Document.CaretIndex );
			if( newCaretPos.X < View.TextAreaX
				|| newCaretPos.Y < 0 )
			{
				WinApi.SetCaretPos( newCaretPos.X, newCaretPos.Y );
				WinApi.HideCaret( Handle );
			}
			else
			{
				//NO_NEED//_Caret.Destroy();
				WinApi.CreateCaret( Handle, _CaretSize );
				WinApi.SetCaretPos( newCaretPos.X, newCaretPos.Y ); // must be called after creation in CE
				
				WinApi.ShowCaret( Handle );
			}

			// move IMM window to there if exists
			WinApi.SetImeWindowPos( Handle, newCaretPos );
		}

		/// <summary>
		/// Gets or sets font to be used for displaying text.
		/// </summary>
		public override Font Font
		{
			get{ return base.Font; }
			set
			{
				if( value == null )
					throw new ArgumentException( "invalid operation; AzukiControl.Font was set to null." );

				base.Font = value;
				View.Font = value;

				// update caret height
				_CaretSize.Height = View.LineHeight;
				WinApi.CreateCaret( Handle, _CaretSize );
			}
		}

		/// <summary>
		/// Color set used for displaying text.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public ColorScheme ColorScheme
		{
			get{ return View.ColorScheme; }
			set{ View.ColorScheme = value; }
		}

		/// <summary>
		/// Gets or sets drawing options.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
#		endif
		public DrawingOption DrawingOption
		{
			get{ return View.DrawingOption; }
			set{ View.DrawingOption = value; }
		}

		/// <summary>
		/// Gets or sets whether to show line number or not.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(true)]
#		endif
		public bool ShowsLineNumber
		{
			get{ return View.ShowLineNumber; }
			set
			{
				if( View.ShowLineNumber != value )
				{
					View.ShowLineNumber = value;
					Invalidate();
				}
			}
		}

		/// <summary>
		/// Whether to show horizontal scroll bar or not.
		/// </summary>
#		if !PocketPC
		[Category("Appearance")]
		[DefaultValue(true)]
		[Description("If true, horizontal scrollbar will appear.")]
#		endif
		public bool ShowsHScrollBar
		{
			get{ return _ShowsHScrollBar; }
			set
			{
				const int SWP_FRAMECHANGED = 0x0020;

				_ShowsHScrollBar = value;

				// make new style bits
				long style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
				if( _ShowsHScrollBar )
					style |= WinApi.WS_HSCROLL;
				else
					style &= ~(WinApi.WS_HSCROLL);

				// apply
				WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );
				WinApi.SetWindowPos( Handle, IntPtr.Zero, Left, Top, Width, Height, SWP_FRAMECHANGED );
				UpdateScrollBarRange();
			}
		}

		/// <summary>
		/// Gets or sets whether the current line would be drawn with underline or not.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(true)]
#		endif
		public bool HighlightsCurrentLine
		{
			get{ return View.HighlightsCurrentLine; }
			set{ View.HighlightsCurrentLine = value; }
		}

		/// <summary>
		/// Gets or sets whether to show half-width space with special graphic or not.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(false)]
#		endif
		public bool DrawsSpace
		{
			get{ return View.DrawsSpace; }
			set{ View.DrawsSpace = value; }
		}

		/// <summary>
		/// Gets or sets whether to show full-width space with special graphic or not.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(true)]
#		endif
		public bool DrawsFullWidthSpace
		{
			get{ return View.DrawsFullWidthSpace; }
			set{ View.DrawsFullWidthSpace = value; }
		}

		/// <summary>
		/// Gets or sets whether to show tab character with special graphic or not.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(true)]
#		endif
		public bool DrawsTab
		{
			get{ return View.DrawsTab; }
			set{ View.DrawsTab = value; }
		}

		/// <summary>
		/// Gets or sets whether to show EOL code with special graphic or not.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(true)]
#		endif
		public bool DrawsEolCode
		{
			get{ return View.DrawsEolCode; }
			set{ View.DrawsEolCode = value; }
		}

		/// <summary>
		/// Gets or sets tab width in count of space chars.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(4)]
#		endif
		public int TabWidth
		{
			get{ return View.TabWidth; }
			set{ View.TabWidth = value; }
		}

		/// <summary>
		/// Gets height of each lines in pixel.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
#		endif
		public int LineHeight
		{
			get{ return View.LineHeight; }
		}

		/// <summary>
		/// Sets width of the content area (including line number area).
		/// </summary>
#		if !PocketPC
		[Browsable(true)]
		[Category("Drawing")]
		[Description("Width of the text content area. In proportional view, highlight line will be drawn in this width and this will be automatically expanded to enough width to show the input text. In wrapped-propotional view, text will be wrapped in this width.")]
#		endif
		public int ViewWidth
		{
			get{ return View.TextAreaWidth + View.TextAreaX; }
			set
			{
				View.TextAreaWidth = value - View.TextAreaX;
				Refresh();
				UpdateCaretGraphic();
			}
		}

		/// <summary>
		/// Invalidate and make 'dirty' whole area
		/// (force to be redrawn by next paint event message).
		/// </summary>
		public new void Invalidate()
		{
			if( _invalidateProc1 == null )
				_invalidateProc1 = base.Invalidate;
			Invoke( _invalidateProc1 );
		}

		/// <summary>
		/// Invalidate and make 'dirty' specified area
		/// (force to be redrawn by next paint event message).
		/// </summary>
		public new void Invalidate( Rectangle rect )
		{
			if( _invalidateProc2 == null )
				_invalidateProc2 = base.Invalidate;
			Invoke( _invalidateProc2, new object[]{rect} );
		}
		#endregion
		
		#region IUserInterface - Editing Behavior
		/// <summary>
		/// Gets or sets whether this document is read-only or not.
		/// </summary>
#		if !PocketPC
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If true, nothing can be input or deleted by keyboard and mouse.")]
#		endif
		public bool IsReadOnly
		{
			get{ return Document.IsReadOnly; }
			set{ Document.IsReadOnly = value; }
		}

		/// <summary>
		/// Gets or sets whether overwrite mode is enabled or not.
		/// In overwrite mode, input character will not be inserted
		/// but replaces the character at where the caret is on.
		/// </summary>
#		if !PocketPC
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If true, input character will not be inserted but replaces the character at where the caret is on.")]
#		endif
		public bool IsOverwriteMode
		{
			get{ return _Impl.IsOverwriteMode; }
			set{ _Impl.IsOverwriteMode = value; }
		}

		/// <summary>
		/// Gets or sets hook delegate to execute auto-indentation.
		/// If null, auto-indentation will not be performed.
		/// </summary>
		/// <seealso cref="AutoIndentLogic"/>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public AutoIndentHook AutoIndentHook
		{
			get{ return _Impl.AutoIndentHook; }
			set{ _Impl.AutoIndentHook = value; }
		}

		/// <summary>
		/// Gets or sets whether to automatically convert
		/// an input tab character to equivalent amount of spaces.
		/// </summary>
#		if !PocketPC
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If true, an input tab character will be automatically converted into equivalent amount of spaces.")]
#		endif
		public bool ConvertsTabToSpaces
		{
			get{ return _Impl.ConvertsTabToSpaces; }
			set{ _Impl.ConvertsTabToSpaces = value; }
		}

		/// <summary>
		/// Gets or sets whether to automatically convert
		/// an input full-width space to a space.
		/// </summary>
#		if !PocketPC
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If true, an input full-width space will be automatically converted to a half-width space.")]
#		endif
		public bool ConvertsFullWidthSpaceToSpace
		{
			get{ return _Impl.ConvertsFullWidthSpaceToSpace; }
			set{ _Impl.ConvertsFullWidthSpaceToSpace = value; }
		}

		/// <summary>
		/// If this is true, treats Enter key as an input and
		/// prevent pressing dialog default button.
		/// </summary>
#		if !PocketPC
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("If this is true, treats Enter key as an input and prevent pressing dialog default button.")]
#		endif
		public bool AcceptsReturn
		{
			get{ return _AcceptsReturn; }
			set{ _AcceptsReturn = value; }
		}

		/// <summary>
		/// If this is true, treats Tab key as an input and
		/// prevent moving focus to other control in a dialog.
		/// </summary>
#		if !PocketPC
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("If this is true, treats Tab key as an input and prevent moving focus to other control in a dialog.")]
#		endif
		public bool AcceptsTab
		{
			get{ return _AcceptsTab; }
			set{ _AcceptsTab = value; }
		}
		#endregion

		#region IUserInterface - Edit Actions
		/// <summary>
		/// Executes UNDO.
		/// </summary>
		public void Undo()
		{
			Actions.Undo( this );
		}

		/// <summary>
		/// Gets whether an available UNDO action exists or not.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public bool CanUndo
		{
			get{ return View.Document.CanUndo; }
		}

		/// <summary>
		/// Clears all stacked edit histories.
		/// </summary>
		public void ClearHistory()
		{
			View.Document.ClearHistory();
		}

		/// <summary>
		/// Gets or sets whether the edit actions will be recorded or not.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public bool IsRecordingHistory
		{
			get{ return View.Document.IsRecordingHistory; }
			set{ View.Document.IsRecordingHistory = value; }
		}

		/// <summary>
		/// Executes REDO.
		/// </summary>
		public void Redo()
		{
			Actions.Redo( this );
		}

		/// <summary>
		/// Gets whether an available REDO action exists or not.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public bool CanRedo
		{
			get{ return View.Document.CanUndo; }
		}

		/// <summary>
		/// Executes cut action.
		/// </summary>
		public void Cut()
		{
			Actions.Cut( this );
		}
		
		/// <summary>
		/// Executes copy action.
		/// </summary>
		public void Copy()
		{
			Actions.Copy( this );
		}
		
		/// <summary>
		/// Executes paste action.
		/// </summary>
		public void Paste()
		{
			Actions.Paste( this );
		}

		/// <summary>
		/// Executes delete action.
		/// </summary>
		public void Delete()
		{
			Actions.Delete( this );
		}
		#endregion

		#region IUserInterface - Selection
		/// <summary>
		/// Gets the index of where the caret is at (in char-index).
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
#		endif
		public int CaretIndex
		{
			get{ return View.Document.CaretIndex; }
		}

		/// <summary>
		/// Sets selection range and update the desired column.
		/// </summary>
		/// <param name="anchor">the position where the selection begins</param>
		/// <param name="caret">the position where the caret is</param>
		public void SetSelection( int anchor, int caret )
		{
			View.Document.SetSelection( anchor, caret );
			View.SetDesiredColumn();
		}

		/// <summary>
		/// Gets range of current selection.
		/// Note that this method does not return [anchor, caret) pair but [begin, end) pair.
		/// </summary>
		/// <param name="begin">index of where the selection begins.</param>
		/// <param name="end">index of where the selection ends (selection do not includes the char at this index).</param>
		public void GetSelection( out int begin, out int end )
		{
			View.Document.GetSelection( out begin, out end );
		}

		/// <summary>
		/// Selects all text.
		/// </summary>
		public void SelectAll()
		{
			Actions.SelectAll( this );
		}
		#endregion

		#region IUserInterface - Content Access
		/// <summary>
		/// Gets or sets currently inputted text.
		/// </summary>
		public override string Text
		{
			get
			{
				// under unknown condition, Text property was called when a Form was disposed.
				// take care of that case.
				if( View == null || View.Document == null )
					return null;

				return View.Document.Text;
			}
			set
			{
				//if( View == null || View.Document == null )
				//	return;

				View.Document.Text = value;
				View.SetDesiredColumn();
				ScrollToCaret();
			}
		}

		/// <summary>
		/// Gets currently inputted character's count.
		/// Note that a surrogate pair will be counted as two chars.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public int TextLength
		{
			get{ return View.Document.Length; }
		}

		/// <summary>
		/// Gets a word at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public string GetWordAt( int index )
		{
			return Document.GetWordAt( index );
		}

		/// <summary>
		/// Gets text in the range [begin, end).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified range was invalid.</exception>
		public string GetTextInRange( int begin, int end )
		{
			return View.Document.GetTextInRange( begin, end );
		}

		/// <summary>
		/// Gets number of lines currently inputted.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public int LineCount
		{
			get{ return View.Document.LineCount; }
		}
		#endregion

		#region IUserInterface - Position / Index Conversion
		/// <summary>
		/// Calculates screen location of the character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Invalid index was given.</exception>
		public Point GetPositionFromIndex( int index )
		{
			Point virPos = View.GetVirPosFromIndex( index );
			View.VirtualToScreen( ref virPos );
			return virPos;
		}

		/// <summary>
		/// Calculates screen location of the character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Invalid index was given.</exception>
		public Point GetPositionFromIndex( int lineIndex, int columnIndex )
		{
			Point vPos = View.GetVirPosFromIndex( lineIndex, columnIndex );
			View.VirtualToScreen( ref vPos );
			return vPos;
		}

		/// <summary>
		/// Gets char-index of the char at the point specified by screen location.
		/// </summary>
		public int GetIndexFromPosition( Point pt )
		{
			View.ScreenToVirtual( ref pt );
			return View.GetIndexFromVirPos( pt );
		}
		#endregion

		#region IUserInterface - Physical Line/Column Index
		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetLineHeadIndex( int lineIndex )
		{
			return View.GetLineHeadIndex( lineIndex );
		}

		/// <summary>
		/// Gets the index of the first char in the physical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			return View.GetLineHeadIndexFromCharIndex( charIndex );
		}

		/// <summary>
		/// Calculates physical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex )
		{
			View.GetLineColumnIndexFromCharIndex( charIndex, out lineIndex, out columnIndex );
		}

		/// <summary>
		/// Calculates char-index from physical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex )
		{
			return View.GetCharIndexFromLineColumnIndex( lineIndex, columnIndex );
		}
		#endregion

		#region IUserInterface - Events
		/// <summary>
		/// Occurs soon after the document's caret was moved.
		/// </summary>
		public event EventHandler CaretMoved;
		internal void InvokeCaretMoved()
		{
			if( CaretMoved != null )
			{
				CaretMoved( this, EventArgs.Empty );
			}
		}
		#endregion

		#region IUserInterface - Scroll
		/// <summary>
		/// Scrolls a portion of the window.
		/// </summary>
		public void Scroll( Rectangle rect, int vOffset, int hOffset )
		{
			WinApi.ScrollWindow( Handle, vOffset, hOffset, rect );
			WinApi.SetScrollPos( Handle, false, View.FirstVisibleLine );
			WinApi.SetScrollPos( Handle, true, View.ScrollPosX );
			UpdateCaretGraphic();
		}

		/// <summary>
		/// Scrolls to where the caret is.
		/// </summary>
		public void ScrollToCaret()
		{
			View.ScrollToCaret();
		}

		/// <summary>
		/// Updates scrollbar's range.
		/// </summary>
		public void UpdateScrollBarRange()
		{
			int vMax, hMax;
			int vPageSize, hPageSize;
			int visibleLineCount;

			// calculate vertical range and page size
			visibleLineCount = View.VisibleSize.Height / View.LineSpacing;
			if( View.LineCount >> 3 <= visibleLineCount )
			{
				vPageSize = visibleLineCount;
			}
			else
			{
				vPageSize = View.LineCount >> 3;
			}
			vMax = View.LineCount - 1;
			vMax += vPageSize - 1;

			// calculate horizontal range and page size
			hMax = View.TextAreaWidth;
			hPageSize = View.VisibleSize.Width;

			// update the range of vertical scrollbar
			WinApi.SetScrollRange( Handle, false, 0, vMax, vPageSize );
			
			// update the range of horizontal scrollbar
			if( ShowsHScrollBar == false )
				WinApi.SetScrollRange( Handle, true, 0, 0, hPageSize ); // bar will be hidden
			else
				WinApi.SetScrollRange( Handle, true, 0, hMax, hPageSize );
		}
		#endregion

		#region IUserInterface - Others
		/// <summary>
		/// Gets a graphic interface.
		/// </summary>
		public IGraphics GetIGraphics()
		{
			return Plat.Inst.GetGraphics( Handle );
		}

		/// <summary>
		/// Gets or sets highlighter for currently active document.
		/// Setting null to this property will disable highlighting.
		/// See built-in highlighters for
		/// <see cref="Sgry.Azuki.Highlighter.Highlighters">Highlighters</see>.
		/// </summary>
		/// <seealso cref="Sgry.Azuki.Highlighter.Highlighters"/>
		/// <remarks>
		/// Note that user can create and specify custom highlighter object.
		/// If you want to create a keyword-based highlighter,
		/// you can customize or extend
		/// <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter">KeywordHighlighter</see>.
		/// If you want ot create not a keyword based one,
		/// create a class which implements
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter">IHighlighter</see>
		/// and write your own highlighting logic.
		/// </remarks>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public IHighlighter Highlighter
		{
			get{ return _Impl.Highlighter; }
			set{ _Impl.Highlighter = value; }
		}
		#endregion

		#region GUI Event Handling
		void HandleWheelEvent( int scrollOffset )
		{
			// get modifier key state
			bool shift = WinApi.IsKeyDown( Keys.ShiftKey );
			bool control = WinApi.IsKeyDown( Keys.ControlKey );
			bool alt = WinApi.IsKeyDown( Keys.Menu );

			// dispatch mouse event
			if( !control && !alt && shift )
			{
				int type = (scrollOffset < 0) ? WinApi.SB_LINEUP : WinApi.SB_LINEDOWN;
				HandleHScrollEvent( type );
			}
			else if( control && !alt && !shift )
			{
				if( 0 < scrollOffset )
					View.ZoomIn();
				else
					View.ZoomOut();
			}
			else if( !control && !alt && !shift )
			{
				View.Scroll( scrollOffset );
			}
		}

		void HandleVScrollEvent( int scrollType )
		{
			int newPos = View.FirstVisibleLine;
			if( scrollType == WinApi.SB_LINEUP )
				newPos--;
			else if( scrollType == WinApi.SB_LINEDOWN )
				newPos++;
			else if( scrollType == WinApi.SB_PAGEUP )
				newPos -= (ClientSize.Height / View.LineSpacing);
			else if( scrollType == WinApi.SB_PAGEDOWN )
				newPos += (ClientSize.Height / View.LineSpacing);
			else if( scrollType == WinApi.SB_TOP )
				newPos = 0;
			else if( scrollType == WinApi.SB_BOTTOM )
				newPos = View.Document.LineCount - 1;
			else if( scrollType == WinApi.SB_THUMBPOSITION
				|| scrollType == WinApi.SB_THUMBTRACK )
				newPos = WinApi.GetScrollTrackPos( Handle, false );
			else if( scrollType == WinApi.SB_ENDSCROLL )
				return;

			int delta = newPos - View.FirstVisibleLine;
			View.Scroll( delta );
		}

		void HandleHScrollEvent( int scrollType )
		{
			int newPos = View.ScrollPosX;
			int scrollUnit = View.TabWidthInPx >> 1;

			if( scrollType == WinApi.SB_LINEUP )
				newPos -= scrollUnit;
			else if( scrollType == WinApi.SB_LINEDOWN )
				newPos += scrollUnit;
			if( scrollType == WinApi.SB_PAGEUP )
				newPos -= (Width - _ScrollBarWidth);
			else if( scrollType == WinApi.SB_PAGEDOWN )
				newPos += (Width - _ScrollBarWidth);
			else if( scrollType == WinApi.SB_TOP )
				newPos = 0;
			else if( scrollType == WinApi.SB_BOTTOM )
			{
				int min, max;
				WinApi.GetScrollRange( Handle, true, out min, out max );
				newPos = max;
			}
			else if( scrollType == WinApi.SB_THUMBPOSITION
				|| scrollType == WinApi.SB_THUMBTRACK )
			{
				// align to scroll unit (half of tab width)
				newPos = WinApi.GetScrollTrackPos( Handle, true );
				int leftScrollPos = (newPos / scrollUnit) * scrollUnit;
				if( newPos < leftScrollPos + scrollUnit/2 )
					newPos = leftScrollPos;
				else
					newPos = leftScrollPos + scrollUnit;
			}
			else if( scrollType == WinApi.SB_ENDSCROLL )
				return;
			
			int delta = newPos - View.ScrollPosX;
			View.HScroll( delta );
		}

		void Control_GotFocus( object sender, EventArgs e )
		{
			WinApi.CreateCaret( Handle, _CaretSize );
			UpdateCaretGraphic();
		}

		void Control_LostFocus( object sender, EventArgs e )
		{
			WinApi.HideCaret( Handle );
		}

		void Control_KeyDown( object sender, KeyEventArgs e )
		{
			_Impl.HandleKeyDown( (uint)e.KeyData );
		}

		void Control_KeyPress( object sender, KeyPressEventArgs e )
		{
			// TranslateMessage API (I think) treats some key combination specially
			// (Ctrl+I as an a HT(HorizontalTab), Ctrl+M as a LF(LineFeed) for example).
			// These behavior should not be expected by editor component users
			// and thus such char event is ignored here
			if( (e.KeyChar == '\t' && WinApi.IsKeyDownAsync(Keys.I))
				|| (e.KeyChar == '\r' && WinApi.IsKeyDownAsync(Keys.M))
				|| (e.KeyChar == '\n' && WinApi.IsKeyDownAsync(Keys.J)) )
			{
				return;
			}

			// ignore unwelcomed chars such as invisible control codes
			if( !MyIsInputChar(e.KeyChar) )
			{
				return;
			}

			// in addition, pressing TAB key with multi-line selection triggers
			// block-indent command
			if( e.KeyChar == '\t' )
			{
				int selBegin, selEnd;
				int selBeginL, selEndL;

				Document.GetSelection( out selBegin, out selEnd );
				selBeginL = Document.GetLineIndexFromCharIndex( selBegin );
				selEndL = Document.GetLineIndexFromCharIndex( selEnd );
				if( selBeginL != selEndL )
				{
					if( WinApi.IsKeyDown(Keys.ShiftKey) )
						Actions.BlockUnIndent( this );
					else
						Actions.BlockIndent( this );
					e.Handled = true;
					return;
				}
			}

			// otherwise, handle key-char event normally
			_Impl.HandleKeyPress( e.KeyChar );
			e.Handled = true;
		}

		void Control_Resized( object sender, EventArgs e )
		{
			View.HandleSizeChanged( ClientSize );
			Invalidate();
		}
		#endregion

		#region Behavior as a .NET Control
		/// <summary>
		/// Gets or sets default text color.
		/// </summary>
		public override Color ForeColor
		{
			get
			{
				if( View != null )
					return View.ColorScheme.ForeColor;
				else
					return base.ForeColor;
			}
			set
			{
				if( View != null )
					View.ColorScheme.ForeColor = value;
				else
					base.ForeColor = value;
			}
		}

		/// <summary>
		/// Gets or sets default background color.
		/// </summary>
		public override Color BackColor
		{
			get
			{
				if( View != null )
					return View.ColorScheme.BackColor;
				else
					return base.BackColor;
			}
			set
			{
				if( View != null )
					View.ColorScheme.BackColor = value;
				else
					base.BackColor = value;
			}
		}
		
#		if !PocketPC
		/// <summary>
		/// This defines the characters which must be treated as input for this control.
		/// This affects mnemonic key event in a dialog and does not affect to KeyPress (WM_CHAR) event.
		/// </summary>
		protected override bool IsInputChar( char charCode )
		{
			return MyIsInputChar( charCode );
		}
		
		/// <summary>
		/// This defines the keys which must be treated as input for this control.
		/// This affects mnemonic key event in a dialog and does not affect to KeyPress (WM_CHAR) event.
		/// </summary>
		protected override bool IsInputKey( Keys keyData )
		{
			// is there an action associted with that key?
			if( _Impl.IsKeyBindDefined((uint)keyData) )
			{
				return true;
			}
			else if( _AcceptsTab
				&& (keyData == Keys.Tab || keyData == (Keys.Tab|Keys.Shift)) )
			{
				return true;
			}
			else if( _AcceptsReturn
				&& keyData == Keys.Return )
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets or sets whether this control uses Ctrl+Tab and Ctrl+Shift+Tab
		/// for moving focus to other controls in a dialog.
		/// </summary>
#		if !PocketPC
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("Whether this control uses Ctrl+Tab and Ctrl+Shift+Tab for moving focus to other controls in a dialog.")]
#		endif
		public bool UseCtrlTabToMoveFocus
		{
			get{ return _UseCtrlTabToMoveFocus; }
			set{ _UseCtrlTabToMoveFocus = value; }
		}

		/// <summary>
		/// This overrides focusing strategy.
		/// </summary>
		protected override bool ProcessDialogKey( Keys keyData )
		{
			// if input key was Ctrl+Tab or Ctrl+Shift+Tab,
			// move focus to next/previous control if so configured
			if( _UseCtrlTabToMoveFocus )
			{
				if( keyData == (Keys.Tab|Keys.Control) )
					return base.ProcessDialogKey( Keys.Tab );
				else if( keyData == (Keys.Tab | Keys.Control | Keys.Shift) )
					return base.ProcessDialogKey( (Keys.Tab | Keys.Shift) );
			}

			return base.ProcessDialogKey( keyData );
		}
#		endif // PocketPC

		bool MyIsInputChar( char charCode )
		{
			if( !_AcceptsReturn && LineLogic.IsEolChar(charCode) )
				return false;
			if( !_AcceptsTab && charCode == '\t' )
				return false;

			if( (0x20 <= charCode && charCode <= 0x7e)
				|| charCode == '\r'
				|| charCode == '\n'
				|| charCode == ' '
				|| charCode == '\t'
				|| charCode == '\x3000' // full-space-width
				|| 0x7f < charCode )
			{
				return true;
			}

			return false;
		}
		#endregion

		#region Custom Window Procedure (handling v/h scroll and paint event etc.)
		IntPtr CustomWndProc( IntPtr window, Int32 message, IntPtr wParam, IntPtr lParam )
		{
			try
			{
				if( message == WinApi.WM_PAINT )
				{
					WinApi.PAINTSTRUCT ps;

					// .NET's Paint event does not inform invalidated region when double buffering was disabled.
					// In addition to this, Control.SetStyle is not supported in Compact Framework
					// and thus enabling double buffering seems impossible.
					// Therefore painting logic is called here.
					unsafe
					{
						WinApi.BeginPaint( window, &ps );

						Rectangle rect = new Rectangle( ps.paint.left, ps.paint.top, ps.paint.right-ps.paint.left, ps.paint.bottom-ps.paint.top );
						_Impl.HandlePaint( rect );

						WinApi.EndPaint( window, &ps );
						// (note that calling BeginPaint here effects something
						// to original paint logic of Control class;
						// "background will not drawn" and something.)
					}
				}
#				if !PocketPC
				else if( DesignMode )
				{
					; // do nothing
				}
#				endif
				else if( message == WinApi.WM_VSCROLL )
				{
					HandleVScrollEvent( wParam.ToInt32() & 0xffff );
				}
				else if( message == WinApi.WM_HSCROLL )
				{
					HandleHScrollEvent( wParam.ToInt32() & 0xffff );
				}
				else if( WinApi.WM_MOUSEMOVE <= message && message <= WinApi.WM_LBUTTONDBLCLK )
				{
					//const int MK_LBUTTON	= 0x0001;
					//const int MK_RBUTTON	= 0x0002;
					const int MK_SHIFT		= 0x0004;
					const int MK_CONTROL	= 0x0008;

					Point pos = new Point();
					int modFlag;
					bool shift, ctrl, alt, win;
					int buttonIndex;

					// get mouse click pos
					pos.X = (lParam.ToInt32() & 0xffff);
					pos.Y = ((lParam.ToInt32() >> 16)& 0xffff);
					
					// get modifier information
					modFlag = wParam.ToInt32();
					shift = (modFlag & MK_SHIFT) != 0;
					ctrl = (modFlag & MK_CONTROL) != 0;
					alt = WinApi.IsKeyDown( Keys.Menu );
					win = WinApi.IsKeyDown( Keys.LWin ) || WinApi.IsKeyDown( Keys.RWin );

					// get button which was used for this click
					if( message == WinApi.WM_RBUTTONDOWN
						|| message == WinApi.WM_LBUTTONDBLCLK )
					{
						buttonIndex = 1;
					}
					else/*if( message == WinApi.WM_LBUTTONDOWN
						|| message == WinApi.WM_LBUTTONDBLCLK )*/
					{
						buttonIndex = 0;
					}

					// delegate
					if( message == WinApi.WM_LBUTTONDBLCLK )
					{
						_Impl.HandleDoubleClick( buttonIndex, pos, shift, ctrl, alt, win );
					}
					else if( message == WinApi.WM_MOUSEMOVE )
					{
						_Impl.HandleMouseMove( buttonIndex, pos, shift, ctrl, alt, win );
					}
					else if( message == WinApi.WM_LBUTTONDOWN || message == WinApi.WM_RBUTTONDOWN )
					{
						this.Focus();
						_Impl.HandleMouseDown( buttonIndex, pos, shift, ctrl, alt, win );
					}
					else if( message == WinApi.WM_LBUTTONUP || message == WinApi.WM_RBUTTONUP )
					{
						_Impl.HandleMouseUp( buttonIndex, pos, shift, ctrl, alt, win );
					}
				}
				else if( message == WinApi.WM_MOUSEWHEEL )
				{
					// [*] on Vista x64, wParam value SHOULD be signed 64 bit like next:
					// 0xFFFFFFFFFF880000
					// but sometimes invalid value like next will be sent for same scroll action:
					// 0x00000000FE980000
					// so we should get extract 3rd and 4th byte and make it 16-bit int

					int linesPerWheel;
					Int16 wheelDelta;

					// get line count to scroll on each wheel event
#					if !PocketPC
					linesPerWheel = SystemInformation.MouseWheelScrollLines;
#					else
					linesPerWheel = 1;
#					endif

					// calculate wheel position
					wheelDelta = (Int16)( wParam.ToInt64() << 32 >> 48 ); // [*]
					_WheelPos += wheelDelta;

					// do scroll when the scroll position exceeds threashould
					if( 120 <= _WheelPos )
					{
						_WheelPos -= 120;
						HandleWheelEvent( -linesPerWheel );
					}
					else if( _WheelPos <= -120 )
					{
						_WheelPos += 120;
						HandleWheelEvent( linesPerWheel );
					}
				}
				else if( message == WinApi.WM_IME_STARTCOMPOSITION )
				{
					// move IMM window to caret position
					WinApi.SetImeWindowFont( Handle, Font );
				}
			}
			catch( Exception ex )
			{
				// because window proc was overwritten,
				// exceptions thrown in this method can not be handled well.
				// so we catch them here.
				Console.Error.WriteLine( ex );
				MessageBox.Show( ex.ToString(), "azuki bug" );
			}

			return WinApi.CallWindowProc( _OriginalWndProcObj, window, message, wParam, lParam );
		}

		/// <summary>
		/// Erases background.
		/// Note that Azuki does nothing on an event of redrawing background
		/// so just ignores WM_ERASEBKGND message.
		/// </summary>
		protected override void OnPaintBackground( PaintEventArgs e )
		{
			//DO_NOT//base.OnPaintBackground( e );
		}

		void RewriteWndProc()
		{
			const int GWL_WNDPROC = -4;
			
			if( _OriginalWndProcObj == IntPtr.Zero )
			{
				_OriginalWndProcObj = WinApi.GetWindowLong( Handle, GWL_WNDPROC );
			}
			if( _CustomWndProcObj == null )
			{
				_CustomWndProcObj = new WinApi.WNDPROC( this.CustomWndProc );
			}
			
			WinApi.SetWindowLong( Handle, GWL_WNDPROC, _CustomWndProcObj );
		}
		#endregion

		#region Utilities
		static class Utl
		{
			public static int CalcOverwriteCaretWidth( Document doc, View view, int caretIndex, bool isOverwriteMode )
			{
				int begin, end;
				char ch;

				// if it's no in overwrite mode, return default width
				if( !isOverwriteMode )
				{
					return DefaultCaretWidth;
				}

				// if something selected, return default width
				doc.GetSelection( out begin, out end );
				if( begin != end || doc.Length <= end )
				{
					return DefaultCaretWidth;
				}

				// calculate and return width
				ch = doc.GetCharAt( begin );
				if( ch != '\t' )
				{
					// this is not a tab so return width of this char
					return view.MeasureTokenEndX( ch.ToString(), 0 );
				}
				else
				{
					// this is a tab so calculate distance
					// from current position to next tab-stop and return it
					int lineHead = view.GetLineHeadIndexFromCharIndex( caretIndex );
					string leftPart = doc.GetTextInRange( lineHead, caretIndex );
					int currentX = view.MeasureTokenEndX( leftPart, 0 );
					int nextTabStopX = view.MeasureTokenEndX( leftPart+'\t', 0 );
					return nextTabStopX - currentX;
				}
			}
		}
		#endregion
	}
}
