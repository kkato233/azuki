// file: PropWrapView.cs
// brief: Platform independent view (proportional, line-wrap).
//=========================================================
//DEBUG//#define PLHI_DEBUG
//DEBUG//#define DRAW_SLOWLY
using System;
using System.Drawing;
using System.Diagnostics;
using StringBuilder = System.Text.StringBuilder;

namespace Sgry.Azuki
{
	using TextLayouts;

	/// <summary>
	/// Platform independent view implementation to display wrapped text with proportional font.
	/// </summary>
	class PropWrapView : PropView
	{
		readonly ITextLayout _Layout;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="ui">Implementation of the platform dependent UI module.</param>
		internal PropWrapView( IUserInterface ui )
			: base( ui )
		{
			_Layout = new PropWrapTextLayout( this );
			Document.ViewParam.ScrollPosX = 0;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		internal PropWrapView( View other )
			: base( other )
		{
			_Layout = new PropWrapTextLayout( this );
			Document.ViewParam.ScrollPosX = 0;
		}
		#endregion

		#region Properties
		public override ITextLayout Layout
		{
			get{ return _Layout; }
		}

		/// <summary>
		/// Gets or sets width of the virtual text area (line number area is not included).
		/// </summary>
		public override int TextAreaWidth
		{
			get{ return base.TextAreaWidth; }
			set
			{
				// ignore if negative integer given.
				// (This case may occur when minimizing window.
				// Note that lower boundary check will be done in
				// base.TextAreaWidth so there is no need to check it here.)
				if( value < 0 )
				{
					return;
				}

				if( base.TextAreaWidth != value )
				{
					using( IGraphics g = _UI.GetIGraphics() )
					{
						// update property
						base.TextAreaWidth = value;

						// update screen line head indexes
						string text = Document.Text;
						PLHI.Clear();
						PLHI.Add( 0 );
						UpdatePLHI( g, 0, "", text );

						// re-calculate line index of caret and anchor
						Document.ViewParam.PrevCaretLine
							= GetLineIndexFromCharIndex( Document.CaretIndex );
						Document.ViewParam.PrevAnchorLine
							= GetLineIndexFromCharIndex( Document.AnchorIndex );

						// update desired column
						// (must be done after UpdatePLHI)
						SetDesiredColumn( g );
					}
				}
			}
		}

		/// <summary>
		/// Re-calculates and updates x-coordinate of the right end of the virtual text area.
		/// </summary>
		/// <param name="desiredX">X-coordinate of scroll destination desired.</param>
		/// <returns>The largest X-coordinate which Azuki can scroll to.</returns>
		protected override int ReCalcRightEndOfTextArea( int desiredX )
		{
			return TextAreaWidth - VisibleTextAreaSize.Width;
		}
		#endregion

		#region Drawing Options
		/// <summary>
		/// Gets or sets tab width in count of space chars.
		/// </summary>
		public override int TabWidth
		{
			get{ return base.TabWidth; }
			set
			{
				base.TabWidth = value;

				// refresh PLHI
				string text = Document.Text;
				PLHI.Clear();
				PLHI.Add( 0 );
				UpdatePLHI( 0, "", text );
			}
		}
		#endregion

		#region Event Handlers
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

			Document doc = base.Document;
			bool isMultiLine;
			int prevLineCount;
			Point oldCaretVirPos;
			Rectangle invalidRect1 = new Rectangle();
			Rectangle invalidRect2 = new Rectangle();

			using( IGraphics g = _UI.GetIGraphics() )
			{
				// get position of the replacement
				oldCaretVirPos = GetVirPosFromIndex( g, e.Index );
				if( IsWrappedLineHead(doc, PLHI, e.Index) )
				{
					oldCaretVirPos.Y -= LineSpacing;
					if( oldCaretVirPos.Y < 0 )
					{
						oldCaretVirPos.X = 0;
						oldCaretVirPos.Y = 0;
					}
				}

				// update screen line head indexes
				prevLineCount = LineCount;
				UpdatePLHI( g, e.Index, e.OldText, e.NewText );
#				if PLHI_DEBUG
				string __result_of_new_logic__ = PLHI.ToString();
				DoLayout();
				if( __result_of_new_logic__ != PLHI.ToString() )
				{
					System.Windows.Forms.MessageBox.Show("sync error");
					Console.Error.WriteLine( __result_of_new_logic__ );
					Console.Error.WriteLine( PLHI );
					Console.Error.WriteLine();
				}
#				endif

				// update indicator graphic on horizontal ruler
				UpdateHRuler( g );

				// invalidate the part at right of the old selection
				if( Document.IsCombiningCharacter(e.OldText, 0)
					|| Document.IsCombiningCharacter(e.NewText, 0) )
				{
					invalidRect1.X = 0; // [*1]
				}
				else
				{
					invalidRect1.X = oldCaretVirPos.X;
				}
				invalidRect1.Y = oldCaretVirPos.Y - (LinePadding >> 1);
				invalidRect1.Width = VisibleSize.Width - invalidRect1.X;
				invalidRect1.Height = LineSpacing;
				VirtualToScreen( ref invalidRect1 );

				// invalidate all lines below caret
				// if old text or new text contains multiple lines
				isMultiLine = TextUtil.IsMultiLine( e.NewText );
				if( prevLineCount != PLHI.Count || isMultiLine )
				{
					//NO_NEED//invalidRect2.X = 0;
					invalidRect2.Y = invalidRect1.Bottom;
					invalidRect2.Width = VisibleSize.Width;
					invalidRect2.Height = VisibleSize.Height - invalidRect2.Top;
				}
				else
				{
					// if the replacement changed screen line count,
					// invalidate this *logical* line
					int logLine;
					int logLineEnd;
					Point logLineEndPos;
					int logLineBottom;

					// get position of the char at the end of the logical line
					logLine = doc.GetLineIndexFromCharIndex( e.Index );
					logLineEnd = doc.GetLineHeadIndex( logLine ) + doc.GetLineLength( logLine );
					logLineEndPos = GetVirPosFromIndex( g, logLineEnd );
					VirtualToScreen( ref logLineEndPos );
					logLineBottom = logLineEndPos.Y - (LinePadding >> 1);

					// make a rectangle that covers the logical line area
					//NO_NEED//invalidRect2.X = 0;
					invalidRect2.Y = invalidRect1.Bottom;
					invalidRect2.Width = VisibleSize.Width;
					invalidRect2.Height = (logLineBottom + LineSpacing) - invalidRect2.Top;
				}

				// invalidate the range
				Invalidate( invalidRect1 );
				if( 0 < invalidRect2.Height )
				{
					//--- multiple logical lines are affected ---
					Invalidate( invalidRect2 );
				}

				// update left side of text area
				UpdateDirtBar( g, doc.GetLineIndexFromCharIndex(e.Index) );
				UpdateLineNumberWidth( g );

				//DO_NOT//base.HandleContentChanged( sender, e );
			}
		}

