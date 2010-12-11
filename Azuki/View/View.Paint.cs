// file: View.Paint.cs
// brief: Common painting logic
// author: YAMAMOTO Suguru
// update: 2010-11-27
//=========================================================
//DEBUG//#define DRAW_SLOWLY
using System;
using System.Collections.Generic;
using System.Drawing;
using StringBuilder = System.Text.StringBuilder;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	abstract partial class View
	{
		/// <summary>
		/// Paints content to a graphic device.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="clipRect">clipping rectangle that covers all invalidated region (in client area coordinate)</param>
		public abstract void Paint( IGraphics g, Rectangle clipRect );

		#region Drawing graphical units of view
		/// <summary>
		/// Paints a token including special characters.
		/// </summary>
		protected void DrawToken(
				IGraphics g, Document doc, int tokenIndex,
				string token, CharClass klass,
				ref Point tokenPos, ref Point tokenEndPos, ref Rectangle clipRect, bool inSelection
			)
		{
			Debug.Assert( g != null, "IGraphics must not be null." );
			Debug.Assert( token != null, "given token is null." );
			Debug.Assert( 0 < token.Length, "given token is empty." );
			Point textPos = tokenPos;
			Color foreColor, backColor;
			TextDecoration[] decorations;
			uint markingBitMask;

			// calculate top coordinate of text
			textPos.Y += (LinePadding >> 1);

#			if DRAW_SLOWLY
			if(!Windows.WinApi.IsKeyDownAsync(System.Windows.Forms.Keys.ControlKey))
			{ g.BackColor=Color.Red; g.FillRectangle(tokenPos.X, tokenPos.Y, 2, LineHeight); DebugUtl.Sleep(400); }
#			endif

			// get drawing style for this token
			Utl.ColorFromCharClass(
					ColorScheme, klass, inSelection, out foreColor, out backColor
				);
			g.BackColor = backColor;
			markingBitMask = doc.GetMarkingBitMaskAt( tokenIndex );
			decorations = ColorScheme.GetMarkingDecorations( markingBitMask );

			//--- draw graphic ---
			// space
			if( token == " " )
			{
				// draw background
				g.FillRectangle( tokenPos.X, tokenPos.Y, _SpaceWidth, LineSpacing );

				// draw foreground graphic
				if( DrawsSpace )
				{
					g.ForeColor = ColorScheme.WhiteSpaceColor;
					g.DrawRectangle(
							tokenPos.X + (_SpaceWidth >> 1) - 1,
							textPos.Y + (_LineHeight >> 1),
							1,
							1
						);
				}
			}
			// full-width space
			else if( token == "\x3000" )
			{
				int graLeft, graWidth, graTop, graBottom;

				// calc desired foreground graphic position
				graLeft = tokenPos.X + 2;
				graWidth = _FullSpaceWidth - 5;
				graTop = (textPos.Y + _LineHeight / 2) - (graWidth / 2);
				graBottom = (textPos.Y + _LineHeight / 2) + (graWidth / 2);

				// draw background
				g.FillRectangle( tokenPos.X, tokenPos.Y, _FullSpaceWidth, LineSpacing );

				// draw foreground
				if( DrawsFullWidthSpace )
				{
					g.ForeColor = ColorScheme.WhiteSpaceColor;
					g.DrawRectangle( graLeft, graTop, graWidth, graBottom-graTop );
				}
			}
			// tab
			else if( token == "\t" )
			{
				int bgLeft, bgRight;
				int fgLeft, fgRight;
				int fgTop = textPos.Y + (_LineHeight * 1 / 3);
				int fgBottom = textPos.Y + (_LineHeight * 2 / 3);

				// calc next tab stop (calc in virtual space and convert it to screen coordinate)
				Point tokenVirPos = tokenPos;
				ScreenToVirtual( ref tokenVirPos );
				bgRight = Utl.CalcNextTabStop( tokenVirPos.X, TabWidthInPx );
				bgRight -= ScrollPosX - XofTextArea;
				
				// calc desired foreground graphic position
				fgLeft = tokenPos.X + 2;
				fgRight = bgRight - 2;
				bgLeft = tokenPos.X;

				// draw background
				g.FillRectangle( bgLeft, tokenPos.Y, bgRight-bgLeft, LineSpacing );

				// draw foreground
				if( DrawsTab )
				{
					g.ForeColor = ColorScheme.WhiteSpaceColor;
					g.DrawLine( fgLeft, fgBottom, fgRight, fgBottom );
					g.DrawLine( fgRight, fgBottom, fgRight, fgTop );
				}
			}
			// EOL-Code
			else if( LineLogic.IsEolChar(token, 0) )
			{
				int width;

				// before to draw background,
				// change bgcolor to normal if it's not selected
				if( inSelection == false )
					g.BackColor = ColorScheme.BackColor;

				// draw background
				width = EolCodeWidthInPx;
				g.FillRectangle( tokenPos.X, tokenPos.Y, width, LineSpacing );

				// draw foreground
				if( DrawsEolCode )
				{
					// calc metric
					int y_middle = tokenPos.Y + (LineSpacing >> 1);
					int x_middle = tokenPos.X + (width >> 1); // width/2
					int halfSpaceWidth = (_SpaceWidth >> 1); // _SpaceWidth/2
					int left = tokenPos.X + 1;
					int right = tokenPos.X + width - 2;
					int bottom = y_middle + (width >> 1);

					// draw EOL char's graphic
					g.ForeColor = ColorScheme.EolColor;
					if( token == "\r" ) // CR (left arrow)
					{
						g.DrawLine( left, y_middle, left+halfSpaceWidth, y_middle-halfSpaceWidth );
						g.DrawLine( left, y_middle, tokenPos.X+width-2, y_middle );
						g.DrawLine( left, y_middle, left+halfSpaceWidth, y_middle+halfSpaceWidth );
					}
					else if( token == "\n" ) // LF (down arrow)
					{
						g.DrawLine( x_middle, bottom, x_middle-halfSpaceWidth, bottom-halfSpaceWidth );
						g.DrawLine( x_middle, y_middle-(width>>1), x_middle, bottom );
						g.DrawLine( x_middle, bottom, x_middle+halfSpaceWidth, bottom-halfSpaceWidth );
					}
					else // CRLF (snapped arrow)
					{
						g.DrawLine( right, y_middle-(width>>1), right, y_middle+2 );

						g.DrawLine( left, y_middle+2, left+halfSpaceWidth, y_middle+2-halfSpaceWidth );
						g.DrawLine( right, y_middle+2, left, y_middle+2 );
						g.DrawLine( left, y_middle+2, left+halfSpaceWidth, y_middle+2+halfSpaceWidth );
					}
				}
			}
			// matched bracket
			else if( doc.CaretIndex == doc.AnchorIndex // ensure nothing is selected
				&& doc.IsMatchedBracket(tokenIndex) )
			{
				Color textColor = ColorScheme.MatchedBracketFore;
				g.BackColor = ColorScheme.MatchedBracketBack;
				if( textColor == Color.Transparent )
				{
					textColor = foreColor;
				}

				g.FillRectangle( tokenPos.X, tokenPos.Y, tokenEndPos.X-tokenPos.X, LineSpacing );
				g.DrawText( token, ref textPos, textColor );
			}
			else
			{
				// draw normal visible text
				g.FillRectangle( tokenPos.X, tokenPos.Y, tokenEndPos.X-tokenPos.X, LineSpacing );
				g.DrawText( token, ref textPos, foreColor );
			}

			// decorate token
			foreach( TextDecoration decoration in decorations )
			{
				if( decoration is UnderlineTextDecoration )
				{
					DrawToken_Underline(
							g, token, tokenPos, tokenEndPos,
							(UnderlineTextDecoration)decoration,
							foreColor
						);
				}
				else if( decoration is OutlineTextDecoration )
				{
					DrawToken_Outline(
							g, doc, token, tokenIndex, tokenPos, tokenEndPos,
							(OutlineTextDecoration)decoration,
							foreColor, markingBitMask
						);
				}
			}
		}

		void DrawToken_Underline(
				IGraphics g, string token,
				Point tokenPos, Point tokenEndPos,
				UnderlineTextDecoration decoration,
				Color currentForeColor
			)
		{
			Debug.Assert( g != null );
			Debug.Assert( token != null );
			Debug.Assert( decoration != null );

			if( decoration.LineStyle == LineStyle.None )
				return;

			// prepare drawing
			if( decoration.LineColor == Color.Transparent )
			{
				g.ForeColor = currentForeColor;
				g.BackColor = currentForeColor;
			}
			else
			{
				g.ForeColor = decoration.LineColor;
				g.BackColor = decoration.LineColor;
			}

			// draw underline
			if( decoration.LineStyle == LineStyle.Dotted )
			{
				int dotSize = (_Font.Size / 13) + 1;
				int dotSpacing = dotSize << 1;
				int offsetX = tokenPos.X % dotSpacing;
				for( int x=tokenPos.X-offsetX; x<tokenEndPos.X; x += dotSpacing )
				{
					g.FillRectangle(
						x, tokenPos.Y + LineHeight - dotSize,
						dotSize, dotSize );
				}
			}
			else if( decoration.LineStyle == LineStyle.Dashed )
			{
				int lineWidthSize = (_Font.Size / 13) + 1;
				int lineLength = lineWidthSize + (lineWidthSize << 2);
				int lineSpacing = lineWidthSize << 3;
				int offsetX = tokenPos.X % lineSpacing;
				for( int x=tokenPos.X-offsetX; x<tokenEndPos.X; x +=lineSpacing )
				{
					g.FillRectangle(
						x, tokenPos.Y + LineHeight - lineWidthSize,
						lineLength, lineWidthSize );
				}
			}
			else if( decoration.LineStyle == LineStyle.Waved )
			{
				int lineWidthSize = (_Font.Size / 24) + 1;
				int lineLength = lineWidthSize + (lineWidthSize << 2);
				int waveHeight = (_Font.Size / 6) + 1;
				int lineSpacing = lineWidthSize << 3;
				int offsetX = tokenPos.X % (waveHeight << 1);

				int valleyY = tokenPos.Y + LineHeight - lineWidthSize;
				int ridgeY = valleyY - waveHeight;
				for( int x=tokenPos.X-offsetX; x<tokenEndPos.X; x += (waveHeight<<1) )
				{
					int ridgeX = x + waveHeight;
					int valleyX = ridgeX + waveHeight;
					g.DrawLine( x, valleyY, ridgeX, ridgeY );
					g.DrawLine( ridgeX, ridgeY, valleyX, valleyY );
				}
			}
			else if( decoration.LineStyle == LineStyle.Double )
			{
				int lineWidth = (_Font.Size / 24) + 1;

				g.FillRectangle(
					tokenPos.X, tokenPos.Y + LineHeight - (3*lineWidth),
					tokenEndPos.X, lineWidth );

				g.FillRectangle(
					tokenPos.X, tokenPos.Y + LineHeight - lineWidth,
					tokenEndPos.X, lineWidth );
			}
			else if( decoration.LineStyle == LineStyle.Solid )
			{
				int lineWidth = (_Font.Size / 24) + 1;

				g.FillRectangle(
					tokenPos.X, tokenPos.Y + LineHeight - lineWidth,
					tokenEndPos.X, lineWidth );
			}
		}

		void DrawToken_Outline(
				IGraphics g, Document doc,
				string token, int tokenIndex,
				Point tokenPos, Point tokenEndPos,
				OutlineTextDecoration decoration,
				Color currentForeColor, uint markingBitMask
			)
		{
			Debug.Assert( g != null );
			Debug.Assert( doc != null );
			Debug.Assert( 0 <= tokenIndex && tokenIndex < doc.Length );
			Debug.Assert( token != null );
			Debug.Assert( decoration != null );

			int tokenEndIndex = tokenIndex + token.Length;

			// prepare drawing
			if( decoration.LineColor == Color.Transparent )
				g.BackColor = currentForeColor;
			else
				g.BackColor = decoration.LineColor;
			int w = (_Font.Size / 24) + 1;
			Rectangle rect = new Rectangle();
			rect.X = tokenPos.X;
			rect.Y = tokenPos.Y + 1;
			rect.Width = tokenEndPos.X - tokenPos.X;
			rect.Height = LineSpacing - w - 2; // 1 == width of current line highlight

			// draw top line
			g.FillRectangle( rect.Left, rect.Top, rect.Width, w );

			// draw right line if previous character is marked same value
			if( doc.Length <= tokenEndIndex
				|| doc.GetMarkingBitMaskAt(tokenEndIndex) != markingBitMask )
			{
				g.FillRectangle( rect.Right - w, rect.Top, w, rect.Height );
			}

			// draw bottom line
			g.FillRectangle( rect.Left, rect.Bottom - w, rect.Width, w );

			// draw left line
			if( tokenIndex-1 < 0
				|| doc.GetMarkingBitMaskAt(tokenIndex-1) != markingBitMask )
			{
				g.FillRectangle( rect.Left, rect.Top, w, rect.Height );
			}
		}

		/// <summary>
		/// Draws underline for the line specified by it's Y coordinate.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="lineTopY">Y-coordinate of the target line.</param>
		/// <param name="color">Color to be used for drawing the underline.</param>
		protected virtual void DrawUnderLine( IGraphics g, int lineTopY, Color color )
		{
			if( lineTopY < 0 )
				return;

			DebugUtl.Assert( (lineTopY % LineSpacing) == (YofTextArea % LineSpacing), "lineTopY:"+lineTopY+", LineSpacing:"+LineSpacing+", YofTextArea:"+YofTextArea );

			// calculate position to underline
			int bottom = lineTopY + LineHeight + (LinePadding >> 1);

			// determine color of the underline
			if( _UI.Focused )
				g.ForeColor = color;
			else
				g.ForeColor = ColorScheme.BackColor;

			// draw under line
			g.DrawLine( XofTextArea, bottom, _VisibleSize.Width, bottom );
		}

		/// <summary>
		/// Draws dirt bar.
		/// </summary>
		protected void DrawDirtBar( IGraphics g, int lineTopY, int logicalLineIndex )
		{
			Debug.Assert( ((lineTopY-YofTextArea) % LineSpacing) == 0, "((lineTopY-YofTextArea) % LineSpacing) is not 0 but " + (lineTopY-YofTextArea) % LineSpacing );
			LineDirtyState dirtyState;
			Color backColor;

			// get dirty state of the line
			if( 0 <= logicalLineIndex && logicalLineIndex < Document.LineCount )
				dirtyState = Document.GetLineDirtyState( logicalLineIndex );
			else
				dirtyState = LineDirtyState.Clean;

			// choose background color
			if( dirtyState == LineDirtyState.Cleaned )
			{
				backColor = ColorScheme.CleanedLineBar;
				if( backColor == Color.Transparent )
				{
					backColor = Utl.BackColorOfLineNumber( ColorScheme );
				}
			}
			else if( dirtyState == LineDirtyState.Dirty )
			{
				backColor = ColorScheme.DirtyLineBar;
				if( backColor == Color.Transparent )
				{
					backColor = Utl.BackColorOfLineNumber( ColorScheme );
				}
			}
			else
			{
				backColor = Utl.BackColorOfLineNumber( ColorScheme );
			}

			// fill
			g.BackColor = backColor;
			g.FillRectangle( XofDirtBar, lineTopY, DirtBarWidth, LineSpacing );
		}

		/// <summary>
		/// Draws line number area at specified line.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="lineTopY">Y-coordinate of the target line.</param>
		/// <param name="lineNumber">line number to be drawn.</param>
		/// <param name="drawsText">specify true if line number text should be drawn.</param>
		protected void DrawLeftOfLine( IGraphics g, int lineTopY, int lineNumber, bool drawsText )
		{
			DebugUtl.Assert( (lineTopY % LineSpacing) == (YofTextArea % LineSpacing), "lineTopY:"+lineTopY+", LineSpacing:"+LineSpacing+", YofTextArea:"+YofTextArea );
			Point pos = new Point( XofLineNumberArea, lineTopY );
			
			// fill line number area
			if( ShowLineNumber )
			{
				g.BackColor = Utl.BackColorOfLineNumber( ColorScheme );
				g.FillRectangle( XofLineNumberArea, pos.Y, LineNumAreaWidth, LineSpacing );
			}

			// fill dirt bar
			if( ShowsDirtBar )
			{
				DrawDirtBar( g, lineTopY, lineNumber-1 );
			}
			
			// fill left margin area
			if( 0 < LeftMargin )
			{
				g.BackColor = ColorScheme.BackColor;
				g.FillRectangle( XofLeftMargin, pos.Y, LeftMargin, LineSpacing );
			}
			
			// draw line number text
			if( ShowLineNumber && drawsText )
			{
				string lineNumText;
				Point textPos;

				// calculate text position
				lineNumText = lineNumber.ToString();
				pos.X = XofDirtBar - g.MeasureText( lineNumText ).Width - LineNumberAreaPadding;
				textPos = pos;
				textPos.Y += (LinePadding >> 1);

				// draw text
				g.DrawText( lineNumText, ref textPos, Utl.ForeColorOfLineNumber(ColorScheme) );
			}

			// draw margin line between the line number area and text area
			if( ShowLineNumber || ShowsDirtBar )
			{
				pos.X = XofLeftMargin - 1;
				g.ForeColor = Utl.ForeColorOfLineNumber( ColorScheme );
				g.DrawLine( pos.X, pos.Y, pos.X, pos.Y+LineSpacing );
			}
		}

		/// <summary>
		/// Draws horizontal ruler on top of the text area.
		/// </summary>
		protected void DrawHRuler( IGraphics g, Rectangle clipRect )
		{
			string columnNumberText;
			int lineX, rulerIndex;
			int leftMostLineX, leftMostRulerIndex;
			int indexDiff;

			if( ShowsHRuler == false || YofTopMargin < clipRect.Y )
				return;

			g.SetClipRect( clipRect );

			// fill ruler area
			g.ForeColor = Utl.ForeColorOfLineNumber( ColorScheme );
			g.BackColor = Utl.BackColorOfLineNumber( ColorScheme );
			g.FillRectangle( 0, YofHRuler, VisibleSize.Width, HRulerHeight );

			// if clipping rectangle covers left of text area,
			// reset clipping rect that does not covers left of text area
			if( clipRect.X < XofLeftMargin )
			{
				clipRect.Width -= XofLeftMargin - clipRect.X;
				clipRect.X = XofLeftMargin;
				g.RemoveClipRect();
				g.SetClipRect( clipRect );
			}

			// calculate first line to be drawn
			leftMostRulerIndex = ScrollPosX / HRulerUnitWidth;
			leftMostLineX = XofTextArea + (leftMostRulerIndex * HRulerUnitWidth) - ScrollPosX;
			while( leftMostLineX < clipRect.Left )
			{
				leftMostRulerIndex++;
				leftMostLineX += HRulerUnitWidth;
			}

			// align first line index to largest multiple of 10 to ensure number text will not be cut off
			indexDiff = (leftMostRulerIndex % 10);
			if( 1 <= indexDiff && indexDiff <= 5 )
			{
				leftMostRulerIndex -= indexDiff;
				leftMostLineX -= indexDiff * HRulerUnitWidth;
			}

			// draw lines on the ruler
			g.FontInfo = _HRulerFont;
			lineX = leftMostLineX;
			rulerIndex = leftMostRulerIndex;
			while( lineX < clipRect.Right )
			{
				// draw ruler line
				if( (rulerIndex % 10) == 0 )
				{
					Point pos;

					// draw largest line
					g.DrawLine( lineX, YofHRuler, lineX, YofHRuler+HRulerHeight );

					// draw column text
					columnNumberText = (rulerIndex / 10).ToString();
					pos = new Point( lineX+2, YofHRuler );
					g.DrawText( columnNumberText, ref pos, Utl.ForeColorOfLineNumber(ColorScheme) );
				}
				else if( (rulerIndex % 5) == 0 )
				{
					// draw middle-length line
					g.DrawLine( lineX, YofHRuler+_HRulerY_5, lineX, YofHRuler+HRulerHeight );
				}
				else
				{
					// draw smallest line
					g.DrawLine( lineX, YofHRuler+_HRulerY_1, lineX, YofHRuler+HRulerHeight );
				}

				// go to next ruler line
				rulerIndex++;
				lineX += HRulerUnitWidth;
			}
			g.FontInfo = _Font;

			// draw bottom border line
			g.DrawLine(
					XofLeftMargin-1, YofHRuler + HRulerHeight - 1,
					VisibleSize.Width, YofHRuler + HRulerHeight - 1
				);

			// draw indicator of caret column
			g.BackColor = ColorScheme.ForeColor;
			if( HRulerIndicatorType == HRulerIndicatorType.Position )
			{
				int indicatorWidth = 2;

				// calculate indicator region
				Point caretPos = GetVirPosFromIndex( g, Document.CaretIndex );
				VirtualToScreen( ref caretPos );
				if( caretPos.X < XofTextArea )
				{
					indicatorWidth -= XofTextArea - caretPos.X;
					caretPos.X = XofTextArea;
				}

				// draw indicator
				if( 0 < indicatorWidth )
				{
					g.FillRectangle( caretPos.X, YofHRuler, indicatorWidth, HRulerHeight );
				}

				// remember lastly drawn ruler bar position
				Document.ViewParam.PrevHRulerVirX = caretPos.X - XofTextArea + ScrollPosX;
			}
			else if( HRulerIndicatorType == HRulerIndicatorType.CharCount )
			{
				int indicatorX, indicatorWidth;
				int caretLineIndex, caretColumnIndex;

				// calculate indicator region
				GetLineColumnIndexFromCharIndex( Document.CaretIndex, out caretLineIndex, out caretColumnIndex );
				indicatorWidth = HRulerUnitWidth - 1;
				indicatorX = leftMostLineX + (caretColumnIndex - leftMostRulerIndex) * HRulerUnitWidth;
				if( indicatorX < XofTextArea )
				{
					indicatorWidth -= XofTextArea - indicatorX;
					indicatorX = XofTextArea;
				}
				
				// draw indicator
				if( 0 < indicatorWidth )
				{
					g.FillRectangle( indicatorX+1, YofHRuler, indicatorWidth, HRulerHeight-1 );
				}

				// remember lastly filled ruler segmentr position
				Document.ViewParam.PrevHRulerVirX = indicatorX - XofTextArea + ScrollPosX;
			}
			else// if( HRulerIndicatorType == HRulerIndicatorType.Segment )
			{
				// calculate indicator region
				int indicatorWidth = HRulerUnitWidth - 1;
				Point indicatorPos = GetVirPosFromIndex( g, Document.CaretIndex );
				indicatorPos.X -= (indicatorPos.X % HRulerUnitWidth);
				VirtualToScreen( ref indicatorPos );
				if( indicatorPos.X < XofTextArea )
				{
					indicatorWidth -= XofTextArea - indicatorPos.X;
					indicatorPos.X = XofTextArea;
				}

				// draw indicator
				if( 0 < indicatorWidth )
				{
					g.FillRectangle( indicatorPos.X+1, YofHRuler, indicatorWidth, HRulerHeight-1 );
				}

				// remember lastly filled ruler segmentr position
				Document.ViewParam.PrevHRulerVirX = indicatorPos.X - XofTextArea + ScrollPosX;
			}

			g.RemoveClipRect();
		}

		/// <summary>
		/// Draws top margin.
		/// </summary>
		protected void DrawTopMargin( IGraphics g )
		{
			// fill area above the line-number area [copied from DrawLineNumber]
			g.BackColor = Utl.BackColorOfLineNumber( ColorScheme );
			g.FillRectangle(
					XofLineNumberArea, YofTopMargin,
					XofTextArea-XofLineNumberArea, TopMargin
				);
			
			// fill left margin area [copied from DrawLineNumber]
			g.BackColor = ColorScheme.BackColor;
			g.FillRectangle( XofLeftMargin, YofTopMargin, LeftMargin, TopMargin );

			// draw margin line between the line number area and text area [copied from DrawLineNumber]
			int x = XofLeftMargin - 1;
			g.ForeColor = Utl.ForeColorOfLineNumber( ColorScheme );
			g.DrawLine( x, YofTopMargin, x, YofTopMargin+TopMargin );

			// fill area above the text area
			g.BackColor = ColorScheme.BackColor;
			g.FillRectangle( XofTextArea, YofTopMargin, VisibleSize.Width-XofTextArea, TopMargin );
		}

		/// <summary>
		/// Draws EOF mark.
		/// </summary>
		protected void DrawEofMark( IGraphics g, ref Point pos )
		{
			Point textPos;

			g.BackColor = ColorScheme.BackColor;
			if( UserPref.UseTextForEofMark )
			{
				int margin = (_SpaceWidth >> 2);

				// fill background
				int width = g.MeasureText( "[EOF]" ).Width;
				g.FillRectangle( pos.X, pos.Y, width+margin, LineSpacing );

				// calculate text position
				pos.X += margin;
				textPos = pos;
				textPos.Y += (LinePadding >> 1);

				// draw text
				g.DrawText( "[EOF]", ref textPos, ColorScheme.EofColor );
				pos.X += width;
			}
			else
			{
				int width = LineHeight - (LineHeight >> 2);

				// fill background
				g.FillRectangle( pos.X, pos.Y, width, LineSpacing );

				// draw graphic
				textPos = pos;
				textPos.Y += (LinePadding >> 1);
				g.ForeColor = ColorScheme.EofColor;
				g.DrawLine( pos.X+2, textPos.Y+2, pos.X+2, textPos.Y + LineHeight - 3 );
				g.DrawLine( pos.X+2, textPos.Y+2, pos.X + width - 3, textPos.Y+2 );
				g.DrawLine( pos.X+2, textPos.Y + LineHeight - 3, pos.X + width - 3, textPos.Y+2 );
				pos.X += width;
			}
		}
		#endregion

		#region Special updating logic
		protected void UpdateHRuler( IGraphics g )
		{
			if( ShowsHRuler == false )
				return;

			Rectangle updateRect_old;
			Rectangle udpateRect_new;

			if( HRulerIndicatorType == HRulerIndicatorType.Position )
			{
				// get virtual position of the new caret
				Point newCaretScreenPos = GetVirPosFromIndex( g, Document.CaretIndex );
				VirtualToScreen( ref newCaretScreenPos );

				// get previous screen position of the caret
				int oldCaretX = Document.ViewParam.PrevHRulerVirX + XofTextArea - ScrollPosX;
				if( oldCaretX == newCaretScreenPos.X )
				{
					return; // horizontal poisition of the caret not changed
				}

				// calculate indicator rectangle for old caret position
				updateRect_old = new Rectangle(
						oldCaretX, YofHRuler, 2, HRulerHeight
					);

				// calculate indicator rectangle for new caret position
				udpateRect_new = new Rectangle(
						newCaretScreenPos.X, YofHRuler, 2, HRulerHeight
					);
			}
			else if( HRulerIndicatorType == HRulerIndicatorType.CharCount )
			{
				int dummy;
				int newCaretColumnIndex;
				int oldSegmentX, newSegmentX;

				// calculate new segment of horizontal ruler
				GetLineColumnIndexFromCharIndex(
						Document.CaretIndex, out dummy, out newCaretColumnIndex
					);
				newSegmentX = (newCaretColumnIndex * HRulerUnitWidth) + XofTextArea - ScrollPosX;

				// calculate previous segment of horizontal ruler
				oldSegmentX = Document.ViewParam.PrevHRulerVirX + XofTextArea - ScrollPosX;
				if( oldSegmentX == newSegmentX )
				{
					return; // horizontal poisition of the caret not changed
				}

				// calculate indicator rectangle for old caret position
				updateRect_old = new Rectangle(
						oldSegmentX, YofHRuler, HRulerUnitWidth, HRulerHeight
					);

				// calculate indicator rectangle for new caret position
				udpateRect_new = new Rectangle(
						newSegmentX, YofHRuler, HRulerUnitWidth, HRulerHeight
					);
			}
			else// if( HRulerIndicatorType == HRulerIndicatorType.Segment )
			{
				int oldSegmentX, newSegmentX;

				// get virtual position of the new caret
				Point newCaretScreenPos = GetVirPosFromIndex( g, Document.CaretIndex );
				VirtualToScreen( ref newCaretScreenPos );

				// calculate new segment of horizontal rulse
				int leftMostRulerIndex = ScrollPosX / HRulerUnitWidth;
				int leftMostLineX = XofTextArea + (leftMostRulerIndex * HRulerUnitWidth) - ScrollPosX;
				newSegmentX = leftMostLineX;
				while( newSegmentX+HRulerUnitWidth <= newCaretScreenPos.X )
				{
					newSegmentX += HRulerUnitWidth;
				}

				// calculate previous segment of horizontal ruler
				oldSegmentX = Document.ViewParam.PrevHRulerVirX + XofTextArea - ScrollPosX;
				if( oldSegmentX == newSegmentX )
				{
					return; // segment was not changed
				}

				// calculate invalid rectangle
				updateRect_old = new Rectangle(
						oldSegmentX, YofHRuler, HRulerUnitWidth, HRulerHeight
					);
				udpateRect_new = new Rectangle(
						newSegmentX, YofHRuler, HRulerUnitWidth, HRulerHeight
					);
			}

			// not invalidate but DRAW old and new indicator here
			// (because if all invalid rectangles was combined,
			// invalidating area in horizontal ruler makes
			// large invalid rectangle and has bad effect on performance,
			// especially on mobile devices.)
			DrawHRuler( g, updateRect_old );
			DrawHRuler( g, udpateRect_new );
		}
		#endregion

		#region Measuring paint text token
		/// <summary>
		/// Calculates x-coordinate of the right end of given token drawed at specified position with specified tab-width.
		/// </summary>
		internal int MeasureTokenEndX( IGraphics g, string token, int virX )
		{
			int dummy;
			return MeasureTokenEndX( g, token, virX, Int32.MaxValue, out dummy );
		}

		/// <summary>
		/// Calculates x-coordinate of the right end of given token
		/// drawed at specified position with specified tab-width.
		/// </summary>
		protected int MeasureTokenEndX( IGraphics g, string token, int virX, int rightLimitX, out int drawableLength )
		{
			StringBuilder subToken;
			int x = virX;
			int relDLen; // relatively calculated drawable length
			int subTokenWidth;
			bool hitRightLimit;

			drawableLength = 0;
			if( token.Length == 0 )
			{
				return x;
			}

			// for each char
			subToken = new StringBuilder( token.Length );
			for( int i=0; i<token.Length; i++ )
			{
				if( token[i] == '\t' )
				{
					//--- found a tab ---
					// calculate drawn length of cached characters
					hitRightLimit = MeasureTokenEndX_TreatSubToken( g, i, subToken, rightLimitX, ref x, ref drawableLength );
					if( hitRightLimit )
					{
						// before this tab, cached characters already hit the limit.
						return x;
					}

					// calc next tab stop
					subTokenWidth = Utl.CalcNextTabStop( x, TabWidthInPx );
					if( rightLimitX <= subTokenWidth )
					{
						// this tab hit the right limit.
						Debug.Assert( drawableLength == i );
						drawableLength = i;
						return x;
					}
					drawableLength++;
					x = subTokenWidth;
				}
				else if( LineLogic.IsEolChar(token, i) )
				{
					//--- detected an EOL char ---
					// calculate drawn length of cached characters
					hitRightLimit = MeasureTokenEndX_TreatSubToken( g, i, subToken, rightLimitX, ref x, ref drawableLength );
					if( hitRightLimit )
					{
						// before this EOL char, cached characters already hit the limit.
						return x;
					}

					// check whether this EOL code can be drawn or not
					if( rightLimitX <= x + EolCodeWidthInPx )
					{
						// this EOL code hit the right limit.
						return x;
					}
					x += EolCodeWidthInPx;

					// treat this EOL code
					drawableLength++;
					if( token[i] == '\r'
						&& i+1 < token.Length && token[i+1] == '\n' )
					{
						drawableLength++;
					}
					return x;
				}
				else
				{
					if( 64 < subToken.Length )
					{
						// pretty long text was cached.
						// calculate its width and check whether drawable or not
						hitRightLimit = MeasureTokenEndX_TreatSubToken( g, i, subToken, rightLimitX, ref x, ref drawableLength );
						if( hitRightLimit )
						{
							return x; // hit the right limit
						}
					}

					// append one grapheme cluster
					subToken.Append( token[i] );
					while( Document.IsNotDividableIndex(token, i+1) )
					{
						subToken.Append( token[i+1] );
						i++;
					}
				}
			}

			// calc last sub-token
			if( 0 < subToken.Length )
			{
				x += g.MeasureText( subToken.ToString(), rightLimitX-x, out relDLen ).Width;
				if( relDLen < subToken.Length )
				{
					drawableLength = token.Length - (subToken.Length - relDLen);
					Debug.Assert( Document.IsNotDividableIndex(token, drawableLength) == false );
					return x; // hit the right limit.
				}
				drawableLength += subToken.Length;
			}
			Debug.Assert( Document.IsNotDividableIndex(token, drawableLength) == false );

			// whole part of the given token can be drawn at given width.
			return x;
		}

		/// <returns>true if measured right poisition hit the limit.</returns>
		static bool MeasureTokenEndX_TreatSubToken( IGraphics gra, int i, StringBuilder subToken, int rightLimitX, ref int x, ref int drawableLength )
		{
			int subTokenWidth;
			int relDLen;
			
			if( subToken.Length == 0 )
			{
				return false;
			}

			subTokenWidth = gra.MeasureText( subToken.ToString(), rightLimitX-x, out relDLen ).Width;
			if( relDLen < subToken.Length )
			{
				// given width is too narrow to draw this sub-token.
				// chop after the limit and re-calc subtoken's width
				drawableLength = i - (subToken.Length - relDLen);
				x += gra.MeasureText(
						subToken.ToString(0, relDLen)
					).Width;
				subToken.Length = 0;
				return true;
			}
			Debug.Assert( Document.IsNotDividableIndex(subToken.ToString(), relDLen) == false );

			x += subTokenWidth;
			drawableLength += subToken.Length;
			subToken.Length = 0;

			return false;
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Calculates end index of the drawing token at longest case by selection state.
		/// </summary>
		int CalcTokenEndAtMost( Document doc, int index, int nextLineHead, out bool inSelection )
		{
			DebugUtl.Assert( doc != null );
			DebugUtl.Assert( index < doc.Length );
			DebugUtl.Assert( index < nextLineHead && nextLineHead <= doc.Length );
			int selBegin, selEnd;

			// get selection range on the line
			doc.GetSelection( out selBegin, out selEnd );
			if( doc.RectSelectRanges != null )
			{
				//--- rectangle selection ---
				int i;

				// find a row that is on the drawing line
				// (finding a begin-end pair whose 'end' is at middle of 'index' and 'nextLineHead')
				for( i=0; i<doc.RectSelectRanges.Length; i+=2 )
				{
					selBegin = doc.RectSelectRanges[i];
					selEnd = doc.RectSelectRanges[i+1];
					if( index <= selEnd && selEnd < nextLineHead )
					{
						break;
					}
				}
				if( doc.RectSelectRanges.Length <= i )
				{
					// this line is not selected
					inSelection = false;
					return nextLineHead;
				}
			}

			if( index < selBegin )
			{
				// token begins before selection range.
				// so this token is out of selection and must stops before reaching the selection
				inSelection = false;
				return Math.Min( selBegin, nextLineHead );
			}
			else if( index < selEnd )
			{
				// token is in selection.
				// this token must stops in the selection range
				inSelection = true;
				return Math.Min( selEnd, nextLineHead );
			}
			else
			{
				inSelection = false;
				return nextLineHead;
			}
		}

		/// <summary>
		/// Gets next token for painting.
		/// </summary>
		protected int NextPaintToken(
				Document doc, int index, int nextLineHead,
				out CharClass out_klass, out bool out_inSelection
			)
		{
			DebugUtl.Assert( nextLineHead <= doc.Length, "param 'nextLineHead'("+nextLineHead+") must not be greater than 'doc.Length'("+doc.Length+")." );

			char firstCh, ch;
			CharClass firstKlass, klass;
			uint firstMarkingBitMask, markingBitMask;
			int tokenEndLimit;

			out_inSelection = false;

			// if given index is out of range,
			// return -1 to terminate outer loop
			if( nextLineHead <= index )
			{
				out_klass = CharClass.Normal;
				return -1;
			}

			// calculate how many chars should be drawn as one token
			tokenEndLimit = CalcTokenEndAtMost( doc, index, nextLineHead, out out_inSelection );
			if( doc.IsMatchedBracket(index) )
			{
				// if specified index is a bracket paired with a bracket at caret, paint this single char
				out_klass = doc.GetCharClass( index );
				return index + 1;
			}

			// get first char class and selection state
			firstCh = doc[ index ];
			firstKlass = doc.GetCharClass( index );
			firstMarkingBitMask = doc.GetMarkingBitMaskAt( index );
			out_klass = firstKlass;
			if( Utl.IsSpecialChar(firstCh) )
			{
				// treat 1 special char as 1 token
				if( firstCh == '\r'
					&& index+1 < doc.Length
					&& doc[index+1] == '\n' )
				{
					return index + 2;
				}
				else
				{
					return index + 1;
				}
			}
			
			// seek until token end appears
			while( index+1 < tokenEndLimit )
			{
				// get next char
				index++;
				ch = doc[ index ];
				klass = doc.GetCharClass( index );
				markingBitMask = doc.GetMarkingBitMaskAt( index );

				// if this char is a special char, stop seeking
				if( Utl.IsSpecialChar(ch) )
				{
					return index;
				}
				// or if this is matched bracket, stop seeking
				else if( doc.IsMatchedBracket(index) )
				{
					return index;
				}
				// or, character class changed; token ended
				else if( klass != firstKlass )
				{
					return index;
				}
				// or, marking changed; token ended
				else if( markingBitMask != firstMarkingBitMask )
				{
					return index;
				}
			}

			// reached to the limit
			return tokenEndLimit;
		}

		/// <summary>
		/// Class containing small utilities for class View.
		/// </summary>
		protected partial class Utl
		{
			/// <summary>
			/// Gets fore/back color pair from scheme according to char class.
			/// </summary>
			public static void ColorFromCharClass(
					ColorScheme cs, CharClass klass, bool inSelection,
					out Color fore, out Color back
				)
			{
				// set fore and back color
				if( inSelection )
				{
					fore = cs.SelectionFore;
					back = cs.SelectionBack;
				}
				else
				{
					cs.GetColor( klass, out fore, out back );
				}

				// fallback if it is transparent
				if( fore == Color.Transparent )
				{
					fore = cs.ForeColor;
				}
				if( back == Color.Transparent )
				{
					back = cs.BackColor;
				}
			}

			public static Color ForeColorOfLineNumber( ColorScheme cs )
			{
				if( cs.LineNumberFore != Color.Transparent )
					return cs.LineNumberFore;
				else
					return cs.ForeColor;
			}

			public static Color BackColorOfLineNumber( ColorScheme cs )
			{
				if( cs.LineNumberBack != Color.Transparent )
					return cs.LineNumberBack;
				else
					return cs.BackColor;
			}

			/// <summary>
			/// Calculate x-coordinate of the next tab stop.
			/// </summary>
			/// <param name="x">calculates next tab stop from this (X coordinate in virtual space)</param>
			/// <param name="tabWidthInPx">tab width (in pixel)</param>
			public static int CalcNextTabStop( int x, int tabWidthInPx )
			{
				DebugUtl.Assert( 0 < tabWidthInPx );
				return ((x / tabWidthInPx) + 1) * tabWidthInPx;
			}

			/// <summary>
			/// Distinguishs whether given char is special for painting or not.
			/// </summary>
			public static bool IsSpecialChar( char ch )
			{
				if( ch == ' '
					|| ch == '\x3000' // full-width space
					|| ch == '\t'
					|| ch == '\r'
					|| ch == '\n' )
				{
					return true;
				}

				return false;
			}

			/// <summary>
			/// Gets minimum value in four integers.
			/// </summary>
			public static int Min( int a, int b, int c, int d )
			{
				return Math.Min( a, Math.Min(b, Math.Min(c,d) ) );
			}

			/// <summary>
			/// Gets maximum value in four integers.
			/// </summary>
			public static int Max( int a, int b, int c, int d )
			{
				return Math.Max( a, Math.Max(b, Math.Max(c,d) ) );
			}
		}
		#endregion
	}
}
