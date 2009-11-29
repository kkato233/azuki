// file: PropView.cs
// brief: Platform independent view (proportional).
// author: YAMAMOTO Suguru
// update: 2009-11-29
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
				Point pos = GetVirPosFromIndex( Document.CaretIndex );
				int newValue = pos.X - (VisibleTextAreaSize.Width / 2);
				if( 0 < newValue )
				{
					ScrollPosX = newValue;
					_UI.UpdateScrollBarRange();
				}
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets number of the physical lines.
		/// </summary>
		public override int LineCount
		{
			get{ return base.Document.LineCount; }
		}
		#endregion

		#region Position / Index Conversion
		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		public override Point GetVirPosFromIndex( int index )
		{
			int line, column;
			Document.GetLineColumnIndexFromCharIndex( index, out line, out column );
			return GetVirPosFromIndex( line, column );
		}

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		public override Point GetVirPosFromIndex( int lineIndex, int columnIndex )
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
				// get partial content of the line which exists before the caret
				string leftPart = Document.GetTextInRange( lineIndex, 0, lineIndex, columnIndex );

				// measure the characters
				pos.X = MeasureTokenEndX( leftPart, pos.X );
			}

			return pos;
		}

		/// <summary>
		/// Gets char-index of the char at the point specified by location in the virtual space.
		/// </summary>
		/// <returns>The index of the character at specified location.</returns>
		public override int GetIndexFromVirPos( Point pt )
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
				int leftPartWidth = MeasureTokenEndX( line, 0, rightLimitX, out drawableTextLen );
				columnIndex = drawableTextLen;

				// if the location is nearer to the NEXT of that char,
				// we should return the index of next one.
				if( drawableTextLen < line.Length )
				{
					string nextChar = line[drawableTextLen].ToString();
					int nextCharWidth = MeasureTokenEndX( nextChar, leftPartWidth ) - leftPartWidth;
					if( leftPartWidth + nextCharWidth/2 < pt.X ) // == "x of middle of next char" < "x of click in virtual text area"
					{
						columnIndex = drawableTextLen + 1;
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
		/// Gets the index of the first char in the physical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			return Document.GetLineHeadIndexFromCharIndex( charIndex );
		}

		/// <summary>
		/// Calculates physical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex )
		{
			Document.GetLineColumnIndexFromCharIndex( charIndex, out lineIndex, out columnIndex );
		}

		/// <summary>
		/// Calculates char-index from physical line/column index.
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
			Document doc = Document;
			int anchor = doc.AnchorIndex;
			int caret = doc.CaretIndex;
			int anchorLine, anchorColumn;
			int caretLine, caretColumn;
			int prevCaretLine = doc.ViewParam.PrevCaretLine;

			// calculate line/column index of current anchor/caret
			GetLineColumnIndexFromCharIndex( anchor, out anchorLine, out anchorColumn );
			GetLineColumnIndexFromCharIndex( caret, out caretLine, out caretColumn );

			try
			{
				if( e.ByContentChanged )
					return;

				// update indicator graphic on horizontal ruler
				UpdateHRuler();

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
					HandleSelectionChanged_OnRectSelect( e );
					return;
				}

				// if there was no selection and is no selection too,
				// update current line highlight if enabled.
				if( e.OldAnchor == e.OldCaret && anchor == caret )
				{
					if( HighlightsCurrentLine
						&& prevCaretLine != caretLine )
					{
						HandleSelectionChanged_UpdateCurrentLineHighlight( prevCaretLine, caretLine );
					}
				}
				// or, does the change release selection?
				else if( e.OldAnchor != e.OldCaret && anchor == caret )
				{
					HandleSelectionChanged_OnReleaseSel( e );
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
						DrawUnderLine( oldCaretLineY, ColorScheme.BackColor );
					}

					// if the change occured in a line?
					if( prevCaretLine == caretLine )
					{
						// in a line.
						if( e.OldCaret < caret )
							HandleSelectionChanged_OnExpandSelInLine( e.OldCaret, caret, prevCaretLine );
						else
							HandleSelectionChanged_OnExpandSelInLine( caret, e.OldCaret, caretLine );
					}
					else
					{
						// not in a line; in multiple lines.
						HandleSelectionChanged_OnExpandSel( e, caretLine, caretColumn );
					}
				}
			}
			catch( Exception ex )
			{
				// if an exception was caught here, it is not a fatal error
				// so avoid crashing application
				Invalidate();
#				if DEBUG
				throw new Exception( "NON FATAL INTERNAL ERROR", ex );
#				else
				ex.GetHashCode(); // (suppressing warning)
#				endif
			}
			finally
			{
				// remember last selection for next invalidation
				doc.ViewParam.PrevCaretLine = caretLine;
				doc.ViewParam.PrevAnchorLine = anchorLine;
			}
		}

		void HandleSelectionChanged_UpdateCurrentLineHighlight( int oldCaretLine, int newCaretLine )
		{
			int prevAnchorLine = Document.ViewParam.PrevAnchorLine;
			int prevCaretLine = Document.ViewParam.PrevCaretLine;

			// invalidate old underline if it is still visible
			if( prevCaretLine == prevAnchorLine && FirstVisibleLine <= prevCaretLine )
			{
				int y = YofLine( prevCaretLine );
				DrawUnderLine( y, ColorScheme.BackColor );
			}
			
			// draw new underline if it is visible
			if( FirstVisibleLine <= newCaretLine )
			{
				int newCaretY = YofLine( newCaretLine );
				DrawUnderLine( newCaretY, ColorScheme.HighlightColor );
			}
		}

		void HandleSelectionChanged_OnRectSelect( SelectionChangedEventArgs e )
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
			firstBeginPos = this.GetVirPosFromIndex( firstBegin );
			lastEndPos = this.GetVirPosFromIndex( lastEnd );
			firstBeginPos.Y -= (LinePadding >> 1);
			lastEndPos.Y -= (LinePadding >> 1);

			// convert it to physical screen coordinate
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

		void HandleSelectionChanged_OnExpandSelInLine( int begin, int end, int beginL )
		{
			DebugUtl.Assert( beginL < LineCount );
			Rectangle rect = new Rectangle();
			int beginLineHead;
			string token = String.Empty;

			// get chars at left of invalid rect
			beginLineHead = GetLineHeadIndex( beginL );
			if( beginLineHead < begin )
			{
				token = Document.GetTextInRange( beginLineHead, begin );
			}
			
			// calculate invalid rect
			rect.X = MeasureTokenEndX( token, 0 );
			rect.Y = YofLine( beginL );
			token = Document.GetTextInRange( beginLineHead, end );
			rect.Width = MeasureTokenEndX( token, 0 ) - rect.X;
			rect.Height = LineSpacing;

			// invalidate
			rect.X -= (ScrollPosX - XofTextArea);
			Invalidate( rect );
		}

		void HandleSelectionChanged_OnExpandSel( SelectionChangedEventArgs e, int caretLine, int caretColumn )
		{
			Document doc = this.Document;
			int begin, beginL;
			int end, endL;
			int beginLineHead, endLineHead;

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
			Invalidate_MultiLines( begin, end, beginL, endL, beginLineHead, endLineHead );
		}

		void HandleSelectionChanged_OnReleaseSel( SelectionChangedEventArgs e )
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

			// if old selection was in one line?
			if( prevCaretLine == prevAnchorLine )
			{
				Rectangle rect = new Rectangle();
				string textBeforeSel = doc.GetTextInRange( beginLineHead, begin );
				string textSelected = doc.GetTextInRange( endLineHead, end );
				int left = MeasureTokenEndX( textBeforeSel, 0 ) - (ScrollPosX - XofTextArea);
				int right = MeasureTokenEndX( textSelected, 0 ) - (ScrollPosX - XofTextArea);
				rect.X = left;
				rect.Y = YofLine( beginL );
				rect.Width = right - left;
				rect.Height = LineSpacing;

				Invalidate( rect );
			}
			else
			{
				Invalidate_MultiLines( begin, end, beginL, endL, beginLineHead, endLineHead );
			}
		}

		internal override void HandleContentChanged( object sender, ContentChangedEventArgs e )
		{
			Point oldCaretPos;
			Rectangle invalidRect1 = new Rectangle();
			Rectangle invalidRect2 = new Rectangle();
			int oldTextLineCount, newTextLineCount;

			// get position of the word replacement occured
			oldCaretPos = GetVirPosFromIndex( e.Index );
			VirtualToScreen( ref oldCaretPos );

			// update indicator graphic on horizontal ruler
			UpdateHRuler();

			// invalidate the part at right of the old selection
			invalidRect1.X = oldCaretPos.X;
			invalidRect1.Y = oldCaretPos.Y - (LinePadding >> 1);
			invalidRect1.Width = VisibleSize.Width - invalidRect1.X;
			invalidRect1.Height = LineSpacing;

			// invalidate all lines below caret
			// if old text or new text contains multiple lines
			oldTextLineCount = LineLogic.CountLine( e.OldText );
			newTextLineCount = LineLogic.CountLine( e.NewText );
			if( 1 < oldTextLineCount || 1 < newTextLineCount )
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

			//DO_NOT//base.HandleContentChanged( sender, e );
			DrawDirtBar( invalidRect1.Top, Document.GetLineIndexFromCharIndex(e.Index) );
		}

		/// <summary>
		/// Requests to invalidate area covered by given text range.
		/// </summary>
		/// <param name="beginIndex">Begin text index of the area to be invalidated.</param>
		/// <param name="endIndex">End text index of the area to be invalidated.</param>
		public override void Invalidate( int beginIndex, int endIndex )
		{
			DebugUtl.Assert( 0 <= beginIndex, "cond: 0 <= beginIndex("+beginIndex+")" );
			DebugUtl.Assert( beginIndex <= endIndex, "cond: beginIndex("+beginIndex+") <= endIndex("+endIndex+")" );
			if( beginIndex == endIndex )
				return;
			
			int beginLineHead, endLineHead;
			int beginL, endL, dummy;

			// get needed coordinates
			GetLineColumnIndexFromCharIndex( beginIndex, out beginL, out dummy );
			GetLineColumnIndexFromCharIndex( endIndex, out endL, out dummy );
			beginLineHead = GetLineHeadIndex( beginL );

			// switch invalidation logic by whether the invalidated area is multiline or not
			if( beginL != endL )
			{
				endLineHead = GetLineHeadIndex( endL ); // this is needed for invalidating multiline selection
				Invalidate_MultiLines( beginIndex, endIndex, beginL, endL, beginLineHead, endLineHead );
			}
			else
			{
				Invalidate_InLine( beginIndex, endIndex, beginL, beginLineHead );
			}
		}
		
		void Invalidate_InLine( int begin, int end, int beginL, int beginLineHead )
		{
			DebugUtl.Assert( 0 <= begin, "cond: 0 <= begin("+begin+")" );
			DebugUtl.Assert( begin <= end, "cond: begin("+begin+") <= end("+end+")" );
			DebugUtl.Assert( end <= Document.Length, "cond: end("+end+") <= Document.Length("+Document.Length+")" );
			DebugUtl.Assert( 0 <= beginL, "cond: 0 <= beginL("+beginL+")" );
			DebugUtl.Assert( beginL <= this.LineCount, "cond: beginL("+beginL+") <= IView.LineCount("+this.LineCount+")" );
			DebugUtl.Assert( beginLineHead <= begin, "cond: beginLineHead("+beginLineHead+") <= begin("+begin+")" );
			if( begin == end )
				return;

			Rectangle rect = new Rectangle();
			string textBeforeSelBegin;
			string textSelected;

			// calculate position of the invalid rect
			textBeforeSelBegin = Document.GetTextInRange( beginLineHead, begin );
			rect.X = MeasureTokenEndX( textBeforeSelBegin, 0 );
			rect.Y = YofLine( beginL );

			// calculate width and height of the invalid rect
			textSelected = Document.GetTextInRange( begin, end );
			rect.Width = MeasureTokenEndX( textSelected, 0 );
			rect.Height = LineSpacing;

			// invalidate
			rect.X -= (ScrollPosX - XofTextArea);
			Invalidate( rect );
		}

		void Invalidate_MultiLines( int begin, int end, int beginLine, int endLine, int beginLineHead, int endLineHead )
		{
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
			Document doc = Document;

			// calculate upper part of the invalid area
			String firstLinePart = doc.GetTextInRange( beginLineHead, begin );
			upper = new Rectangle();
			if( FirstVisibleLine <= beginLine ) // if not visible, no need to invalidate
			{
				upper.X = MeasureTokenEndX( firstLinePart, 0 ) - (ScrollPosX - XofTextArea);
				upper.Y = YofLine( beginLine );
				upper.Width = VisibleSize.Width - upper.X;
				upper.Height = LineSpacing;
			}

			// calculate lower part of the invalid area
			String finalLinePart = doc.GetTextInRange( endLineHead, end );
			lower = new Rectangle();
			if( FirstVisibleLine <= endLine ) // if not visible, no need to invalidate
			{
				lower.X = XofTextArea;
				lower.Y = YofLine( endLine );
				lower.Width = MeasureTokenEndX( finalLinePart, 0 ) - ScrollPosX;
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
		/// <param name="clipRect">clipping rectangle that covers all invalidated region (in client area coordinate)</param>
		public override void Paint( Rectangle clipRect )
		{
			DebugUtl.Assert( FontInfo != null, "invalid state; FontInfo is null" );
			DebugUtl.Assert( Document != null, "invalid state; Document is null" );

			int selBegin, selEnd;
			Point pos = new Point();

			// prepare off-screen buffer
#			if !DRAW_SLOWLY && !PocketPC
			_Gra.BeginPaint( clipRect );
#			endif

#			if DRAW_SLOWLY
			_Gra.ForeColor = Color.Blue;
			_Gra.DrawRectangle( clipRect.X, clipRect.Y, clipRect.Width-1, clipRect.Height-1 );
			_Gra.DrawLine( clipRect.X, clipRect.Y, clipRect.X+clipRect.Width-1, clipRect.Y+clipRect.Height-1 );
			_Gra.DrawLine( clipRect.X+clipRect.Width-1, clipRect.Y, clipRect.X, clipRect.Y+clipRect.Height-1 );
			DebugUtl.Sleep(400);
#			endif

			// draw horizontal ruler
			DrawHRuler( clipRect );

			// draw top margin
			DrawTopMargin();

			// draw all lines
			_Gra.SetClipRect( clipRect );
			pos.X = -(ScrollPosX - XofTextArea);
			pos.Y = YofTextArea;
			for( int i=FirstVisibleLine; i<LineCount; i++ )
			{
				if( pos.Y < clipRect.Bottom && clipRect.Top <= pos.Y+LineSpacing )
				{
					DrawLine( i, pos, clipRect );
				}
				pos.Y += LineSpacing;
			}
			_Gra.RemoveClipRect();

			// fill area below of the text
			_Gra.BackColor = ColorScheme.BackColor;
			_Gra.FillRectangle( 0, pos.Y, VisibleSize.Width, VisibleSize.Height-pos.Y );

			// flush drawing results BEFORE updating current line highlight
			// because the highlight graphic can be drawn outside of the clipping rect
#			if !DRAW_SLOWLY && !PocketPC
			_Gra.EndPaint();
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
					DrawUnderLine( caretPosY, ColorScheme.HighlightColor );
				}
			}
		}

		void DrawLine( int lineIndex, Point pos, Rectangle clipRect )
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
				// get this token
				token = Document.GetTextInRange( begin, end );
				DebugUtl.Assert( 0 < token.Length );

				// calc next drawing pos before drawing text
				{
					int virLeft = pos.X - (XofTextArea - ScrollPosX);
					tokenEndPos.X = MeasureTokenEndX( token, virLeft );
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
					invisibleWidth = MeasureTokenEndX( token, 0, rightLimit, out invisibleCharCount );
					if( invisibleCharCount < token.Length )
					{
						// cut extra (invisible) part of the token
						token = token.Substring( invisibleCharCount );

						// advance drawing position as if the cut part was actually drawn
						pos.X += invisibleWidth;
					}
				}

				// if the token area crosses the RIGHT boundary, cut off extra
				if( clipRect.Right < tokenEndPos.X )
				{
					int visibleCharCount;

					// calculate how many chars can be drawn in the clip-rect
					MeasureTokenEndX( token, pos.X, clipRect.Right, out visibleCharCount );

					// set the position to cut extra trailings of this token
					if( visibleCharCount == token.Length )
					{
						token = token.Substring( 0, visibleCharCount );
					}
					else if( visibleCharCount+2 < token.Length
						&& Document.IsHighSurrogate(token[visibleCharCount]) )
					{
						token = token.Substring( 0, visibleCharCount + 2 );
					}
					else if( visibleCharCount+1 < token.Length )
					{
						token = token.Substring( 0, visibleCharCount + 1 );
					}

					// set token end position to the right limit to terminate loop
					tokenEndPos.X = clipRect.Right;
				}

				// draw this token
				DrawToken( token, klass, inSelection, ref pos, ref tokenEndPos, ref clipRect );

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
					|| (0 < lineEnd && LineLogic.IsEolChar(Document[lineEnd-1]) == false) )
				{
					DrawEofMark( ref pos );
				}
			}

			// fill right of the line text
			if( pos.X < clipRect.Right )
			{
				// to prevent drawing line number area,
				// make drawing pos to text area's left if the line end does not exceed left of text area
				if( pos.X < XofTextArea )
					pos.X = XofTextArea;
				_Gra.BackColor = ColorScheme.BackColor;
				_Gra.FillRectangle( pos.X, pos.Y, clipRect.Right-pos.X, LineSpacing );
			}

			// if this line is wider than the width of virtual space,
			// calculate full width of this line and make it the width of virtual space.
			Point virPos = pos;
			ScreenToVirtual( ref virPos );
			if( TextAreaWidth - TabWidthInPx < virPos.X )
			{
				string lineContent;
				int lineWidth, newWidth;

				// calculate full length of this line, in pixel
				lineContent = Document.GetTextInRange( lineHead, lineEnd );
				lineWidth = MeasureTokenEndX( lineContent, 0 );

				// calculate new text area width
				newWidth = lineWidth;
				while( newWidth < lineWidth+512 )
				{
					newWidth += 1024;
				}

				// apply
				TextAreaWidth = newWidth;
				_UI.UpdateScrollBarRange();
			}

			// draw line number
			DrawLineNumber( pos.Y, lineIndex+1, true );
		}
		#endregion
	}
}
