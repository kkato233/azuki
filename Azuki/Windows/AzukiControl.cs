// file: AzukiControl.cs
// brief: User interface for Windows platform (both Desktop and CE).
// author: YAMAMOTO Suguru
// update: 2009-10-12
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
		bool _UseCtrlTabToMoveFocus = true;
		int _WheelPos = 0;
		long _InputSuppressLimit = 0;
		BorderStyle _BorderStyle = BorderStyle.Fixed3D;
		
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
			dummy.ToInt32(); // (suppress warning to unreferenced variable)
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
			this.BorderStyle = _BorderStyle;

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
			ViewType = ViewType.Proportional; // (setting ViewType installs document event handlers)

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
			set
			{
				if( value == null )
					throw new ArgumentNullException();

				// uninstall event handler
				if( _Impl.Document != null )
				{
					_Impl.Document.ContentChanged -= Document_ContentChanged;
				}

				// switch to the new document
				_Impl.Document = value;

				// install event handler
				_Impl.Document.ContentChanged += Document_ContentChanged;
			}
		}

		void Document_ContentChanged( object sender, ContentChangedEventArgs e )
		{
			// just invoke TextChanged event of this Control.
			base.OnTextChanged( e );
		}

		/// <summary>
		/// Gets the associated view object.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public IView View
		{
			get{ return _Impl.View; }
		}

		/// <summary>
		/// Gets or sets type of the view.
		/// View type determine how to render text content.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(ViewType.Proportional)]
		[Description("Specify how to draw text content. Wrapped proportional view shows text wrapped within the width specified as ViewWidth property. Proportional view do not wrap text but draws faster.")]
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
			const int VK_OEM4 = 219; // key code of '['
			const int VK_OEM6 = 221; // key code of ']'

			_Impl.ClearKeyBind();

			// bind keys to move caret
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

			// bind keys to set selection
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
			SetKeyBind( Keys.Right|Keys.Alt, Actions.RectSelectToRight );
			SetKeyBind( Keys.Left|Keys.Alt, Actions.RectSelectToLeft );
			SetKeyBind( Keys.Up|Keys.Alt, Actions.RectSelectToUp );
			SetKeyBind( Keys.Down|Keys.Alt, Actions.RectSelectToDown );
			SetKeyBind( Keys.A|Keys.Control, Actions.SelectAll );

			// bind keys to edit document
			SetKeyBind( Keys.Back, Actions.BackSpace );
			SetKeyBind( Keys.Back|Keys.Control, Actions.BackSpaceWord );
			SetKeyBind( Keys.Delete, Actions.Delete );
			SetKeyBind( Keys.Delete|Keys.Control, Actions.DeleteWord );
			SetKeyBind( Keys.V|Keys.Control, Actions.Paste );
			SetKeyBind( Keys.C|Keys.Control, Actions.Copy );
			SetKeyBind( Keys.X|Keys.Control, Actions.Cut );
			SetKeyBind( Keys.Z|Keys.Control, Actions.Undo );
			SetKeyBind( Keys.Z|Keys.Control|Keys.Shift, Actions.Redo );
			SetKeyBind( Keys.Y|Keys.Control, Actions.Redo );

			// bind misc keys
			SetKeyBind( (Keys)VK_OEM4|Keys.Control, Actions.GoToMatchedBracket );
			SetKeyBind( (Keys)VK_OEM6|Keys.Control, Actions.GoToMatchedBracket );
			SetKeyBind( Keys.B|Keys.Control, Actions.ToggleRectSelectMode );
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
		/// Gets whether Azuki is in rectangle selection mode or not.
		/// </summary>
		public bool IsRectSelectMode
		{
			get{ return _Impl.IsRectSelectMode; }
			set
			{
				_Impl.IsRectSelectMode = value;

#				if !PocketPC
				// update mouse cursor graphic
				if( _Impl.IsRectSelectMode )
				{
					Cursor = Cursors.Arrow;
				}
				else
				{
					Cursor = Cursors.IBeam;
				}
#				endif
			}
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
		/// Gets or sets top margin of the view in pixel.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(1)]
		[Description("Specify margin at top of the text area in pixel.")]
#		endif
		public int TopMargin
		{
			get{ return View.TopMargin; }
			set
			{
				View.TopMargin = value;
				UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets left margin of the view in pixel.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(1)]
		[Description("Specify margin at left of the text area in pixel.")]
#		endif
		public int LeftMargin
		{
			get{ return View.LeftMargin; }
			set
			{
				View.LeftMargin = value;
				UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets type of the indicator on the horizontal ruler.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(HRulerIndicatorType.Segment)]
		[Description("Specify type of the indicator on the horizontal ruler.")]
#		endif
		public HRulerIndicatorType HRulerIndicatorType
		{
			get{ return View.HRulerIndicatorType; }
			set{ View.HRulerIndicatorType = value; }
		}

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
			_CaretSize.Width = Utl.CalcOverwriteCaretWidth( Document, _Impl.View, CaretIndex, IsOverwriteMode );

			// calculate caret position and show/hide caret
			Point newCaretPos = GetPositionFromIndex( Document.CaretIndex );
			if( newCaretPos.X < _Impl.View.XofTextArea
				|| newCaretPos.Y < _Impl.View.YofTextArea )
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
		/// Gets or sets graphical style of border of this control.
		/// </summary>
#		if !PocketPC
		[Category("Appearance")]
		[DefaultValue(BorderStyle.Fixed3D)]
#		endif
		public BorderStyle BorderStyle
		{
			get{ return _BorderStyle; }
			set
			{
				long style, exStyle;

				if( value == BorderStyle.FixedSingle )
				{
					// enable WS_BORDER window style
					style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
					style |= WinApi.WS_BORDER;
					WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );

					// disable WS_EX_CLIENTEDGE window style
					exStyle = WinApi.GetWindowLong( Handle, WinApi.GWL_EXSTYLE ).ToInt64();
					exStyle &= ~(WinApi.WS_EX_CLIENTEDGE);
					WinApi.SetWindowLong( Handle, WinApi.GWL_EXSTYLE, new IntPtr(exStyle) );
				}
				else if( value == BorderStyle.Fixed3D )
				{
					// disable WS_BORDER window style
					style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
					style &= ~(WinApi.WS_BORDER);
					WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );

					// enable WS_EX_CLIENTEDGE window style
					exStyle = WinApi.GetWindowLong( Handle, WinApi.GWL_EXSTYLE ).ToInt64();
					exStyle |= WinApi.WS_EX_CLIENTEDGE;
					WinApi.SetWindowLong( Handle, WinApi.GWL_EXSTYLE, new IntPtr(exStyle) );
				}
				else// if( value == BorderStyle.None )
				{
					// disable WS_BORDER window style
					style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
					style &= ~(WinApi.WS_BORDER);
					WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );

					// disable WS_EX_CLIENTEDGE window style
					exStyle = WinApi.GetWindowLong( Handle, WinApi.GWL_EXSTYLE ).ToInt64();
					exStyle &= ~(WinApi.WS_EX_CLIENTEDGE);
					WinApi.SetWindowLong( Handle, WinApi.GWL_EXSTYLE, new IntPtr(exStyle) );
				}

				// remember the last style
				// and force to redraw border by recalculating window dimension
				_BorderStyle = value;
				WinApi.SetWindowPos( Handle, IntPtr.Zero, Left, Top, Width, Height, WinApi.SWP_FRAMECHANGED );
			}
		}

		/// <summary>
		/// Gets or sets the index of the first visible (graphically top most) line
		/// of currently active document.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets the index of the first visible (graphically top most) line
		/// of currently active document.
		/// </para>
		/// <para>
		/// This property is just a synonym of Document.ViewParam.FirstVisibleLine
		/// so changing Document property will also changes this property value.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.ViewParam">Document.ViewParam</seealso>
		/// <seealso cref="Sgry.Azuki.ViewParam.FirstVisibleLine">ViewParam.FirstVisibleLine</seealso>
		public int FirstVisibleLine
		{
			get{ return Document.ViewParam.FirstVisibleLine; }
			set{ Document.ViewParam.FirstVisibleLine = value; }
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
		/// Gets or sets whether to show horizontal ruler or not.
		/// </summary>
#		if !PocketPC
		[Category("Drawing")]
		[DefaultValue(false)]
#		endif
		public bool ShowsHRuler
		{
			get{ return View.ShowsHRuler; }
			set
			{
				if( View.ShowsHRuler != value )
				{
					View.ShowsHRuler = value;
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
				_ShowsHScrollBar = value;

				// make new style bits
				long style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
				if( _ShowsHScrollBar )
					style |= WinApi.WS_HSCROLL;
				else
					style &= ~(WinApi.WS_HSCROLL);

				// apply
				WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );
				WinApi.SetWindowPos( Handle, IntPtr.Zero, Left, Top, Width, Height, WinApi.SWP_FRAMECHANGED );
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
		/// Gets or sets tab width in count of space characters.
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
		/// Gets distance between lines in pixel.
		/// </summary>
#		if !PocketPC
		[Browsable(false)]
#		endif
		public int LineSpacing
		{
			get{ return View.LineSpacing; }
		}

		/// <summary>
		/// Sets width of the content area (including line number area).
		/// </summary>
#		if !PocketPC
		[Browsable(true)]
		[Category("Drawing")]
		[Description("Width of the text content area. In proportional view, highlight line will be drawn in this width and this will be automatically expanded to enough width to show the input text. In wrapped-proportional view, text will be wrapped in this width.")]
#		endif
		public int ViewWidth
		{
			get{ return _Impl.View.TextAreaWidth + _Impl.View.XofTextArea; }
			set
			{
				_Impl.View.TextAreaWidth = value - _Impl.View.XofTextArea;
				UpdateCaretGraphic();
				UpdateScrollBarRange(); // (needed for PropWrapView)
				Refresh();
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
		
		#region IUserInterface - Behavior
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
		/// <remarks>
		/// <para>
		/// This property gets or sets a delegate object to execute auto-indentation.
		/// There are some built-in auto-indentation hook delegates
		/// declared as members of
		/// <see cref="Sgry.Azuki.AutoIndentHooks">AutoIndentHooks</see> class.
		/// Use one of the member of AutoIndentHooks or user-made hook to enable auto-indentation,
		/// otherwise, set null to this property to disable auto-indentation.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.AutoIndentHooks">AutoIndentHooks</seealso>
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
		/// Gets or sets whether tab characters are used for indentation, instead of space characters.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property is replaced with
		/// <see cref="Sgry.Azuki.Windows.AzukiControl.UsesTabForIndent">UsesTabForIndent</see>
		/// property and is now obsoleted.
		/// Use
		/// <see cref="Sgry.Azuki.Windows.AzukiControl.UsesTabForIndent">UsesTabForIndent</see>
		/// property instead.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Windows.AzukiControl.UsesTabForIndent">UsesTabForIndent</seealso>
#		if !PocketPC
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If false, tab characters are used for indentation, instead of space characters.")]
#		endif
		[Obsolete("Please use UsesTabForIndent property instead.", false)]
		public bool ConvertsTabToSpaces
		{
			get{ return !UsesTabForIndent; }
			set{ UsesTabForIndent = !(value); }
		}

		/// <summary>
		/// Gets or sets whether tab characters are used for indentation, instead of space characters.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether tab characters are used for indentation,
		/// instead of space characters.
		/// </para>
		/// <para>
		/// In addition to the case of inserting a new tab character,
		/// This property affects some other cases like next:
		/// </para>
		/// <list type="bullet">
		///		<item>
		///		When executing block-indent.
		///		</item>
		///		<item>
		///		When additional indent characters are needed.
		///		This case is about auto-indentation for specific syntax such as C/C++ language
		///		(term <term>smart-indentation</term> is more appropriate here.)
		///		In C/C++, if user hits the Enter key on a line
		///		that ends with a closing curly bracket (<c> } </c>),
		///		newly generated line will be indented one more level
		///		by inserting additional indent characters.
		///		</item>
		///		<item>
		///		When pasting rectangle selection data.
		///		Let's suppose pasting the text data
		///		when the caret is at end of a long line
		///		and the lines below is shorter than the line caret is at.
		///		In this case, whitespaces will be appended automatically
		///		for the lines below as a padding to make pasted result a 'rectangle.'
		///		</item>
		/// </list>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Windows.AzukiControl.TabWidth">AzukiControl.TabWidth property</seealso>
		/// <seealso cref="Sgry.Azuki.Actions.BlockIndent">Actions.BlockIndent action</seealso>
		/// <seealso cref="Sgry.Azuki.Actions.BlockUnIndent">Actions.BlockUnIndent action</seealso>
#		if !PocketPC
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("If true, tab characters are used for indentation, instead of space characters.")]
#		endif
		public bool UsesTabForIndent
		{
			get{ return _Impl.UsesTabForIndent; }
			set{ _Impl.UsesTabForIndent = value; }
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
		/// <remarks>
		/// The default value is true.
		/// </remarks>
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
		/// <remarks>
		/// The default value is true.
		/// </remarks>
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
		/// <remarks>
		/// <para>
		/// This method restores the modification lastly done for currently active document.
		/// If there is no UNDOable action, this method will do nothing.
		/// </para>
		/// <para>
		/// To get whether any UNDOable action exists or not,
		/// use <see cref="Sgry.Azuki.Windows.AzukiControl.CanUndo">CanUndo</see> property.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Windows.AzukiControl.CanUndo">AzukiControl.CanUndo property</seealso>
		/// <seealso cref="Sgry.Azuki.Document.Undo">Document.Undo method</seealso>
		public void Undo()
		{
			Actions.Undo( this );
		}

		/// <summary>
		/// Gets whether an available UNDO action exists or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets whether one or more UNDOable action exists or not.
		/// </para>
		/// <para>
		/// To execute UNDO, use <see cref="Sgry.Azuki.Windows.AzukiControl.Undo">Undo</see> method.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Windows.AzukiControl.Undo">AzukiControl.Undo method</seealso>
		/// <seealso cref="Sgry.Azuki.Document.CanUndo">Document.CanUndo property</seealso>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public bool CanUndo
		{
			get{ return Document.CanUndo; }
		}

		/// <summary>
		/// Clears all stacked edit histories.
		/// </summary>
		public void ClearHistory()
		{
			Document.ClearHistory();
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
			get{ return Document.IsRecordingHistory; }
			set{ Document.IsRecordingHistory = value; }
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
			get{ return Document.CanRedo; }
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
			get{ return Document.CaretIndex; }
		}

		/// <summary>
		/// Sets selection range and update the desired column.
		/// </summary>
		/// <param name="anchor">the position where the selection begins</param>
		/// <param name="caret">the position where the caret is</param>
		/// <remarks>
		/// <para>
		/// This method sets the selection range and also updates
		/// the desired column.
		/// </para>
		/// <para>
		/// The desired column is the column index
		/// that Azuki tries to set next caret position
		/// when the caret moves up or down.
		/// </para>
		/// </remarks>
		public void SetSelection( int anchor, int caret )
		{
			Document.SetSelection( anchor, caret );
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
			Document.GetSelection( out begin, out end );
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
				if( Document == null )
					return null;

				return Document.Text;
			}
			set
			{
				if( Document == null )
					return;

				Document.Text = value;
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
			get{ return Document.Length; }
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
			return Document.GetTextInRange( begin, end );
		}

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
			return _Impl.GetSelectedText();
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
			get{ return Document.LineCount; }
		}
		#endregion

		#region IUserInterface - Position / Index Conversion
		/// <summary>
		/// Calculates screen location of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
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
		/// <returns>The location of the character at specified index.</returns>
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
		/// <returns>The index of the character at specified location.</returns>
		public int GetIndexFromPosition( Point pt )
		{
			View.ScreenToVirtual( ref pt );
			return View.GetIndexFromVirPos( pt );
		}

		/// <summary>
		/// Calculates location of character at specified index in horizontal ruler index.
		/// </summary>
		/// <param name="charIndex">The index of the character to calculate its location.</param>
		/// <returns>Horizontal ruler index of the character.</returns>
		/// <remarks>
		/// <para>
		/// This method calculates location of character at specified index
		/// in horizontal ruler index.
		/// </para>
		/// <para>
		/// 'Horizontal ruler index' here means how many small lines drawn on the horizontal ruler
		/// exist between left-end of the text area
		/// and the character at index specified by <paramref name="charIndex"/>.
		/// This value is zero-based index.
		/// </para>
		/// </remarks>
		public int GetHRulerIndex( int charIndex )
		{
			return View.GetHRulerIndex( charIndex );
		}

		/// <summary>
		/// Calculates location of character at specified index in horizontal ruler index.
		/// </summary>
		/// <param name="lineIndex">The line index of the character to calculate its location.</param>
		/// <param name="columnIndex">The column index of the character to calculate its location.</param>
		/// <returns>Horizontal ruler index of the character.</returns>
		/// <remarks>
		/// <para>
		/// This method calculates location of character at specified index
		/// in horizontal ruler index.
		/// </para>
		/// <para>
		/// 'Horizontal ruler index' here means how many small lines drawn on the horizontal ruler
		/// exist between left-end of the text area
		/// and the character at index specified by <paramref name="charIndex"/>.
		/// This value is zero-based index.
		/// </para>
		/// </remarks>
		public int GetHRulerIndex( int lineIndex, int columnIndex )
		{
			return View.GetHRulerIndex( lineIndex, columnIndex );
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
			WinApi.SetScrollPos( Handle, false, _Impl.View.FirstVisibleLine );
			WinApi.SetScrollPos( Handle, true, _Impl.View.ScrollPosX );
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
		/// Gets or sets highlighter object to highlight currently active document
		/// or null to disable highlighting.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets highlighter for this document.
		/// </para>
		/// <para>
		/// Highlighter objects are used to highlight syntax of documents.
		/// They implements
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter">IHighlighter</see>
		/// interface and called
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter.Highlight(Sgry.Azuki.Document, ref int, ref int)">Highlight</see>
		/// method every time slightly after user input stopped to execute own highlighting logic.
		/// If null was set to this property, highlighting feature will be disabled.
		/// </para>
		/// <para>
		/// Azuki provides some built-in highlighters.
		/// See
		/// <see cref="Sgry.Azuki.Highlighter.Highlighters">Highlighter.Highlighters</see>
		/// class members.
		/// </para>
		/// <para>
		/// User can create and use custom highlighter object.
		/// If you want to create a keyword-based highlighter,
		/// you can extend
		/// <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter">KeywordHighlighter</see>.
		/// If you want ot create not a keyword based one,
		/// create a class which implements
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter">IHighlighter</see>
		/// and write your own highlighting logic.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Highlighter.Highlighters">Highlighter.Highlighters</seealso>
#		if !PocketPC
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#		endif
		public IHighlighter Highlighter
		{
			get{ return _Impl.Highlighter; }
			set{ _Impl.Highlighter = value; }
		}

		/// <summary>
		/// Gets version of Azuki.dll.
		/// </summary>
		public Version Version
		{
			get
			{
				return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			}
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
				newPos = Document.LineCount - 1;
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
			int newPos = _Impl.View.ScrollPosX;
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
			
			int delta = newPos - _Impl.View.ScrollPosX;
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
			_Impl.View.HandleSizeChanged( ClientSize );
			Invalidate();
		}
		#endregion

		#region IME Reconversion
		unsafe int HandleImeReconversion( WinApi.RECONVERTSTRING* reconv )
		{
			/*
			 * There are three 'string's on executing IME reconversion.
			 * First is target string.
			 * This string will be reconverted.
			 * Second is composition string.
			 * This contains target string and MAY be the target string.
			 * Third is, string body.
			 * This contains composition string and MAY be the composition string.
			 * Typical usage is set each range as next:
			 * 
			 * - target string: selected text or adjusted by IME
			 * - composition string: selected text or adjusted by IME
			 * - string body: selected text or current line or entire text buffer it self.
			 * 
			 * IME reconversion takes two steps to be executed.
			 * On first step, IME sends WM_IME_REQUEST with IMR_RECONVERTSTRING parameter to a window
			 * to query size of string body (plus RECONVERTSTRING structure).
			 * This time, Azuki returns selected text length or length of current line if nothing selected.
			 * On second step, IME allocates memory block to store
			 * RECONVERTSTRING structure and string body for the application.
			 * This time, Azuki copies string body to the buffer and
			 * set structure members and return non-zero value (meaning OK).
			 * Then, IME will execute reconversion.
			 */
			const int MaxRangeLength = 40;
			int		rc;
			int		selBegin, selEnd;
			string	stringBody;
			int		stringBodyIndex;
			IntPtr	ime;
			char*	strPos;
			int		infoBufSize;

			// determine string body
			Document.GetSelection( out selBegin, out selEnd );
			if( selBegin != selEnd )
			{
				// something selected.
				// set them as string body, composition string, and target string.
				int end;

				// shurink range if it is unreasonably big
				end = selEnd;
				if( MaxRangeLength < end - selBegin )
				{
					end = selBegin + MaxRangeLength;
					if( end < Document.Length
						&& Document.IsLowSurrogate(Document[end]) )
					{
						end--;
					}
				}

				// get selected text
				stringBody = Document.GetTextInRange( selBegin, end );
				stringBodyIndex = selBegin;
			}
			else
			{
				// nothing selected.
				// set current line as string body
				// and let IME to determine composition string and target string.
				int lineIndex, lineHeadIndex, lineEndIndex;
				int begin, end;

				// get current line range
				lineIndex = Document.GetLineIndexFromCharIndex( selBegin );
				lineHeadIndex = Document.GetLineHeadIndex( lineIndex );
				lineEndIndex = lineHeadIndex + Document.GetLineLength( lineIndex );
				begin = Math.Max( lineHeadIndex, selBegin - (MaxRangeLength / 2) );
				end = Math.Min( selBegin + (MaxRangeLength / 2), lineEndIndex );
				if( end < Document.Length && Document.IsLowSurrogate(Document[end]) )
				{
					end--;
				}

				// get current line content
				stringBody = Document.GetTextInRange( begin, end );
				stringBodyIndex = begin;
			}

			// calculate size of information buffer to communicate with IME
			infoBufSize = sizeof(WinApi.RECONVERTSTRING)
					+ Encoding.Unicode.GetByteCount(stringBody) + 1;
			if( reconv == null )
			{
				// this is the first call for re-conversion.
				// just inform IME the size of information buffer this time.
				return infoBufSize;
			}

			// validate parameters
			if( reconv->dwSize != (UInt32)infoBufSize
				|| reconv->dwVersion != 0 )
			{
				return 0;
			}

			// get IME context
			ime = WinApi.ImmGetContext( this.Handle );
			if( ime == IntPtr.Zero )
			{
				return 0;
			}

			// copy string body
			reconv->dwStrLen = (UInt32)stringBody.Length;
			reconv->dwStrOffset = (UInt32)sizeof(WinApi.RECONVERTSTRING);
			strPos = (char*)( (byte*)reconv + reconv->dwStrOffset );
			for( int i=0; i<stringBody.Length; i++ )
			{
				strPos[i] = stringBody[i];
			}
			strPos[stringBody.Length] = '\0';

			// calculate range of composition string and target string
			if( selBegin != selEnd )
			{
				// set selected range as reconversion target
				reconv->dwCompStrLen = (UInt32)( stringBody.Length );
				reconv->dwCompStrOffset = 0;
				reconv->dwTargetStrLen = reconv->dwCompStrLen;
				reconv->dwTargetStrOffset = reconv->dwCompStrOffset;
			}
			else
			{
				// let IME adjust RECONVERTSTRING parameters
				reconv->dwCompStrLen = 0;
				reconv->dwCompStrOffset = (UInt32)( (selBegin - stringBodyIndex) * 2);
				reconv->dwTargetStrLen = 0;
				reconv->dwTargetStrOffset = reconv->dwCompStrOffset;
				rc = WinApi.ImmSetCompositionStringW(
						ime,
						WinApi.SCS_QUERYRECONVERTSTRING,
						reconv, (uint)infoBufSize, null, 0
					);
				if( rc == 0 )
				{
					return 0;
				}
			}

			// select target string to make it being replaced by reconverted new string
			selBegin = stringBodyIndex + (int)(reconv->dwTargetStrOffset / 2);
			selEnd = selBegin + (int)reconv->dwTargetStrLen;
			Document.SetSelection( selBegin, selEnd );

			// adjust position of IME composition window
			WinApi.SetImeWindowPos( this.Handle,
					GetPositionFromIndex(selBegin)
				);

			// release context object
			WinApi.ImmReleaseContext( this.Handle, ime );

			return infoBufSize;
		}
		#endregion

		#region Behavior as a .NET Control
		/// <summary>
		/// Gets or sets default text color.
		/// </summary>
		/// <remarks>
		/// This property gets or sets default foreground color.
		/// Note that this is a synonym of
		/// <see cref="Sgry.Azuki.Windows.AzukiControl.ColorScheme">AzukiControl.ColorScheme</see>.BackColor
		/// .
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Windows.AzukiControl.ColorScheme">AzukiControl.ColorScheme</seealso>
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
		/// <remarks>
		/// This property gets or sets default background color.
		/// Note that this is a synonym of
		/// <see cref="Sgry.Azuki.Windows.AzukiControl.ColorScheme">AzukiControl.ColorScheme</see>.BackColor
		/// .
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Windows.AzukiControl.ColorScheme">AzukiControl.ColorScheme</seealso>
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
		
		/// <summary>
		/// Gets or sets whether this control uses Ctrl+Tab and Ctrl+Shift+Tab
		/// for moving focus to other controls in a dialog.
		/// </summary>
		/// <remarks>
		/// The default value is true.
		/// </remarks>
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
		IntPtr CustomWndProc( IntPtr window, UInt32 message, IntPtr wParam, IntPtr lParam )
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
					}

					// return zero here to prevent executing original painting logic of Control class.
					// (if the original logic runs,
					// we will get invalid(?) update region from BeginPaint API in Windows XP or former.)
					return IntPtr.Zero;
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

					// get mouse cursor pos
					pos.X = (short)( (lParam.ToInt32()      ) & 0xffff );
					pos.Y = (short)( (lParam.ToInt32() >> 16) & 0xffff );

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
#						if !PocketPC
						if( alt )
						{
							this.Cursor = Cursors.Arrow;
						}
#						endif
					}
					else if( message == WinApi.WM_LBUTTONUP || message == WinApi.WM_RBUTTONUP )
					{
#						if !PocketPC
						this.Cursor = Cursors.IBeam;
#						endif
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

					const int threashold = 120;
					int linesPerWheel;
					Int16 wheelDelta;
					int scrollCount;

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
					scrollCount = _WheelPos / threashold;
					_WheelPos = _WheelPos % threashold;
					if( 0 != scrollCount )
					{
						HandleWheelEvent( -(linesPerWheel * scrollCount) );
					}
				}
				else if( message == WinApi.WM_IME_STARTCOMPOSITION )
				{
					// move IMM window to caret position
					WinApi.SetImeWindowFont( Handle, Font );
				}
				else if( message == WinApi.WM_IME_ENDCOMPOSITION )
				{
					// put down the flag to ignore character input event
					_InputSuppressLimit = 0;
				}
				else if( message == WinApi.WM_IME_COMPOSITION )
				{
					// if this event notifies that a portion of the composition was completed,
					// get the partial string and insert it to the document
					if( (lParam.ToInt64() & WinApi.GCS_RESULTSTR) != 0 )
					{
						const long suppressingSpan = 500; // 50 milliseconds

						IntPtr imc = WinApi.ImmGetContext( Handle );
						unsafe
						{
							char[] buf;
							Int32 bufSize;

							// prepare buffer to receive composition result
							bufSize = WinApi.ImmGetCompositionStringW( imc, WinApi.GCS_RESULTSTR, null, 0 );
							buf = new char[ bufSize/2 ];
							fixed( void* p = buf )
							{
								WinApi.ImmGetCompositionStringW( imc, WinApi.GCS_RESULTSTR, p, (uint)bufSize );
							}

							// insert it to the active document
							if( Document.IsReadOnly == false )
							{
								Document.Replace( new String(buf) );
							}
						}
						WinApi.ImmReleaseContext( Handle, imc );

						// Record current time stamp to ignore following WM_IME_CHAR/WM_CHAR.
						// Because composition result will be sent in form of
						// WM_IME_CHAR (or WM_CHAR in WinCE) sequence
						// just after WM_IME_COMPOSITION with GCS_RESULTSTR,
						// suppress input for a while
						_InputSuppressLimit = DateTime.Now.Ticks + suppressingSpan;
					}
				}
				else if( message == WinApi.WM_IME_CHAR )
				{
#					if !PocketPC
					if( DateTime.Now.Ticks < _InputSuppressLimit )
						return IntPtr.Zero;
#					endif
				}
				else if( message == WinApi.WM_CHAR )
				{
#					if PocketPC
					if( DateTime.Now.Ticks < _InputSuppressLimit )
						return IntPtr.Zero;
#					endif
				}
				else if( message == WinApi.WM_IME_REQUEST
					&& wParam.ToInt64() == (long)WinApi.IMR_RECONVERTSTRING )
				{
					int rc;

					unsafe {
						rc = HandleImeReconversion( (WinApi.RECONVERTSTRING*)lParam.ToPointer() );
					}

					return new IntPtr( rc );
				}
#				if PocketPC
				else if( message == WinApi.WM_KEYDOWN )
				{
					// Default behavior of .NET Control class for WM_KEYDOWN
					// moves focus and there seems to be no way to prevent moving focus on WinCE env.
					// Thus here I hook WM_KEYDOWN message and control focus
					Keys keyCode = (Keys)wParam.ToInt32();
					Keys keyData = keyCode | WinApi.GetCurrentModifierKeyStates();
					if( keyData == (Keys.Tab)
						&& AcceptsTab )
					{
						base.OnKeyDown( new KeyEventArgs(keyData) );
						return IntPtr.Zero;
					}
					if( keyData == (Keys.Tab | Keys.Control)
						&& UseCtrlTabToMoveFocus == false )
					{
						base.OnKeyDown( new KeyEventArgs(keyData) );
						return IntPtr.Zero;
					}
				}
#				endif
			}
			catch( Exception ex )
			{
				// because window proc was overwritten,
				// exceptions thrown in this method can not be handled well.
				// so we catch them here.
				Console.Error.WriteLine( ex );
#				if DEBUG
				MessageBox.Show( ex.ToString(), "azuki bug" );
#				endif
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