		/// <summary>
		/// Update dirt bar area.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="logLineIndex">dirt bar area for the line indicated by this index will be updated.</param>
		void UpdateDirtBar( IGraphics g, int logLineIndex )
		{
			int logLineHeadIndex, logLineEndIndex;
			Point top, bottom;
			Document doc = this.Document;

			// calculate beginning and ending index of the modified logical line
			logLineHeadIndex = doc.GetLineHeadIndex( logLineIndex );
			logLineEndIndex = logLineHeadIndex + doc.GetLineLength( logLineIndex );

			// get the screen position of both beginning and ending character
			top = this.GetVirPosFromIndex( g, logLineHeadIndex );
			bottom = this.GetVirPosFromIndex( g, logLineEndIndex );
			VirtualToScreen( ref top );
			VirtualToScreen( ref bottom );
			if( bottom.Y < YofTextArea )
			{
				return;
			}
			bottom.Y += LineSpacing;

			// prevent to draw on horizontal ruler
			if( top.Y < YofTextArea )
			{
				top.Y = YofTextArea;
			}

			// adjust drawing position for line padding
			// (move it up, a half of the height of line padding)
			top.Y -= (LinePadding >> 1);
			bottom.Y -= (LinePadding >> 1);

			// overdraw dirt bar
			for( int y=top.Y; y<bottom.Y; y+=LineSpacing )
			{
				DrawDirtBar( g, y, logLineIndex );
			}
		}

