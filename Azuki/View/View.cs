// file: View.cs
// brief: Platform independent view implementation of Azuki engine.
// author: YAMAMOTO Suguru
// update: 2011-07-07
//=========================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using StringBuilder = System.Text.StringBuilder;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// Platform independent view of Azuki.
	/// </summary>
	abstract partial class View : IView, IDisposable
	{
		#region Fields and Types
		const float GoldenRatio = 1.6180339887f;
		const int DefaultTabWidth = 8;
		const int MinimumFontSize = 1;
		const int LineNumberAreaPadding = 2;
		static readonly int[] _LineNumberSamples = new int[] {
			9999,
			99999,
			999999,
			9999999,
			99999999,
			999999999,
			2000000000
		};
		protected IUserInterface _UI;
		int _TextAreaWidth = 4096;
		int _MinimumTextAreaWidth = 300;
		Size _VisibleSize = new Size( 300, 300 );
		int _LastUsedLineNumberSample = _LineNumberSamples[0];
		protected int _LineNumAreaWidth = 0;// Width of the line number area in pixel
		int _SpaceWidth; 					// Width of a space char (U+0020) in pixel
		protected int _FullSpaceWidth = 0;	// Width of a full-width space char (U+3000) in pixel
		int _LineHeight;
		int _LinePadding = 1;
		int _TabWidth = DefaultTabWidth;
		int _TabWidthInPx;
		int _XCharWidth;
		int _DirtBarWidth;
		int _HRulerHeight;	// height of the largest lines of the horizontal ruler
		int _HRulerY_5;		// height of the middle lines of the horizontal ruler
		int _HRulerY_1;		// height of the smallest lines of the horizontal ruler
		int _HRulerTextHeight;
		HRulerIndicatorType _HRulerIndicatorType = HRulerIndicatorType.Segment;

		ColorScheme _ColorScheme = ColorScheme.Default;
		FontInfo _Font = new FontInfo( "Courier New", 10, FontStyle.Regular );
		FontInfo _HRulerFont;
		int _TopMargin = 1;
		int _LeftMargin = 1;
		DrawingOption _DrawingOption
			= DrawingOption.DrawsTab
			| DrawingOption.DrawsFullWidthSpace
			| DrawingOption.DrawsEol
			| DrawingOption.HighlightCurrentLine
			| DrawingOption.ShowsLineNumber
			| DrawingOption.ShowsDirtBar
			| DrawingOption.HighlightsMatchedBracket;
		bool _ScrollsBeyondLastLine = true;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="ui">Implementation of the platform dependent UI module.</param>
		internal View( IUserInterface ui )
		{
			Debug.Assert( ui != null );
			_UI = ui;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="other">another view object to inherit settings</param>
		internal View( View other )
		{
			Debug.Assert( other != null );

			// inherit reference to the UI module
			this._UI = other._UI;

			// inherit other parameters
			if( other != null )
			{
				this._ColorScheme = new ColorScheme( other._ColorScheme );
				this._DrawingOption = other._DrawingOption;
				//DO_NOT//this._DirtBarWidth = other._DirtBarWidth;
				//DO_NOT//this._HRulerFont = other._HRulerFont;
				//DO_NOT//this._LCharWidth = other._LCharWidth;
				//DO_NOT//this._LineHeight = other._LineHeight;
				//DO_NOT//this._LineNumAreaWidth = other._LineNumAreaWidth;
				//DO_NOT//this._SpaceWidth = other._SpaceWidth;
				this._TabWidth = other._TabWidth;
				this._LinePadding = other._LinePadding;
				this._LeftMargin = other._LeftMargin;
				this._TopMargin = other.TopMargin;
				//DO_NOT//this._TabWidthInPx = other._TabWidthInPx;
				this._TextAreaWidth = other._TextAreaWidth;
				//DO_NOT//this._UI = other._UI;
				this._VisibleSize = other._VisibleSize;

				// set Font through property
				if( other.FontInfo != null )
					this.FontInfo = other.FontInfo;

				// re-calculate graphic metrics
				// (because there is a metric which needs a reference to Document to be calculated
				// but it cannnot be set Document before setting Font by structural reason)
				using( IGraphics g = _UI.GetIGraphics() )
				{
					UpdateMetrics( g );
				}
			}
		}

		/// <summary>
		/// Disposes resources.
		/// </summary>
		public virtual void Dispose()
		{
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets the document displayed in this view.
		/// </summary>
		public virtual Document Document
		{
			get{ return _UI.Document; }
		}

		/// <summary>
		/// Gets number of the screen lines.
		/// </summary>
		public abstract int LineCount
		{
			get;
		}

		/// <summary>
		/// Gets or sets width of the virtual text area (line number area is not included).
		/// </summary>
		public virtual int TextAreaWidth
		{
			get{ return _TextAreaWidth; }
			set
			{
				if( value < _MinimumTextAreaWidth )
				{
					value = _MinimumTextAreaWidth;
				}
				_TextAreaWidth = value;
			}
		}

		/// <summary>
		/// Re-calculates and updates x-coordinate of the right end of the virtual text area.
		/// </summary>
		/// <param name="desiredX">X-coordinate of scroll destination desired.</param>
		/// <returns>The largest X-coordinate which Azuki can scroll to.</returns>
		protected abstract int ReCalcRightEndOfTextArea( int desiredX );

		/// <summary>
		/// Gets or sets size of the currently visible area (line number area is included).
		/// </summary>
		public Size VisibleSize
		{
			get{ return _VisibleSize; }
			set{ _VisibleSize = value; }
		}

		/// <summary>
		/// Gets or sets size of the currently visible size of the text area (line number area is not included).
		/// </summary>
		public Size VisibleTextAreaSize
		{
			get
			{
				Size size = _VisibleSize;
				size.Width -= XofTextArea;
				size.Height -= YofTextArea;
				return size;
			}
		}

		/// <summary>
		/// Gets or sets whether to scroll beyond the last line of the document or not.
		/// </summary>
		public bool ScrollsBeyondLastLine
		{
			get{ return _ScrollsBeyondLastLine; }
			set{ _ScrollsBeyondLastLine = value; }
		}

		/// <summary>
		/// Gets or sets the font used for drawing text.
		/// </summary>
		public virtual FontInfo FontInfo
		{
			get{ return _Font; }
			set
			{
				if( value == null )
					throw new ArgumentNullException( "View.FontInfo was set to null." );

				// apply font
				_Font = value;
				_HRulerFont = new FontInfo(
						value.Name,
						(int)( value.Size / GoldenRatio ),
						FontStyle.Regular
					);

				// update font metrics and graphic
				using( IGraphics g = _UI.GetIGraphics() )
				{
					UpdateMetrics( g );
				}
				Invalidate();
				_UI.UpdateCaretGraphic();
			}
		}

		protected void UpdateMetrics( IGraphics g )
		{
			StringBuilder buf = new StringBuilder( 32 );
			_LastUsedLineNumberSample = _LineNumberSamples[0];

			// calculate tab width in pixel
			for( int i=0; i<_TabWidth; i++ )
			{
				buf.Append( ' ' );
			}
			_TabWidthInPx = g.MeasureText( buf.ToString() ).Width;

			// update other metrics
			_SpaceWidth = g.MeasureText( " " ).Width;
			_XCharWidth = g.MeasureText( "x" ).Width;
			_FullSpaceWidth = g.MeasureText( "\x3000" ).Width;
			_LineHeight = g.MeasureText( "Mp" ).Height;
			if( this.Document != null )
			{
				_LastUsedLineNumberSample = Document.ViewParam.MaxLineNumber;
			}
			_LineNumAreaWidth
				= g.MeasureText( _LastUsedLineNumberSample.ToString() ).Width + _SpaceWidth;
			_DirtBarWidth = Math.Max( 3, _SpaceWidth >> 1 );

			// update metrics related with horizontal ruler
			_HRulerHeight = (int)( _LineHeight / GoldenRatio ) + 2;
			_HRulerY_5 = (int)( _HRulerHeight / (GoldenRatio * GoldenRatio) );
			_HRulerY_1 = (int)( _HRulerHeight / (GoldenRatio) );
			g.FontInfo = _HRulerFont;
			_HRulerTextHeight = (int)( g.MeasureText("Mp").Height * 0.97 );
			g.FontInfo = _Font;

			// calculate minimum text area width
			_MinimumTextAreaWidth = Math.Max( _FullSpaceWidth, TabWidthInPx ) << 1;
		}
		#endregion

		/// <summary>
		/// Gets length of the pysical line.
		/// </summary>
		/// <param name="lineIndex">Index of the line of which to get the length.</param>
		/// <returns>Length of the specified line in character count.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetLineLength( int lineIndex )
		{
			if( lineIndex < 0 || LineCount <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid line index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			Document doc = Document;
			int lineHeadIndex, lineEndIndex;

			lineHeadIndex = GetLineHeadIndex( lineIndex );
			if( lineIndex+1 < LineCount )
			{
				lineEndIndex = GetLineHeadIndex( lineIndex + 1 );
				if( 0 <= lineEndIndex-1 && doc.GetCharAt(lineEndIndex-1) == '\n'
					&& 0 <= lineEndIndex-2 && doc.GetCharAt(lineEndIndex-2) == '\r' )
				{
					lineEndIndex -= 2; // CR+LF
				}
				else
				{
					lineEndIndex -= 1; // CR or LF
				}
			}
			else
			{
				lineEndIndex = doc.Length;
			}

			return lineEndIndex - lineHeadIndex;
		}

		#region Drawing Options
		/// <summary>
		/// Gets or sets top margin of the view in pixel.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
		public int TopMargin
		{
			get{ return _TopMargin; }
			set
			{
				if( value < 0 )
					throw new ArgumentOutOfRangeException( "value", "TopMargin must not be a negative number (value:"+value+")" );

				// apply value
				_TopMargin = value;

				// send dummy scroll event
				// to update screen position of the caret
				_UI.Scroll( Rectangle.Empty, 0, 0 );
			}
		}

		/// <summary>
		/// Gets or sets left margin of the view in pixel.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
		public int LeftMargin
		{
			get{ return _LeftMargin; }
			set
			{
				if( value < 0 )
					throw new ArgumentOutOfRangeException( "value", "LeftMargin must not be a negative number (value:"+value+")" );

				// apply value
				_LeftMargin = value;

				// send dummy scroll event
				// to update screen position of the caret
				_UI.Scroll( Rectangle.Empty, 0, 0 );
			}
		}

		/// <summary>
		/// Gets or sets type of the indicator on the horizontal ruler.
		/// </summary>
		public HRulerIndicatorType HRulerIndicatorType
		{
			get{ return _HRulerIndicatorType; }
			set{ _HRulerIndicatorType = value; }
		}

		/// <summary>
		/// Gets or sets view options.
		/// </summary>
		public DrawingOption DrawingOption
		{
			get{ return _DrawingOption; }
			set{ _DrawingOption = value; }
		}

		/// <summary>
		/// Gets or sets whether the current line would be drawn with underline or not.
		/// </summary>
		public bool HighlightsCurrentLine
		{
			get{ return (DrawingOption & DrawingOption.HighlightCurrentLine) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.HighlightCurrentLine;
				else
					DrawingOption &= ~DrawingOption.HighlightCurrentLine;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to highlight matched bracket or not.
		/// </summary>
		public bool HighlightsMatchedBracket
		{
			get{ return (DrawingOption & DrawingOption.HighlightsMatchedBracket) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.HighlightsMatchedBracket;
				else
					DrawingOption &= ~DrawingOption.HighlightsMatchedBracket;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to show line number or not.
		/// </summary>
		public bool ShowLineNumber
		{
			get{ return (DrawingOption & DrawingOption.ShowsLineNumber) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.ShowsLineNumber;
				else
					DrawingOption &= ~DrawingOption.ShowsLineNumber;

				_UI.UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to show horizontal ruler or not.
		/// </summary>
		public bool ShowsHRuler
		{
			get{ return (DrawingOption & DrawingOption.ShowsHRuler) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.ShowsHRuler;
				else
					DrawingOption &= ~DrawingOption.ShowsHRuler;

				_UI.UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to show 'dirt bar' or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether to show 'dirt bar' or not.
		/// The 'dirt bar'
		/// </para>
		/// </remarks>
		public bool ShowsDirtBar
		{
			get{ return (DrawingOption & DrawingOption.ShowsDirtBar) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.ShowsDirtBar;
				else
					DrawingOption &= ~DrawingOption.ShowsDirtBar;

				_UI.UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw half-width space with special graphic or not.
		/// </summary>
		public bool DrawsSpace
		{
			get{ return (DrawingOption & DrawingOption.DrawsSpace) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsSpace;
				else
					DrawingOption &= ~DrawingOption.DrawsSpace;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw full-width space with special graphic or not.
		/// </summary>
		public bool DrawsFullWidthSpace
		{
			get{ return (DrawingOption & DrawingOption.DrawsFullWidthSpace) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsFullWidthSpace;
				else
					DrawingOption &= ~DrawingOption.DrawsFullWidthSpace;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw tab character with special graphic or not.
		/// </summary>
		public bool DrawsTab
		{
			get{ return (DrawingOption & DrawingOption.DrawsTab) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsTab;
				else
					DrawingOption &= ~DrawingOption.DrawsTab;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw EOL code with special graphic or not.
		/// </summary>
		public bool DrawsEolCode
		{
			get{ return (DrawingOption & DrawingOption.DrawsEol) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsEol;
				else
					DrawingOption &= ~DrawingOption.DrawsEol;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw EOF mark by special graphic or not.
		/// </summary>
		public bool DrawsEofMark
		{
			get{ return (DrawingOption & DrawingOption.DrawsEof) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsEof;
				else
					DrawingOption &= ~DrawingOption.DrawsEof;
				Invalidate();
			}
		}

		/// <summary>
		/// Color set used for displaying text.
		/// </summary>
		public ColorScheme ColorScheme
		{
			get{ return _ColorScheme; }
			set
			{
				if( value == null )
					value = ColorScheme.Default;

				_ColorScheme = value;
			}
		}

		/// <summary>
		/// Gets or sets tab width in count of space chars.
		/// </summary>
		public virtual int TabWidth
		{
			get{ return _TabWidth; }
			set
			{
				if( value <= 0 )
					throw new ArgumentOutOfRangeException( "value", "TabWidth must not be a negative number (given value:"+value+".)" );

				using( IGraphics g = _UI.GetIGraphics() )
				{
					_TabWidth = value;
					UpdateMetrics( g );
					Invalidate();
				}
			}
		}
		
		/// <summary>
		/// Gets width of tab character (U+0009) in pixel.
		/// </summary>
		public int TabWidthInPx
		{
			get{ return _TabWidthInPx; }
		}

		/// <summary>
		/// Gets width of space character (U+0020) in pixel.
		/// </summary>
		public int SpaceWidthInPx
		{
			get{ return _SpaceWidth; }
		}
		#endregion

		#region States
		/// <summary>
		/// Gets or sets index of the line which is displayed at top of this view.
		/// </summary>
		/// <remarks>
		/// This property simply accesses Document.ViewParam.FirstVisibleLine property.
		/// </remarks>
		public int FirstVisibleLine
		{
			get{ return Document.ViewParam.FirstVisibleLine; }
			set{ Document.ViewParam.FirstVisibleLine = value; }
		}

		/// <summary>
		/// Gets or sets x-coordinate of the view's origin.
		/// </summary>
		/// <remarks>
		/// This property simply accesses Document.ViewParam.ScrollPosX property.
		/// </remarks>
		internal int ScrollPosX
		{
			get{ return Document.ViewParam.ScrollPosX; }
			set{ Document.ViewParam.ScrollPosX = value; }
		}

		/// <summary>
		/// Gets height of each lines in pixel.
		/// </summary>
		public int LineHeight
		{
			get{ return _LineHeight; }
		}

		/// <summary>
		/// Gets or sets size of padding between lines in pixel.
		/// </summary>
		public int LinePadding
		{
			get{ return _LinePadding; }
			set
			{
				if( value < 1 )
					value = 1;
				_LinePadding = value;
			}
		}

		/// <summary>
		/// Gets distance between lines in pixel.
		/// </summary>
		public int LineSpacing
		{
			get{ return _LineHeight+_LinePadding; }
		}

		/// <summary>
		/// Gets width of the line number area in pixel.
		/// </summary>
		public int LineNumAreaWidth
		{
			get
			{
				if( ShowLineNumber )
					return _LineNumAreaWidth;
				else
					return 0;
			}
		}

		/// <summary>
		/// Gets width of the dirt bar in pixel.
		/// </summary>
		public int DirtBarWidth
		{
			get
			{
				if( ShowsDirtBar )
					return _DirtBarWidth;
				else
					return 0;
			}
		}

		/// <summary>
		/// Gets height of the horizontal ruler.
		/// </summary>
		public int HRulerHeight
		{
			get
			{
				if( ShowsHRuler )
					return _HRulerHeight;
				else
					return 0;
			}
		}

		/// <summary>
		/// Gets distance between lines on the horizontal ruler.
		/// </summary>
		public int HRulerUnitWidth
		{
			get{ return _XCharWidth; }
		}
		#endregion

		#region Desired Column Management
		/// <summary>
		/// Sets column index of the current caret position to "desired column" value.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When the caret moves up or down,
		/// Azuki tries to set next caret's column index to this value.
		/// Note that "desired column" is associated with each document
		/// so this value may change when Document property was set to another document.
		/// </para>
		/// </remarks>
		public void SetDesiredColumn()
		{
			using( IGraphics g = _UI.GetIGraphics() )
			{
				SetDesiredColumn( g );
			}
		}

		/// <summary>
		/// Sets column index of the current caret position to "desired column" value.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Normally the caret tries to keep its x-coordinate
		/// on moving line to line unless user explicitly changes x-coordinate of it.
		/// The term 'Desired Column' means this x-coordinate which the caret tries to stick close to.
		/// </para>
		/// <para>
		/// Note that the desired column is associated with each document.
		/// </para>
		/// </remarks>
		public void SetDesiredColumn( IGraphics g )
		{
			Document.ViewParam.DesiredColumnX = GetVirPosFromIndex( g, Document.CaretIndex ).X;
		}

		/// <summary>
		/// Gets current "desired column" value.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Normally the caret tries to keep its x-coordinate
		/// on moving line to line unless user explicitly changes x-coordinate of it.
		/// The term 'Desired Column' means this x-coordinate which the caret tries to stick close to.
		/// </para>
		/// <para>
		/// Note that the desired column is associated with each document.
		/// </para>
		/// </remarks>
		public int GetDesiredColumn()
		{
			return Document.ViewParam.DesiredColumnX;
		}
		#endregion

		#region Position / Index Conversion
		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		public Point GetVirPosFromIndex( int index )
		{
			using( IGraphics g = _UI.GetIGraphics() )
			{
				return GetVirPosFromIndex( g, index );
			}
		}

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		public abstract Point GetVirPosFromIndex( IGraphics g, int index );

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		public Point GetVirPosFromIndex( int lineIndex, int columnIndex )
		{
			using( IGraphics g = _UI.GetIGraphics() )
			{
				return GetVirPosFromIndex( g, lineIndex, columnIndex );
			}
		}

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		public abstract Point GetVirPosFromIndex( IGraphics g, int lineIndex, int columnIndex );

		/// <summary>
		/// Gets char-index of the char at the point specified by location in the virtual space.
		/// </summary>
		/// <returns>The index of the char or -1 if invalid point was specified.</returns>
		public int GetIndexFromVirPos( Point pt )
		{
			using( IGraphics g = _UI.GetIGraphics() )
			{
				return GetIndexFromVirPos( g, pt );
			}
		}

		/// <summary>
		/// Gets char-index of the char at the point specified by location in the virtual space.
		/// </summary>
		/// <returns>The index of the char or -1 if invalid point was specified.</returns>
		public abstract int GetIndexFromVirPos( IGraphics g, Point pt );

		/// <summary>
		/// Converts a coordinate in virtual space to a coordinate in client area.
		/// </summary>
		public void VirtualToScreen( ref Point pt )
		{
			pt.X = (pt.X - ScrollPosX) + XofTextArea;
			pt.Y = (pt.Y - FirstVisibleLine * LineSpacing) + YofTextArea;
		}

		/// <summary>
		/// Converts a coordinate in virtual space to a coordinate in client area.
		/// </summary>
		public void VirtualToScreen( ref Rectangle rect )
		{
			rect.X = (rect.X - ScrollPosX) + XofTextArea;
			rect.Y = (rect.Y - FirstVisibleLine * LineSpacing) + YofTextArea;
		}

		/// <summary>
		/// Converts a coordinate in client area to a coordinate in virtual text area.
		/// </summary>
		public void ScreenToVirtual( ref Point pt )
		{
			pt.X = (pt.X + ScrollPosX) - XofTextArea;
			pt.Y = (pt.Y + FirstVisibleLine * LineSpacing) - YofTextArea;
		}

		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract int GetLineHeadIndex( int lineIndex );

		/// <summary>
		/// Gets the index of the first char in the screen line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract int GetLineHeadIndexFromCharIndex( int charIndex );

		/// <summary>
		/// Calculates screen line index from char-index.
		/// </summary>
		/// <param name="charIndex">The index of the line which contains the char at this parameter will be calculated.</param>
		/// <returns>The index of the line which contains the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public int GetLineIndexFromCharIndex( int charIndex )
		{
			int lineIndex, columnIndex;

			GetLineColumnIndexFromCharIndex( charIndex, out lineIndex, out columnIndex );

			return lineIndex;
		}

		/// <summary>
		/// Calculates screen line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex );

		/// <summary>
		/// Calculates char-index from screen line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex );

		/// <summary>
		/// Calculates and returns text ranges that will be selected by specified rectangle.
		/// </summary>
		/// <param name="selRect">Rectangle to be used to specify selection target.</param>
		/// <returns>Array of indexes (1st begin, 1st end, 2nd begin, 2nd end, ...)</returns>
		/// <remarks>
		/// <para>
		/// (This method is basically for internal use.
		/// I do not recommend to use this from outside of Azuki.)
		/// </para>
		/// <para>
		/// This method calculates text ranges which will be selected by given rectangle.
		/// Because mapping of character indexes and graphical position (layout) are
		/// executed by view implementations, the result of this method will be changed
		/// according to the interface implementation.
		/// </para>
		/// <para>
		/// Return value of this method is an array of text indexes
		/// that is consisted with beginning index of first text range (row),
		/// ending index of first text range,
		/// beginning index of second text range,
		/// ending index of second text range and so on.
		/// </para>
		/// </remarks>
		public int[] GetRectSelectRanges( Rectangle selRect )
		{
			List<int> selRanges = new List<int>();
			Point leftPos = new Point();
			Point rightPos = new Point();
			int leftIndex;
			int rightIndex;
			int y;
			int selRectBottom;

			// if bottom coordinate of the selection rectangle is negative value,
			// modify it to zero.
			selRectBottom = selRect.Bottom;
			if( selRect.Bottom < 0 )
			{
				selRectBottom = 0;
			}

			// get text in the rect
			leftPos.X = selRect.Left;
			rightPos.X = selRect.Right;
			y = selRect.Top - (selRect.Top % LineSpacing);
			while( y <= selRectBottom )
			{
				// calculate sub-selection range made with this line
				leftPos.Y = rightPos.Y = y;
				leftIndex = this.GetIndexFromVirPos( leftPos );
				rightIndex = this.GetIndexFromVirPos( rightPos );
				if( 1 < selRanges.Count && selRanges[selRanges.Count-1] == rightIndex )
				{
					break; // reached EOF
				}
				Debug.Assert( Document.IsNotDividableIndex(leftIndex) == false );
				Debug.Assert( Document.IsNotDividableIndex(rightIndex) == false );

				// add this sub-selection range
				selRanges.Add( leftIndex );
				selRanges.Add( rightIndex );

				// go to next line
				y += LineSpacing;
			}

			return selRanges.ToArray();
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
			Point virPos;

			if( charIndex < 0 || Document.Length < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Specified index is out of range. (value:"+charIndex+", document length:"+Document.Length+")" );

			// calculate location of the character in coordinate in virtual text area
			using( IGraphics g = _UI.GetIGraphics() )
			{
				virPos = GetVirPosFromIndex( g, charIndex );
			}

			// calculate how many smallest lines exist at left of the character
			return (virPos.X / HRulerUnitWidth);
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
			Point virPos;

			if( lineIndex < 0 || LineCount <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Specified index is out of range. (value:"+lineIndex+", line count:"+LineCount+")" );
			if( columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Specified index is out of range. (value:"+columnIndex+")" );

			// calculate location of the character in coordinate in virtual text area
			using( IGraphics g = _UI.GetIGraphics() )
			{
				virPos = GetVirPosFromIndex( g, lineIndex, columnIndex );
			}

			// calculate how many smallest lines exist at left of the character
			return (virPos.X / HRulerUnitWidth);
		}
		#endregion

		#region Operations
		/// <summary>
		/// Scroll to where the caret is.
		/// </summary>
		public void ScrollToCaret()
		{
			using( IGraphics g = _UI.GetIGraphics() )
			{
				ScrollToCaret( g );
			}
		}

		/// <summary>
		/// Scroll to where the caret is.
		/// </summary>
		public void ScrollToCaret( IGraphics g )
		{
			Rectangle threshRect = new Rectangle();
			Point caretPos;
			int vDelta = 0, hDelta;

			// make rentangle of virtual text view
			threshRect.X = ScrollPosX + SpaceWidthInPx;
			threshRect.Y = FirstVisibleLine * LineSpacing;
			threshRect.Width = (_VisibleSize.Width - XofTextArea) - (SpaceWidthInPx * 2);
			threshRect.Height = (_VisibleSize.Height - YofTextArea) - LineSpacing;

			// shrink the rectangle if some lines must be visible
			if( 0 < UserPref.AutoScrollMargin )
			{
				int yMargin = Math.Max( 0, UserPref.AutoScrollMargin * LineSpacing );
				threshRect.Y += yMargin;
				threshRect.Height -= (yMargin * 2);
			}

			// calculate caret position
			caretPos = GetVirPosFromIndex( g, Document.CaretIndex );
			if( threshRect.Left <= caretPos.X
				&& caretPos.X <= threshRect.Right
				&& threshRect.Top <= caretPos.Y
				&& caretPos.Y <= threshRect.Bottom )
			{
				return; // caret is already visible
			}

			// calculate horizontal offset to the position where we desire to scroll to
			hDelta = 0;
			if( threshRect.Right <= caretPos.X )
			{
				// scroll to right
				hDelta = caretPos.X - (threshRect.Right - TabWidthInPx);
			}
			else if( caretPos.X < threshRect.Left )
			{
				// scroll to left
				hDelta = caretPos.X - (threshRect.Left + TabWidthInPx);
			}

			// calculate vertical offset to the position where we desire to scroll to
			vDelta = 0;
			if( threshRect.Bottom <= caretPos.Y )
			{
				// scroll down
				vDelta = (caretPos.Y + LineSpacing) - threshRect.Bottom;
			}
			else if( caretPos.Y < threshRect.Top )
			{
				// scroll up
				vDelta = caretPos.Y - threshRect.Top;
			}

			// scroll the view
			Scroll( vDelta / LineSpacing );
			HScroll( hDelta );

			// update horizontal ruler graphic.
			// because indicator graphic may have been scrolled out partially (drawn partially),
			// just scrolling horizontal ruler area may make uncompeltely drawn indicator
			if( ShowsHRuler && 0 < hDelta )
			{
				UpdateHRuler( g );
			}
		}

		/// <summary>
		/// Scroll vertically.
		/// </summary>
		public void Scroll( int lineDelta )
		{
			int delta;
			Rectangle clipRect;
			int destLineIndex;
			int maxLineIndex;
			int visibleLineCount;

			if( lineDelta == 0 )
				return;

			// calculate specified index of new FirstVisibleLine and biggest acceptable value of it
			destLineIndex = FirstVisibleLine + lineDelta;
			if( ScrollsBeyondLastLine )
			{
				maxLineIndex = LineCount - 1;
			}
			else
			{
				visibleLineCount = VisibleSize.Height / LineSpacing;
				maxLineIndex = Math.Max( 0, LineCount-visibleLineCount+1 );
			}

			// calculate scroll distance
			if( destLineIndex < 0 )
			{
				delta = -FirstVisibleLine;
			}
			else if( maxLineIndex < destLineIndex )
			{
				delta = maxLineIndex - FirstVisibleLine;
			}
			else
			{
				delta = lineDelta;
			}

			// make clipping rectangle
			clipRect = new Rectangle( 0, YofTextArea, _VisibleSize.Width, _VisibleSize.Height );

			// do scroll
			FirstVisibleLine += delta;
			_UI.Scroll( clipRect, 0, -(delta * LineSpacing) );
			_UI.UpdateCaretGraphic();
		}

		/// <summary>
		/// Scroll horizontally.
		/// </summary>
		public void HScroll( int columnDelta )
		{
			int deltaInPx;
			Rectangle clipRect = new Rectangle();
			int rightLimit;
			int desiredX;

			if( columnDelta == 0 )
				return;

			// calculate the x-coord of right most scroll position
			desiredX = ScrollPosX + columnDelta;
			rightLimit = ReCalcRightEndOfTextArea( desiredX );
			if( rightLimit <= 0 )
			{
				return; // virtual text area is narrower than visible area. no need to scroll
			}

			// calculate scroll distance
			if( desiredX < 0 )
			{
				//--- scrolling to left of the text area ---
				// do nothing if already at left most position
				if( ScrollPosX == 0 )
					return;
				
				// scroll to left most position
				deltaInPx = -ScrollPosX;
			}
			else if( rightLimit <= desiredX )
			{
				//--- scrolling to right of the text area ---
				// do nothing if already at right most position
				if( rightLimit == ScrollPosX+columnDelta )
					return;

				// scroll to right most position
				deltaInPx = (rightLimit - ScrollPosX);
			}
			else
			{
				deltaInPx = columnDelta;
			}

			// make clipping rectangle
			clipRect.X = XofTextArea;
			clipRect.Y = 0;
			clipRect.Width = _VisibleSize.Width - XofTextArea;
			clipRect.Height = _VisibleSize.Height;

			// do scroll
			ScrollPosX += deltaInPx;
			_UI.Scroll( clipRect, -deltaInPx, 0 );
		}

		/// <summary>
		/// Requests to invalidate whole area.
		/// </summary>
		public void Invalidate()
		{
			_UI.Invalidate();
		}

		/// <summary>
		/// Requests to invalidate specified area.
		/// </summary>
		public void Invalidate( int x, int y, int width, int height )
		{
			Invalidate( new Rectangle(x, y, width, height) );
		}

		/// <summary>
		/// Requests to invalidate specified area.
		/// </summary>
		/// <param name="rect">rectangle area to be invalidate (in client area coordinate)</param>
		public void Invalidate( Rectangle rect )
		{
//DEBUG//using(IGraphics g = _UI.GetIGraphics() ){g.ForeColor=Color.Red; g.DrawLine(rect.Left,rect.Top,rect.Right-1,rect.Bottom-1);g.DrawLine(rect.Left,rect.Bottom-1,rect.Right-1,rect.Top);DebugUtl.Sleep(400);}
			_UI.Invalidate( rect );
		}

		/// <summary>
		/// Requests to invalidate area covered by given text range.
		/// </summary>
		/// <param name="beginIndex">Begin text index of the area to be invalidated.</param>
		/// <param name="endIndex">End text index of the area to be invalidated.</param>
		public abstract void Invalidate( int beginIndex, int endIndex );

		/// <summary>
		/// Requests to invalidate area covered by given text range.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="beginIndex">Begin text index of the area to be invalidated.</param>
		/// <param name="endIndex">End text index of the area to be invalidated.</param>
		public abstract void Invalidate( IGraphics g, int beginIndex, int endIndex );

		/// <summary>
		/// Sets font size to larger one.
		/// </summary>
		public void ZoomIn()
		{
			// remember left-end position of text area
			int oldTextAreaX = XofTextArea;

			// calculate next font size
			int newSize = (int)( FontInfo.Size / 0.9 );
			if( newSize <= FontInfo.Size )
			{
				newSize = FontInfo.Size + 1;
			}

			// apply new font size
			FontInfo = new FontInfo( FontInfo.Name, newSize, FontInfo.Style );
			_UI.FontInfo = FontInfo;

			// reset text area to sustain total width of view
			// because changing font size also changes width of line number area,
			TextAreaWidth += oldTextAreaX - XofTextArea;

			_UI.UpdateCaretGraphic();
		}

		/// <summary>
		/// Sets font size to smaller one.
		/// </summary>
		public void ZoomOut()
		{
			// remember left-end position of text area
			int oldTextAreaX = XofTextArea;

			// calculate next font size
			int newSize = (int)(FontInfo.Size * 0.9);
			if( newSize < MinimumFontSize )
			{
				newSize = MinimumFontSize;
			}

			// apply new font size
			FontInfo = new FontInfo( FontInfo.Name, newSize, FontInfo.Style );
			_UI.FontInfo = FontInfo;

			// reset text area to sustain total width of view
			// because changing font size also changes width of line number area,
			TextAreaWidth += oldTextAreaX - XofTextArea;

			_UI.UpdateCaretGraphic();
		}
		#endregion

		#region Communication between UI Module
		/// <summary>
		/// UI module must call this method
		/// to synchronize visible size between UI module and view.
		/// </summary>
		internal void HandleSizeChanged( Size newSize )
		{
			_VisibleSize = newSize;
		}

		/// <summary>
		/// Internal use only.
		/// UI module must call this method
		/// when the document object was changed to another object.
		/// </summary>
		internal virtual void HandleDocumentChanged( Document prevDocument )
		{
			using( IGraphics g = _UI.GetIGraphics() )
			{
				// reset width of line number area
				UpdateLineNumberWidth( g );

				// re-calculate line index of caret and anchor
				Document.ViewParam.PrevCaretLine
					= GetLineIndexFromCharIndex( Document.CaretIndex );
				Document.ViewParam.PrevAnchorLine
					= GetLineIndexFromCharIndex( Document.AnchorIndex );

				// reset desired column to current caret position
				SetDesiredColumn( g );
			}
		}

		/// <summary>
		/// This method will be called when the selection was changed.
		/// </summary>
		internal abstract void HandleSelectionChanged( object sender, SelectionChangedEventArgs e );

		/// <summary>
		/// This method will be called when the 'dirty' state of document was changed.
		/// </summary>
		internal virtual void HandleDirtyStateChanged( object sender, EventArgs e )
		{
			// if dirty flag has been cleared, redraw entire dirt bar
			if( Document.IsDirty == false )
			{
				Rectangle rect = new Rectangle();
				rect.X = XofDirtBar;
				rect.Y = YofTextArea;
				rect.Width = DirtBarWidth;
				rect.Height = VisibleSize.Height;
				Invalidate( rect );
			}
		}

		/// <summary>
		/// This method will be called when the content was changed.
		/// </summary>
		internal abstract void HandleContentChanged( object sender, ContentChangedEventArgs e );

		/// <summary>
		/// Updates width of the line number area.
		/// </summary>
		protected void UpdateLineNumberWidth( IGraphics g )
		{
			DebugUtl.Assert( this.Document != null );

			// if current width of line number area is appropriate, do nothing
			if( Document.LineCount <= Document.ViewParam.MaxLineNumber )
			{
				return;
			}

			// find minimum value from samples for calculating width of line number area
			for( int i=0; i<_LineNumberSamples.Length; i++ )
			{
				if( Document.LineCount <= _LineNumberSamples[i] )
				{
					Document.ViewParam.MaxLineNumber = _LineNumberSamples[i];
					if( _LastUsedLineNumberSample != _LineNumberSamples[i] )
					{
						UpdateMetrics( g );
						Invalidate();
					}
					return;
				}
			}
		}

		internal void HandleGotFocus()
		{
			// draw underline on current line
			if( HighlightsCurrentLine )
			{
				using( IGraphics g = _UI.GetIGraphics() )
				{
					int selBegin, selEnd;
					Document.GetSelection( out selBegin, out selEnd );
					if( selBegin == selEnd )
					{
						DrawUnderLine( g, YofLine(GetLineIndexFromCharIndex(selBegin)), ColorScheme.HighlightColor );
					}
				}
			}
		}

		internal void HandleLostFocus()
		{
			// erase underline on current line
			if( HighlightsCurrentLine )
			{
				using( IGraphics g = _UI.GetIGraphics() )
				{
					int selBegin, selEnd;
					Document.GetSelection( out selBegin, out selEnd );
					if( selBegin == selEnd )
					{
						DrawUnderLine( g, YofLine(GetLineIndexFromCharIndex(selBegin)), ColorScheme.BackColor );
					}
				}
			}
		}
		#endregion

		#region Coordinates of Graphical Parts
		/// <summary>
		/// Gets X coordinate in client area of line number area.
		/// </summary>
		public int XofLineNumberArea
		{
			get{ return 0; }
		}

		/// <summary>
		/// Gets X coordinate in client area of dirt bar area.
		/// </summary>
		public int XofDirtBar
		{
			get{ return XofLineNumberArea + LineNumAreaWidth; }
		}

		/// <summary>
		/// Gets X coordinate in client area of left margin.
		/// </summary>
		public int XofLeftMargin
		{
			get
			{
				int value = XofDirtBar + DirtBarWidth;
				if( 0 < value )
					return value + 1;
				else
					return value;
			}
		}

		/// <summary>
		/// Gets X coordinate in client area of text area.
		/// </summary>
		public int XofTextArea
		{
			get
			{
				int value = XofLeftMargin + LeftMargin;
				if( LeftMargin <= 0 )
					return value + 1;
				else
					return value;
			}
		}

		/// <summary>
		/// Gets Y coordinate in client area of horizontal ruler.
		/// </summary>
		public int YofHRuler
		{
			get{ return 0; }
		}

		/// <summary>
		/// Gets Y coordinate in client area of top margin.
		/// </summary>
		public int YofTopMargin
		{
			get{ return YofHRuler + HRulerHeight; }
		}

		/// <summary>
		/// Gets Y coordinate in client area of text area.
		/// </summary>
		public int YofTextArea
		{
			get{ return YofTopMargin + TopMargin; }
		}

		/// <summary>
		/// Calculates size and location of the dirt bar area.
		/// </summary>
		public Rectangle DirtBarRectangle
		{
			get
			{
				return new Rectangle(
						XofDirtBar, YofTextArea,
						DirtBarWidth, VisibleSize.Height - YofTextArea
					);
			}
		}

		/// <summary>
		/// Gets location and size of the line number area.
		/// </summary>
		public Rectangle LineNumberAreaRectangle
		{
			get
			{
				return new Rectangle(
						XofLineNumberArea, YofTextArea,
						LineNumAreaWidth, VisibleSize.Height - YofTextArea
					);
			}
		}

		/// <summary>
		/// Gets location and size of the horizontal ruler area.
		/// </summary>
		public Rectangle HRulerRectangle
		{
			get
			{
				return new Rectangle(
						0, YofHRuler, VisibleSize.Width, YofTopMargin
					);
			}
		}

		/// <summary>
		/// Gets location and size of the visible text area in screen.
		/// </summary>
		public Rectangle TextAreaRectangle
		{
			get
			{
				return new Rectangle(
						XofTextArea, YofTextArea,
						VisibleSize.Width - XofTextArea,
						VisibleSize.Height - YofTextArea
					);
			}
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets Y coordinate in client area of specified line.
		/// </summary>
		internal int YofLine( int lineIndex )
		{
			return (  (lineIndex - FirstVisibleLine) * LineSpacing  ) + YofTextArea;
		}

		internal int EolCodeWidthInPx
		{
			get{ return (_LineHeight >> 1) + (_LineHeight >> 2); }
		}
		#endregion
	}
}
