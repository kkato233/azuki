// file: PropView.cs
// brief: Platform independent view (proportional).
//=========================================================
//DEBUG//#define DRAW_SLOWLY
using System;
using System.Drawing;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Platform independent view implementation to display text with proportional font.
	/// </summary>
	class PropView : View
	{
		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="ui">Implementation of the platform dependent UI module.</param>
		internal PropView( IUserInterface ui )
			: base( ui )
		{
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		internal PropView( View other )
			: base( other )
		{
			// release selection
			// (because changing view while keeping selection makes
			// pretty difficult problem around invalidation,
			// force to release selection here)
			if( Document != null )
			{
				Document.SetSelection( Document.CaretIndex, Document.CaretIndex );

				// scroll to caret manually.
				// (because text graphic was not drawn yet,
				// maximum line length is unknown
				// so ScrollToCaret does not work properly)
				using( IGraphics g = _UI.GetIGraphics() )
				{
					Point pos = GetVirPosFromIndex( g, Document.CaretIndex );
					int newValue = pos.X - (VisibleTextAreaSize.Width / 2);
					if( 0 < newValue )
					{
						ScrollPosX = newValue;
						_UI.UpdateScrollBarRange();
					}
				}
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets number of the screen lines.
		/// </summary>
		public override int LineCount
		{
			get{ return base.Document.LineCount; }
		}

		/// <summary>
		/// Re-calculates and updates x-coordinate of the right end of the virtual text area.
		/// </summary>
		/// <param name="desiredX">X-coordinate of scroll destination desired.</param>
		/// <returns>The largest X-coordinate which Azuki can scroll to.</returns>
		protected override int ReCalcRightEndOfTextArea( int desiredX )
		{
			if( TextAreaWidth < desiredX )
			{
				TextAreaWidth = desiredX + (VisibleTextAreaSize.Width >> 3);
				_UI.UpdateScrollBarRange();
			}
			return TextAreaWidth;
		}
		#endregion

		#region Position / Index Conversion
		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		public override Point GetVirPosFromIndex( IGraphics g, int index )
		{
			int line, column;
			Document.GetLineColumnIndexFromCharIndex( index, out line, out column );
			return GetVirPosFromIndex( g, line, column );
		}

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		public override Point GetVirPosFromIndex( IGraphics g, int lineIndex, int columnIndex )
		{
			if( lineIndex < 0 || LineCount <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Specified index is out of range. (value:"+lineIndex+", line count:"+LineCount+")" );
			if( columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Specified index is out of range. (value:"+columnIndex+")" );

			Point pos = new Point();

			// set value for when the columnIndex is 0
			pos.X = 0;
			pos.Y = (lineIndex * LineSpacing) + (LinePadding >> 1);

			// if the location is not the head of the line, calculate x-coord.
			if( 0 < columnIndex )
			{
				int begin = GetCharIndexFromLineColumnIndex( lineIndex, 0 );
				int end = GetCharIndexFromLineColumnIndex( lineIndex, columnIndex );
				pos.X = MeasureTokenEndX( g, new TextSegment(begin, end), pos.X );
			}

			return pos;
		}

		/// <summary>
		/// Gets char-index of the char at the point specified by location in the virtual space.
		/// </summary>
		/// <returns>The index of the character at specified location.</returns>
		public override int GetIndexFromVirPos( IGraphics g, Point pt )
		{
			int lineIndex, columnIndex;
			int drawableTextLen;

			// calc line index
			lineIndex = (pt.Y / LineSpacing);
			if( lineIndex < 0 )
			{
				lineIndex = 0;
			}
			else if( Document.LineCount <= lineIndex
				&& Document.LineCount != 0 )
			{
				// the point indicates beyond the final line.
				// treat as if the final line was specified
				lineIndex = Document.LineCount - 1;
			}

			// calc column index
			columnIndex = 0;
			if( 0 < pt.X )
			{
				// get content of the line
				string line = Document.GetLineContent( lineIndex );

				// calc maximum length of chars in line
				int rightLimitX = pt.X;
				int leftPartWidth = MeasureTokenEndX( g, line, 0, rightLimitX, out drawableTextLen );
				Debug.Assert( Document.IsNotDividableIndex(line, drawableTextLen) == false );
				columnIndex = drawableTextLen;

				// if the location is nearer to the NEXT of that char,
				// we should return the index of next one.
				if( drawableTextLen < line.Length )
				{
					// get next grapheme cluster
					var nextChar = new TextSegment( drawableTextLen, drawableTextLen+1 );
					while( Document.IsNotDividableIndex(line, nextChar.End) )
						nextChar.End++;

					// determine which side the location is near
					int nextCharWidth = MeasureTokenEndX( g,
														  nextChar,
														  leftPartWidth ) - leftPartWidth;
					if( leftPartWidth + nextCharWidth/2 < pt.X ) // == "x of middle of next char" < "x of click in virtual text area"
					{
						columnIndex = drawableTextLen + 1;
						while( Document.IsNotDividableIndex(line, columnIndex) )
						{
							columnIndex++;
						}
					}
				}
			}

			return Document.GetCharIndexFromLineColumnIndex( lineIndex, columnIndex );
		}

		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override int GetLineHeadIndex( int lineIndex )
		{
			return Document.GetLineHeadIndex( lineIndex );
		}

		/// <summary>
		/// Gets the index of the first char in the screen line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			return Document.GetLineHeadIndexFromCharIndex( charIndex );
		}

		/// <summary>
		/// Calculates screen line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex )
		{
			Document.GetLineColumnIndexFromCharIndex( charIndex, out lineIndex, out columnIndex );
		}

		/// <summary>
		/// Calculates char-index from screen line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public override int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex )
		{
			return Document.GetCharIndexFromLineColumnIndex( lineIndex, columnIndex );
		}
		#endregion

		#region Appearance Invalidating and Updating
		internal override void HandleSelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			Debug.Assert( TextUtil.IsDividableIndex(Document.InternalBuffer, e.OldAnchor) );
			Debug.Assert( TextUtil.IsDividableIndex(Document.InternalBuffer, e.OldCaret) );
			Document doc = Document;
			int anchor = doc.AnchorIndex;
			int caret = doc.CaretIndex;
			int anchorLine, anchorColumn;
			int caretLine, caretColumn;
			int prevCaretLine = doc.ViewParam.PrevCaretLine;
			IGraphics g = null;

			// calculate line/column index of current anchor/caret
			GetLineColumnIndexFromCharIndex( anchor, out anchorLine, out anchorColumn );
			GetLineColumnIndexFromCharIndex( caret, out caretLine, out caretColumn );

			try
			{
				if( e.ByContentChanged )
					return;

				g = _UI.GetIGraphics();

				// update indicator graphic on horizontal ruler
				UpdateHRuler( g );

				// if the anchor moved, firstly invalidate old selection area
				// because invalidation logic bellow does not expect the anchor's move.
				if( e.OldAnchor != anchor )
				{
					if( e.OldAnchor < e.OldCaret )
						Invalidate( e.OldAnchor, e.OldCaret );
					else
						Invalidate( e.OldCaret, e.OldAnchor );
				}

				// if in rectangle selection mode, execute special logic
				if( e.OldRectSelectRanges != null )
				{
					HandleSelectionChanged_OnRectSelect( g, e );
					return;
				}

				// if there was no selection and is no selection too,
				// update current line highlight if enabled.
				if( e.OldAnchor == e.OldCaret && anchor == caret )
				{
					if( HighlightsCurrentLine
						&& prevCaretLine != caretLine )
					{
						HandleSelectionChanged_UpdateCurrentLineHighlight( g, prevCaretLine, caretLine );
					}
				}
				// or, does the change release selection?
				else if( e.OldAnchor != e.OldCaret && anchor == caret )
				{
					HandleSelectionChanged_OnReleaseSel( g, e );
				}
				// then, the change expands selection.
				else
				{
					// if this is the beginning of selection, remove current line highlight (underline)
					if( HighlightsCurrentLine && e.OldCaret == e.OldAnchor )
					{
						int oldCaretLine, oldCaretColumn, oldCaretLineY;

						GetLineColumnIndexFromCharIndex( e.OldCaret, out oldCaretLine, out oldCaretColumn );
						oldCaretLineY = YofLine( oldCaretLine );
						DrawUnderLine( g, oldCaretLineY, ColorScheme.BackColor );
					}

					// if the change occured in a line?
					if( prevCaretLine == caretLine )
					{
						// in a line.
						if( e.OldCaret < caret )
							HandleSelectionChanged_OnExpandSelInLine( g, e, e.OldCaret, caret, prevCaretLine );
						else
							HandleSelectionChanged_OnExpandSelInLine( g, e, caret, e.OldCaret, caretLine );
					}
					else
					{
						// not in a line; in multiple lines.
						HandleSelectionChanged_OnExpandSel( g, e, caretLine, caretColumn );
					}
				}
			}
			catch( Exception ex )
			{
				// if an exception was caught here, it is not a fatal error
				// so avoid crashing application
				Invalidate();
				Debug.Fail( ex.ToString() );
			}
			finally
			{
				// remember last selection for next invalidation
				doc.ViewParam.PrevCaretLine = caretLine;
				doc.ViewParam.PrevAnchorLine = anchorLine;

				// dispose graphics resource
				if( g != null )
				{
					g.Dispose();
				}
			}
		}

		void HandleSelectionChanged_UpdateCurrentLineHighlight( IGraphics g, int oldCaretLine, int newCaretLine )
		{
			int prevAnchorLine = Document.ViewParam.PrevAnchorLine;
			int prevCaretLine = Document.ViewParam.PrevCaretLine;

			// invalidate old underline if it is still visible
			if( prevCaretLine == prevAnchorLine && FirstVisibleLine <= prevCaretLine )
			{
				int y = YofLine( prevCaretLine );
				DrawUnderLine( g, y, ColorScheme.BackColor );
			}
			
			// draw new underline if it is visible
			if( FirstVisibleLine <= newCaretLine )
			{
				int newCaretY = YofLine( newCaretLine );
				DrawUnderLine( g, newCaretY, ColorScheme.HighlightColor );
			}
		}

		void HandleSelectionChanged_OnRectSelect( IGraphics g, SelectionChangedEventArgs e )
		{
			int firstBegin, lastEnd;
			Point firstBeginPos, lastEndPos;
			Rectangle invalidRect;

			//--- make rectangle that covers ---
			// 1) all lines covered by the selection rectangle and
			// 2) extra lines for both upper and lower direction
			
			// calculate rectangle in virtual space
			firstBegin = e.OldRectSelectRanges[0];
			lastEnd = e.OldRectSelectRanges[ e.OldRectSelectRanges.Length - 1 ];
			Debug.Assert( 0 <= firstBegin && firstBegin <= Document.Length );
			Debug.Assert( 0 <= lastEnd && lastEnd <= Document.Length );
			firstBeginPos = this.GetVirPosFromIndex( g, firstBegin );
			lastEndPos = this.GetVirPosFromIndex( g, lastEnd );
			firstBeginPos.Y -= (LinePadding >> 1);
			lastEndPos.Y -= (LinePadding >> 1);

			// convert it to screen screen coordinate
			VirtualToScreen( ref firstBeginPos );
			VirtualToScreen( ref lastEndPos );

			// then, invalidate that rectangle
			invalidRect = new Rectangle(
					0,
					firstBeginPos.Y - LineSpacing,
					VisibleSize.Width,
					(lastEndPos.Y - firstBeginPos.Y) + (LineSpacing * 3) // 3 ... a line above, the line, and a line below
				);
			Invalidate( invalidRect );
		}

		void HandleSelectionChanged_OnExpandSelInLine( IGraphics g, SelectionChangedEventArgs e,
													   int begin, int end, int beginL )
		{
			DebugUtl.Assert( beginL < LineCount );
			Debug.Assert( TextUtil.IsDividableIndex(Document.InternalBuffer, begin) );
			Debug.Assert( TextUtil.IsDividableIndex(Document.InternalBuffer, end) );
			var doc = Document;
			var rect = new Rectangle();

			// if anchor was moved, invalidate largest range made with four indexes
			if( e.OldAnchor != doc.AnchorIndex )
			{
				begin = Utl.Min( e.OldAnchor, e.OldCaret, doc.AnchorIndex, doc.CaretIndex );
				end = Utl.Max( e.OldAnchor, e.OldCaret, doc.AnchorIndex, doc.CaretIndex );
				Invalidate( begin, end );

				return;
			}

			// calculate location of invalid rectangle
			var beginLineHead = GetLineHeadIndex( beginL );
			if( beginLineHead < begin )
				rect.X = MeasureTokenEndX( g, new TextSegment(beginLineHead, begin), 0 );
			rect.Y = YofLine( beginL );

			// calculate width of invalid rectangle
			rect.Width = MeasureTokenEndX( g, new TextSegment(beginLineHead, end), 0 ) - rect.X;
			rect.Height = LineSpacing;

			// invalidate
			rect.X -= (ScrollPosX - XofTextArea);
			Invalidate( rect );
		}

		void HandleSelectionChanged_OnExpandSel( IGraphics g, SelectionChangedEventArgs e, int caretLine, int caretColumn )
		{
			Document doc = this.Document;
			int begin, beginL;
			int end, endL;
			int beginLineHead, endLineHead;

			// if anchor was moved, invalidate largest range made with four indexes
			if( e.OldAnchor != doc.AnchorIndex )
			{
				begin = Utl.Min( e.OldAnchor, e.OldCaret, doc.AnchorIndex, doc.CaretIndex );
				end = Utl.Max( e.OldAnchor, e.OldCaret, doc.AnchorIndex, doc.CaretIndex );
				Invalidate( begin, end );

				return;
			}

			// get range between old caret and current caret
			if( e.OldCaret < doc.CaretIndex )
			{
				begin = e.OldCaret;
				beginL = doc.ViewParam.PrevCaretLine;
				end = doc.CaretIndex;
				endL = caretLine;
			}
			else
			{
				begin = doc.CaretIndex;
				beginL = caretLine;
				end = e.OldCaret;
				endL = doc.ViewParam.PrevCaretLine;
			}
			beginLineHead = GetLineHeadIndex( beginL );
			endLineHead = GetLineHeadIndex( endL ); // if old caret is the end pos and if the pos exceeds current text length, this will fail.

			// invalidate
			Invalidate_MultiLines( g, begin, end, beginL, endL, beginLineHead, endLineHead );
		}

		void HandleSelectionChanged_OnReleaseSel( IGraphics g, SelectionChangedEventArgs e )
		{
			// in this case, we must invalidate between
			// old anchor pos and old caret pos.
			Document doc = base.Document;
			int beginLineHead, endLineHead;
			int begin, beginL;
			int end, endL;
			int prevAnchorLine = doc.ViewParam.PrevAnchorLine;
			int prevCaretLine = doc.ViewParam.PrevCaretLine;

			// get old selection range
			if( e.OldAnchor < e.OldCaret )
			{
				begin = e.OldAnchor;
				beginL = prevAnchorLine;
				end = e.OldCaret;
				endL = prevCaretLine;
			}
			else
			{
				begin = e.OldCaret;
				beginL = prevCaretLine;
				end = e.OldAnchor;
				endL = prevAnchorLine;
			}
			beginLineHead = GetLineHeadIndexFromCharIndex( begin );
			endLineHead = GetLineHeadIndexFromCharIndex( end );

			if( prevCaretLine == prevAnchorLine )
			{
				Rectangle rect = new Rectangle();
				int left = MeasureTokenEndX( g, new TextSegment(beginLineHead, begin), 0 )
						   - (ScrollPosX - XofTextArea);
				int right = MeasureTokenEndX( g, new TextSegment(endLineHead, end), 0 )
							- (ScrollPosX - XofTextArea);
				rect.X = left;
				rect.Y = YofLine( beginL );
				rect.Width = right - left;
				rect.Height = LineSpacing;

				Invalidate( rect );
			}
			else
			{
				Invalidate_MultiLines( g, begin, end, beginL, endL, beginLineHead, endLineHead );
			}
		}

		internal override void HandleContentChanged( object sender, ContentChangedEventArgs e )
		{
			// [*1] if replacement breaks or creates
			// a combining character sequence at left boundary of the range,
			// at least one grapheme cluster left must be redrawn.
			// 
			// One case of that e.OldText has combining char at first:
			//    aa^aa --(replace [2, 4) to "AA")--> aaAAa
			// 
			// One case of that e.NewText has combining char at first:
			//    aaaa --(replace [2, 3) to "^A")--> aa^Aa

			Point invalidStartPos;
			int invalidStartIndex;
			Rectangle invalidRect1 = new Rectangle();
			Rectangle invalidRect2 = new Rectangle();

			using( IGraphics g = _UI.GetIGraphics() )
			{
				// calculate where to start invalidation
				invalidStartIndex = e.Index;
				if( Document.IsCombiningCharacter(e.OldText, 0)
					|| Document.IsCombiningCharacter(e.NewText, 0) )
				{
					// [*1]
					invalidStartIndex = GetLineHeadIndexFromCharIndex(
							invalidStartIndex
						);
				}

				// get graphical position of the place
				invalidStartPos = GetVirPosFromIndex( g, invalidStartIndex );
				VirtualToScreen( ref invalidStartPos );

				// update indicator graphic on horizontal ruler
				UpdateHRuler( g );

				// invalidate the part at right of the old selection
				invalidRect1.X = invalidStartPos.X;
				invalidRect1.Width = VisibleSize.Width - invalidRect1.X;
				invalidRect1.Y = invalidStartPos.Y - (LinePadding >> 1);
				invalidRect1.Height = LineSpacing;

				// invalidate all lines below caret
				// if old text or new text contains multiple lines
				if( TextUtil.IsMultiLine(e.OldText) || TextUtil.IsMultiLine(e.NewText) )
				{
					//NO_NEED//invalidRect2.X = 0;
					invalidRect2.Y = invalidRect1.Bottom;
					invalidRect2.Width = VisibleSize.Width;
					invalidRect2.Height = VisibleSize.Height - invalidRect2.Top;
				}

				// invalidate the range
				Invalidate( invalidRect1 );
				if( 0 < invalidRect2.Height )
				{
					Invalidate( invalidRect2 );
				}

				// update left side of text area
				DrawDirtBar( g, invalidRect1.Top, Document.GetLineIndexFromCharIndex(e.Index) );
				UpdateLineNumberWidth( g );

				//DO_NOT//base.HandleContentChanged( sender, e );
			}
		}

		/// <summary>
		/// Requests to invalidate area covered by given text range.
		/// </summary>
		/// <param name="beginIndex">Begin text index of the area to be invalidated.</param>
		/// <param name="endIndex">End text index of the area to be invalidated.</param>
		public override void Invalidate( int beginIndex, int endIndex )
		{
			using( IGraphics g = _UI.GetIGraphics() )
			{
				Invalidate( g, beginIndex, endIndex );
			}
		}

		/// <summary>
		/// Requests to invalidate area covered by given text range.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="beginIndex">Begin text index of the area to be invalidated.</param>
		/// <param name="endIndex">End text index of the area to be invalidated.</param>
		public override void Invalidate( IGraphics g, int beginIndex, int endIndex )
		{
			DebugUtl.Assert( 0 <= beginIndex, "cond: 0 <= beginIndex("+beginIndex+")" );
			DebugUtl.Assert( beginIndex <= endIndex, "cond: beginIndex("+beginIndex+") <= endIndex("+endIndex+")" );
			DebugUtl.Assert( endIndex <= Document.Length, "endIndex("+endIndex+") must not exceed document length ("+Document.Length+")" );
			if( beginIndex == endIndex )
				return;
			
			int beginLineHead, endLineHead;
			int beginL, endL, dummy;

			while( TextUtil.IsUndividableIndex(Document.InternalBuffer, beginIndex) )
				beginIndex++;
			while( TextUtil.IsUndividableIndex(Document.InternalBuffer, endIndex) )
				endIndex++;

			// get needed coordinates
			GetLineColumnIndexFromCharIndex( beginIndex, out beginL, out dummy );
			GetLineColumnIndexFromCharIndex( endIndex, out endL, out dummy );
			beginLineHead = GetLineHeadIndex( beginL );

			// switch invalidation logic by whether the invalidated area is multiline or not
			if( beginL != endL )
			{
				endLineHead = GetLineHeadIndex( endL ); // this is needed for invalidating multiline selection
				Invalidate_MultiLines( g, beginIndex, endIndex, beginL, endL, beginLineHead, endLineHead );
			}
			else
			{
				Invalidate_InLine( g, beginIndex, endIndex, beginL, beginLineHead );
			}
		}
		
		void Invalidate_InLine( IGraphics g, int begin, int end, int beginL, int beginLineHead )
		{
			DebugUtl.Assert( g != null, "null was given to PropView.Invalidate_InfLine." );
			DebugUtl.Assert( 0 <= begin, "cond: 0 <= begin("+begin+")" );
			DebugUtl.Assert( begin <= end, "cond: begin("+begin+") <= end("+end+")" );
			DebugUtl.Assert( end <= Document.Length, "cond: end("+end+") <= Document.Length("+Document.Length+")" );
			DebugUtl.Assert( 0 <= beginL, "cond: 0 <= beginL("+beginL+")" );
			DebugUtl.Assert( beginL <= this.LineCount, "cond: beginL("+beginL+") <= IView.LineCount("+this.LineCount+")" );
			DebugUtl.Assert( beginLineHead <= begin, "cond: beginLineHead("+beginLineHead+") <= begin("+begin+")" );
			Debug.Assert( TextUtil.IsDividableIndex(Document.InternalBuffer, begin) );
			Debug.Assert( TextUtil.IsDividableIndex(Document.InternalBuffer, end) );
			Debug.Assert( TextUtil.IsDividableIndex(Document.InternalBuffer, beginLineHead) );
			if( begin == end )
				return;

			Rectangle rect = new Rectangle();

			// calculate position of the invalid rect
			rect.X = MeasureTokenEndX( g, new TextSegment(beginLineHead, begin), 0 );
			rect.Y = YofLine( beginL );

			// calculate width and height of the invalid rect
			rect.Width = MeasureTokenEndX( g, new TextSegment(begin, end), rect.X ) - rect.X;
			rect.Height = LineSpacing;
			Debug.Assert( 0 <= rect.Width );

			// invalidate
			rect.X -= (ScrollPosX - XofTextArea);
			Invalidate( rect );
		}

		void Invalidate_MultiLines( IGraphics g, int begin, int end, int beginLine, int endLine, int beginLineHead, int endLineHead )
		{
			DebugUtl.Assert( g != null, "null was given to PropView.Invalidate_MultiLines." );
			DebugUtl.Assert( 0 <= begin, "cond: 0 <= begin("+begin+")" );
			DebugUtl.Assert( begin <= end, "cond: begin("+begin+") <= end("+end+")" );
			DebugUtl.Assert( end <= Document.Length, "cond: end("+end+") <= Document.Length("+Document.Length+")" );
			DebugUtl.Assert( 0 <= beginLine, "cond: 0 <= beginLine("+beginLine+")" );
			DebugUtl.Assert( beginLine < endLine, "cond: beginLine("+beginLine+") < endLine("+endLine+")" );
			DebugUtl.Assert( endLine <= this.LineCount, "cond: endLine("+endLine+") <= IView.LineCount("+this.LineCount+")" );
			DebugUtl.Assert( beginLineHead <= begin, "cond: beginLineHead("+beginLineHead+") <= begin("+begin+")" );
			DebugUtl.Assert( beginLineHead < endLineHead, "cond: beginLineHead("+beginLineHead+" < endLineHead("+endLineHead+")" );
			DebugUtl.Assert( endLineHead <= end, "cond: endLineHead("+endLineHead+") <= end("+end+")" );
			if( begin == end )
				return;

			Rectangle upper, lower, middle;
			var doc = Document;

			// calculate upper part of the invalid area
			upper = new Rectangle();
			if( FirstVisibleLine <= beginLine ) // if not visible, no need to invalidate
			{
				upper.X = MeasureTokenEndX( g, new TextSegment(beginLineHead, begin), 0 )
						  - (ScrollPosX - XofTextArea);
				upper.Y = YofLine( beginLine );
				upper.Width = VisibleSize.Width - upper.X;
				upper.Height = LineSpacing;
			}

			// calculate lower part of the invalid area
			lower = new Rectangle();
			if( FirstVisibleLine <= endLine ) // if not visible, no need to invalidate
			{
				lower.X = XofTextArea;
				lower.Y = YofLine( endLine );
				lower.Width = MeasureTokenEndX( g, new TextSegment(endLineHead, end), 0 ) - ScrollPosX;
				lower.Height = LineSpacing;
			}

			// calculate middle part of the invalid area
			middle = new Rectangle();
			if( FirstVisibleLine < beginLine+1 )
			{
				middle.Y = YofLine( beginLine + 1 );
			}
			middle.X = XofTextArea;
			middle.Width = VisibleSize.Width;
			middle.Height = lower.Y - middle.Y;

			// invalidate three rectangles
			if( 0 < upper.Height )
				Invalidate( upper );
			if( 0 < middle.Height )
				Invalidate( middle );
			if( 0 < lower.Height )
				Invalidate( lower );
		}
		#endregion

		#region Painting
		/// <summary>
		/// Paints content to a graphic device.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="clipRect">clipping rectangle that covers all invalidated region (in client area coordinate)</param>
		public override void Paint( IGraphics g, Rectangle clipRect )
		{
			// [*1] if the graphic of a line should be redrawn by owner draw,
			// Azuki does not redraw the line but invalidate
			// the area of the line and let it be drawn on next drawing chance
			// so that the graphic will not flicker.
			DebugUtl.Assert( g != null, "invalid argument; IGraphics is null" );
			DebugUtl.Assert( FontInfo != null, "invalid state; FontInfo is null" );
			DebugUtl.Assert( Document != null, "invalid state; Document is null" );

			int selBegin, selEnd;
			Point pos = new Point();
			int longestLineLength = 0;
			bool shouldRedraw1, shouldRedraw2;

			// prepare off-screen buffer
#			if !DRAW_SLOWLY
			g.BeginPaint( clipRect );
#			endif

#			if DRAW_SLOWLY
			g.ForeColor = Color.Blue;
			g.DrawRectangle( clipRect.X, clipRect.Y, clipRect.Width-1, clipRect.Height-1 );
			g.DrawLine( clipRect.X, clipRect.Y, clipRect.X+clipRect.Width-1, clipRect.Y+clipRect.Height-1 );
			g.DrawLine( clipRect.X+clipRect.Width-1, clipRect.Y, clipRect.X, clipRect.Y+clipRect.Height-1 );
			DebugUtl.Sleep(400);
#			endif

			// draw horizontal ruler
			DrawHRuler( g, clipRect );

			// draw top margin
			DrawTopMargin( g );

			// draw all lines
			g.SetClipRect( clipRect );
			pos.X = -(ScrollPosX - XofTextArea);
			pos.Y = YofTextArea;
			for( int i=FirstVisibleLine; i<LineCount; i++ )
			{
				if( pos.Y < clipRect.Bottom && clipRect.Top <= pos.Y+LineSpacing )
				{
					// invoke pre-draw event
					shouldRedraw1 = _UI.InvokeLineDrawing( g, i, pos );

					// draw the line
					DrawLine( g, i, pos, clipRect, ref longestLineLength );

					// invoke post-draw event
					shouldRedraw2 = _UI.InvokeLineDrawn( g, i, pos );

					// [*1] invalidate the line graphic if needed
					if( (shouldRedraw1 || shouldRedraw2)
						&& 0 < clipRect.Left ) // prevent infinite loop
					{
						Invalidate( 0, clipRect.Y, VisibleSize.Width, clipRect.Height );
					}
				}
				pos.Y += LineSpacing;
			}
			g.RemoveClipRect();

			// expand text area width for longest line
			ReCalcRightEndOfTextArea( longestLineLength );

			// fill area below of the text
			g.BackColor = ColorScheme.BackColor;
			g.FillRectangle( XofTextArea, pos.Y, VisibleSize.Width-XofTextArea, VisibleSize.Height-pos.Y );
			for( int y=pos.Y; y<VisibleSize.Height; y+=LineSpacing )
			{
				DrawLeftOfLine( g, y, -1, false );
			}

			// flush drawing results BEFORE updating current line highlight
			// because the highlight graphic can be drawn outside of the clipping rect
#			if !DRAW_SLOWLY
			g.EndPaint();
#			endif

			// draw underline to highlight current line if there is no selection
			Document.GetSelection( out selBegin, out selEnd );
			if( HighlightsCurrentLine && selBegin == selEnd )
			{
				int caretLine, caretPosY;

				// draw underline only when the current line is visible
				caretLine = Document.GetLineIndexFromCharIndex( Document.CaretIndex );
				if( FirstVisibleLine <= caretLine )
				{
					// calculate position of the underline
					int lineDiff = caretLine - FirstVisibleLine;
					caretPosY = (lineDiff * LineSpacing) + YofTextArea;
					
					// draw underline
					DrawUnderLine( g, caretPosY, ColorScheme.HighlightColor );
				}
			}
		}

		void DrawLine( IGraphics g, int lineIndex, Point pos, Rectangle clipRect, ref int longestLineLength )
		{
			// note that given pos is NOT virtual position BUT screen position.
			string token;
			int lineHead, lineEnd;
			int begin, end; // range of the token in the text
			CharClass klass;
			Point tokenEndPos = pos;
			bool inSelection;

			// calc position of head/end of this line
			lineHead = Document.GetLineHeadIndex( lineIndex );
			if( lineIndex+1 < Document.LineCount )
				lineEnd = Document.GetLineHeadIndex( lineIndex + 1 );
			else
				lineEnd = Document.Length;

			// draw line text
			begin = lineHead;
			end = NextPaintToken( Document, begin, lineEnd, out klass, out inSelection );
			while( end <= lineEnd // until end-pos reaches line-end
				&& pos.X < clipRect.Right // or reaches right-end of the clip rect
				&& end != -1 ) // or reaches the end of text
			{
				Debug.Assert( TextUtil.IsDividableIndex(Document.InternalBuffer, begin) );
				Debug.Assert( TextUtil.IsDividableIndex(Document.InternalBuffer, end) );

				// get this token
				token = Document.GetTextInRangeRef( ref begin, ref end );
				DebugUtl.Assert( 0 < token.Length );

				// calc next drawing pos before drawing text
				{
					int virLeft = pos.X - (XofTextArea - ScrollPosX);
					tokenEndPos.X = MeasureTokenEndX( g, new TextSegment(begin, end), virLeft );
					tokenEndPos.X += (XofTextArea - ScrollPosX);
				}

				// if this token is out of the clip-rect, skip drawing.
				if( tokenEndPos.X < clipRect.Left || clipRect.Right < pos.X )
				{
					goto next_token;
				}

				// if the token area crosses the LEFT boundary of the clip-rect, cut off extra
				if( pos.X < clipRect.Left )
				{
					int invisibleCharCount, invisibleWidth;
					int rightLimit = clipRect.Left - pos.X;

					// calculate how many chars will not be in the clip-rect
					invisibleWidth = MeasureTokenEndX( g, token, 0, rightLimit, out invisibleCharCount );
					if( 0 < invisibleCharCount && invisibleCharCount < token.Length )
					{
						// cut extra (invisible) part of the token
						token = token.Substring( invisibleCharCount );
						begin += invisibleCharCount;
						pos.X += invisibleWidth;
					}
				}

				// if the token area crosses the RIGHT boundary, cut off extra
				if( clipRect.Right < tokenEndPos.X )
				{
					int visibleCharCount;

					// calculate how many chars can be drawn in the clip-rect
					MeasureTokenEndX( g, token, pos.X, clipRect.Right, out visibleCharCount );

					// set the position to cut extra trailings of this token
					if( visibleCharCount+1 <= token.Length )
					{
						if( TextUtil.IsUndividableIndex(token, visibleCharCount+1) )
						{
							token = token.Substring( 0, visibleCharCount + 2 );
						}
						else
						{
							token = token.Substring( 0, visibleCharCount + 1 );
						}
					}
					else
					{
						token = token.Substring( 0, visibleCharCount );
					}
					end = begin + token.Length;
				}

				// draw this token
				DrawToken( g, Document, begin, token, klass, ref pos, ref tokenEndPos, ref clipRect, inSelection );

			next_token:
				// get next token
				pos = tokenEndPos;
				begin = end;
				end = NextPaintToken( Document, begin, lineEnd, out klass, out inSelection );
			}

			// draw EOF mark
			if( DrawsEofMark && lineEnd == Document.Length )
			{
				DebugUtl.Assert( lineHead <= lineEnd );
				if( lineHead == lineEnd
					|| (0 < lineEnd && TextUtil.IsEolChar(Document[lineEnd-1]) == false) )
				{
					DrawEofMark( g, ref pos );
				}
			}

			// fill right of the line text
			if( pos.X < clipRect.Right )
			{
				// to prevent drawing line number area,
				// make drawing pos to text area's left if the line end does not exceed left of text area
				if( pos.X < XofTextArea )
					pos.X = XofTextArea;
				g.BackColor = ColorScheme.BackColor;
				g.FillRectangle( pos.X, pos.Y, clipRect.Right-pos.X, LineSpacing );
			}

			// if this line is wider than the width of virtual space,
			// calculate full width of this line and make it the width of virtual space.
			Point virPos = pos;
			ScreenToVirtual( ref virPos );
			if( TextAreaWidth < virPos.X + (VisibleSize.Width >> 3) )
			{
				// calculate full length of this line, in pixel
				var lineWidth = MeasureTokenEndX( g, new TextSegment(lineHead, lineEnd), 0 );

				// remember length of this line if it is the longest ever
				if( longestLineLength < lineWidth )
					longestLineLength = lineWidth;
			}

			// draw graphics at left of text
			DrawLeftOfLine( g, pos.Y, lineIndex+1, true );
		}
		#endregion
	}
}