		internal override void HandleDocumentChanged( Document prevDocument )
		{
			// update screen line head indexes if needed
			if( Document.ViewParam.LastModifiedTime != Document.LastModifiedTime
				|| Document.ViewParam.LastFontHashCode != FontInfo.GetHashCode()
				|| Document.ViewParam.LastTextAreaWidth != TextAreaWidth )
			{
				DebugUtl.Assert( 0 < PLHI.Count );
				UpdatePLHI( 0, "", Document.Text );
			}

			base.HandleDocumentChanged( prevDocument );
		}
		#endregion

		#region Layout Logic
#if PLHI_DEBUG
		void DoLayout()
		{
			// initialize PLHI
			PLHI.Clear();
			PLHI.Add( 0 );
			
			// if the view is very thin, text may not be able to be rendered
			if( TextAreaWidth < (TabWidthInPx >> 2) )
			{
				return;
			}

			// calculate
			for( int i=0; i<Document.LineCount; i++ )
			{
				DoLayoutOneLine( i );
			}

			PLHI.RemoveRange( PLHI.Count-1, PLHI.Count );
		}

		void DoLayoutOneLine( int lineIndex )
		{
			Document doc = Document;
			int begin, end;
			string lineContent;
			int drawableLength;

			// get range of this line
			begin = doc.GetLineHeadIndex( lineIndex );
			if( lineIndex+1 < doc.LineCount )
				end = doc.GetLineHeadIndex( lineIndex + 1 );
			else
				end = doc.Length;

			// get content of this line
			lineContent = doc.GetTextInRange( begin, end );

			// meaure this line and add line end index to PLHI
			MeasureTokenEndX( lineContent, 0, TextAreaWidth, out drawableLength );
			while( drawableLength < end-begin )
			{
				// whole line can not be printed in the virtual space.
				// so wrap this line

				// make drawable part of this line as a screen line
				PLHI.Add( begin + drawableLength );
				begin += drawableLength;

				// measure following
				lineContent = lineContent.Substring( drawableLength );
				MeasureTokenEndX( lineContent, 0, TextAreaWidth, out drawableLength );
			}

			// add last part
			PLHI.Add( begin + drawableLength );
		}
#endif

		/// <summary>
		/// Maintain line head indexes.
		/// </summary>
		/// <param name="index">The index of the place where replacement was occurred.</param>
		/// <param name="oldText">The text which is removed by the replacement.</param>
		/// <param name="newText">The text which is inserted by the replacement.</param>
		void UpdatePLHI( int index, string oldText, string newText )
		{
			using( IGraphics g = _UI.GetIGraphics() )
			{
				UpdatePLHI( g, index, oldText, newText );
			}
		}

		/// <summary>
		/// Maintain line head indexes.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="index">The index of the place where replacement was occurred.</param>
		/// <param name="oldText">The text which is removed by the replacement.</param>
		/// <param name="newText">The text which is inserted by the replacement.</param>
		void UpdatePLHI( IGraphics g, int index, string oldText, string newText )
		{
			Debug.Assert( 0 < this.TabWidth );
			Document doc = Document;
			int delBeginL, delEndL;
			int reCalcBegin, reCalcEnd;
			int shiftBeginL;
			int diff = newText.Length - oldText.Length;
			int replaceEnd;
			int preTargetEndL;

			// calculate where to recalculate PLHI from
			int firstDirtyLineIndex = TextUtil.GetLineIndexFromCharIndex( PLHI, index );
			if( 0 < firstDirtyLineIndex )
			{
				// we should always recalculate PLHI from previous line of the line replacement occured
				// because word-wrapping may move token at line head to previous line
				firstDirtyLineIndex--;
			}

			// [phase 3] calculate range of indexes to be deleted
			delBeginL = firstDirtyLineIndex + 1;
			int lastDirtyLogLineIndex = doc.GetLineIndexFromCharIndex( index + newText.Length );
			if( lastDirtyLogLineIndex+1 < doc.LineCount )
			{
				int delEnd = doc.GetLineHeadIndex( lastDirtyLogLineIndex + 1 ) - diff;
				delEndL = TextUtil.GetLineIndexFromCharIndex( PLHI, delEnd );
			}
			else
			{
				delEndL = PLHI.Count;
			}
#			if PLHI_DEBUG
			Console.Error.WriteLine("[3] del:[{0}, {1})", delBeginL, delEndL);
#			endif
			
			// [phase 2] calculate range of indexes to be re-calculated
			reCalcBegin = PLHI[ firstDirtyLineIndex ];
			replaceEnd = index + newText.Length;
			preTargetEndL = doc.GetLineIndexFromCharIndex( replaceEnd );
			if( preTargetEndL+1 < doc.LineCount )
			{
				reCalcEnd = doc.GetLineHeadIndex( preTargetEndL+1 );
			}
			else
			{
				reCalcEnd = doc.Length;
			}
#			if PLHI_DEBUG
			Console.Error.WriteLine("[2] rc:[{0}, {1})", reCalcBegin, reCalcEnd);
#			endif

			// [phase 1] calculate range of indexes to be shifted
			if( replaceEnd == doc.Length )
			{
				// there are no more chars following.
				shiftBeginL = Int32.MaxValue;
			}
			else if( replaceEnd < doc.Length
				&& TextUtil.NextLineHead(doc.InternalBuffer, replaceEnd) == -1 )
			{
				// there exists more characters but no lines.
				shiftBeginL = Int32.MaxValue;
			}
			else
			{
				// there are following lines.
				shiftBeginL = TextUtil.GetLineIndexFromCharIndex( PLHI, reCalcEnd - diff );
			}
#			if PLHI_DEBUG
			Console.Error.WriteLine("[1] shift:[{0}, {1})", shiftBeginL, PLHI.Count);
#			endif

			//--- apply ----
			// [phase 1] shift all followings
			for( int i=shiftBeginL; i<PLHI.Count; i++ )
			{
				PLHI[i] += newText.Length - oldText.Length;
			}

			// [phase 2] delete LHI of affected screen lines except first one
			if( delBeginL < delEndL && delEndL <= PLHI.Count )
				PLHI.RemoveRange( delBeginL, delEndL );

			// [phase 3] re-calculate screen line indexes
			// (here we should divide the text in the range into small segments
			// to avoid making unnecessary copy of the text so many times)
			const int segmentLen = 32;
			int x = 0;
			int drawableLen;
			int begin, end;
			int line = delBeginL;
			end = reCalcBegin;
			do
			{
				// calc next segment range
				begin = end;
				if( begin+segmentLen < reCalcEnd )
				{
					end = begin + segmentLen;
				}
				else
				{
					end = reCalcEnd;
				}

				// get next segment
				string str = doc.GetTextInRangeRef( ref begin, ref end );
				x = MeasureTokenEndX( g, str, x, TextAreaWidth, out drawableLen );

				// can this segment be written in this screen line?
				if( drawableLen < str.Length
					|| TextUtil.IsEolChar(str, drawableLen-1) )
				{
					// hit right limit. end this screen line
					end = begin + drawableLen;
					if( TextUtil.IsEolChar(str, drawableLen-1) == false )
					{
						// wrap word
						int newEndIndex = doc.WordProc.HandleWordWrapping( doc, begin+drawableLen );
						if( PLHI[line-1] < newEndIndex )
						{
							end = newEndIndex;
						}
					}
					Debug.Assert( PLHI[line-1] < end, "INTERNAL ERROR" );
					PLHI.Insert( line, end );
					line++;
					x = 0;
				}
			}
			while( end < reCalcEnd );

			// then, remove extra last screen line index made as the result of phase 3
			if( line != delBeginL && line < PLHI.Count )
			{
				PLHI.RemoveAt( line-1 );
			}

			// remember the condition of the calculation
			Document.ViewParam.LastTextAreaWidth = TextAreaWidth;
			Document.ViewParam.LastFontHashCode = FontInfo.GetHashCode();
			Document.ViewParam.LastModifiedTime = Document.LastModifiedTime;
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
			Debug.Assert( FontInfo != null, "invalid state; FontInfo is null" );
			Debug.Assert( Document != null, "invalid state; Document is null" );

			int selBegin, selEnd;
			Point pos = new Point();
			bool shouldRedraw1, shouldRedraw2;

			// prepare off-screen buffer
#			if !DRAW_SLOWLY
			g.BeginPaint( clipRect );
#			endif

#			if DRAW_SLOWLY
			g.ForeColor = Color.Fuchsia;
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
			pos.Y = YofTextArea;
			for( int i=FirstVisibleLine; i<LineCount; i++ )
			{
				if( pos.Y < clipRect.Bottom && clipRect.Top <= pos.Y+LineSpacing )
				{
					// reset x-coord of drawing position
					pos.X = -(ScrollPosX - XofTextArea);

					// invoke pre-draw event
					shouldRedraw1 = _UI.InvokeLineDrawing( g, i, pos );

					// draw the line
					DrawLine( g, i, pos, clipRect );

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

			// fill area below of the text
			g.BackColor = ColorScheme.BackColor;
			g.FillRectangle( 0, pos.Y, VisibleSize.Width, VisibleSize.Height-pos.Y );
			for( int y=pos.Y; y<VisibleSize.Height; y+=LineSpacing )
			{
				DrawLeftOfLine( g, y, -1, false );
			}

			// flush drawing results BEFORE updating current line highlight
			// because the highlight graphic is never limited to clipping rect
#			if !DRAW_SLOWLY
			g.EndPaint();
#			endif

			// draw right edge
			{
				int x = (XofTextArea + TextAreaWidth) - ScrollPosX;
				g.ForeColor = ColorScheme.RightEdgeColor;
				g.DrawLine( x, YofTextArea, x, VisibleSize.Height );
			}

			// draw underline to highlight current line if there is no selection
			Document.GetSelection( out selBegin, out selEnd );
			if( HighlightsCurrentLine && selBegin == selEnd )
			{
				int caretLine, caretPosY;

				// calculate position of the underline
				caretLine = TextUtil.GetLineIndexFromCharIndex( PLHI, Document.CaretIndex );
				caretPosY = YofTextArea + (caretLine - FirstVisibleLine) * LineSpacing;
				
				// draw underline to current line
				DrawUnderLine( g, caretPosY, ColorScheme.HighlightColor );
			}
		}

		void DrawLine( IGraphics g, int lineIndex, Point pos, Rectangle clipRect )
		{
			Debug.Assert( this.FontInfo != null );
			Debug.Assert( this.Document != null );

			// note that given pos is NOT virtual position BUT screen position.
			string token;
			int lineHead, lineEnd;
			int begin, end; // range of the token in the text
			CharClass klass;
			Point tokenEndPos = pos;
			bool inSelection;

			// calc position of head/end of this screen line
			lineHead = PLHI[ lineIndex ];
			if( lineIndex+1 < PLHI.Count )
				lineEnd = PLHI[ lineIndex + 1 ];
			else
				lineEnd = Document.Length;

			// adjust and set clipping rect
			if( clipRect.X < XofTextArea )
			{
				// given clip rect covers line number area.
				// redraw line nubmer and shrink clip rect
				// to avoid overwriting line number by text content
				bool drawsText;
				int lineIndexToDraw;

				if( UseScreenLineNumber )
				{
					lineIndexToDraw = lineIndex;
					drawsText = true;
				}
				else
				{
					// Use logical line index if it's a first screen line of the logical line.
					lineIndexToDraw = Document.GetLineIndexFromCharIndex( lineHead );
					drawsText = ( Document.GetLineHeadIndex(lineIndexToDraw) == lineHead );
				}

				DrawLeftOfLine( g, pos.Y, lineIndexToDraw+1, drawsText );
				clipRect.Width -= (XofTextArea - clipRect.X);
				clipRect.X = XofTextArea;
			}
#			if !DRAW_SLOWLY
			g.SetClipRect( clipRect );
#			endif

			// draw line text
			begin = lineHead;
			end = NextPaintToken( Document, begin, lineEnd, out klass, out inSelection );
			while( end <= lineEnd && end != -1 )
			{
				// get this token
				token = Document.GetTextInRangeRef( ref begin, ref end );
				Debug.Assert( 0 < token.Length, "@View.Paint. NextPaintToken returns empty range." );

				// calc next drawing pos before drawing text
				{
					int virLeft = pos.X - (XofTextArea - ScrollPosX);
					tokenEndPos.X = MeasureTokenEndX( g, new TextSegment(begin, end), virLeft );
					tokenEndPos.X += (XofTextArea - ScrollPosX);
				}

				// if this token is at right of the clip-rect, no need to draw more.
				if( clipRect.Right < pos.X )
				{
					break;
				}

				// if this token is not visible yet, skip this token.
				if( tokenEndPos.X < clipRect.Left )
				{
					goto next_token;
				}

				// if the token area crosses the LEFT boundary of the clip-rect, cut off extra
				if( pos.X < clipRect.Left )
				{
					int invisibleCharCount, invisibleWidth; // invisible char count / width
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
					int visCharCount; // visible char count
					int visPartRight;
					int peekingCharRight = 0;

					// calculate number of chars which fits within the clip-rect
					visPartRight = MeasureTokenEndX( g, token, pos.X, clipRect.Right, out visCharCount );

					// (if the clip-rect's right boundary is NOT the text area's right boundary,
					// we must write one more char so that the peeking char appears at the boundary.)

					// try to get graphically peeking (drawn over the border line) char
					var peekingChar = new TextSegment( visCharCount, TextUtil.NextGraphemeClusterIndex(Document.InternalBuffer, visCharCount) );

					// calculate right end coordinate of the peeking char
					if( peekingChar.IsEmpty == false )
					{
						peekingCharRight = MeasureTokenEndX( g, peekingChar, visPartRight );
					}

					// cut trailing extra
					token = token.Substring( 0, visCharCount+peekingChar.Length );
					tokenEndPos.X = (peekingCharRight != 0) ? peekingCharRight : visPartRight;

					// to terminate this loop, set token end position to invalid one
					end = Int32.MaxValue;
				}

				// draw this token
				DrawToken( g, Document, begin, token, klass, ref pos, ref tokenEndPos, ref clipRect, inSelection );

			next_token:
				// get next token
				pos.X = tokenEndPos.X;
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

#			if !DRAW_SLOWLY
			g.RemoveClipRect();
#			endif
		}

		/// <summary>
		/// Draws underline for the line specified by it's Y coordinate.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="lineTopY">Y-coordinate of the target line.</param>
		/// <param name="color">Color to be used for drawing the underline.</param>
		protected override void DrawUnderLine( IGraphics g, int lineTopY, Color color )
		{
			if( lineTopY < 0 )
				return;

			DebugUtl.Assert( (lineTopY % LineSpacing) == (YofTextArea % LineSpacing), "lineTopY:"+lineTopY+", LineSpacing:"+LineSpacing+", YofTextArea:"+YofTextArea );

			// calculate position to underline
			int bottom = lineTopY + LineHeight + (LinePadding >> 1);

			// draw underline
			Point rightEnd = new Point( TextAreaWidth, 0 );
			VirtualToScreen( ref rightEnd );
			g.ForeColor = color;
			g.DrawLine( XofTextArea, bottom, rightEnd.X-1, bottom );
		}
		#endregion

		#region Utilities
		internal SplitArray<int> PLHI
		{
			get
			{
				Debug.Assert( Document != null );
				return Document.ViewParam.PLHI;
			}
		}

		bool IsEolCode( string str )
		{
			return (str == "\r" || str == "\n" || str == "\r\n");
		}

		int Min( int a, int b, int c )
		{
			return Math.Min(
				Math.Min(a, b),
				c
			);
		}

		static bool IsWrappedLineHead( Document doc, SplitArray<int> plhi, int index )
		{
			int lineHeadIndex = TextUtil.GetLineHeadIndexFromCharIndex( doc.InternalBuffer, plhi, index );
			if( lineHeadIndex <= 0 )
			{
				return false;
			}

			char lastCharOfPrevLine = doc[lineHeadIndex-1];
			return ( TextUtil.IsEolChar(lastCharOfPrevLine) == false );
		}
		#endregion
	}
}
