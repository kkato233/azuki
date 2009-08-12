// file: PropWrapView.cs
// brief: Platform independent view (proportional, line-wrap).
// author: YAMAMOTO Suguru
// update: 2009-06-20
//=========================================================
//DEBUG//#define PLHI_DEBUG
//DEBUG//#define DRAW_SLOWLY
using System;
using System.Drawing;
using System.Diagnostics;
using StringBuilder = System.Text.StringBuilder;

namespace Sgry.Azuki
{
	/// <summary>
	/// Platform independent view implementation to display wrapped text with proportional font.
	/// </summary>
	class PropWrapView : PropView
	{
		int _MinimalWidth;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="ui">Implementation of the platform dependent UI module.</param>
		internal PropWrapView( IUserInterface ui )
			: base( ui )
		{
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		internal PropWrapView( View other )
			: base( other )
		{
		}
		#endregion

		internal override void HandleDocumentChanged( Document prevDocument )
		{
			// update physical line head indexes if needed
			if( Document.ViewParam.LastModifiedTime != Document.LastModifiedTime
				|| Document.ViewParam.LastFontHashCode != Font.GetHashCode()
				|| Document.ViewParam.LastTextAreaWidth != TextAreaWidth )
			{
				DebugUtl.Assert( 0 < PLHI.Count );
				UpdatePLHI( 0, "", Document.Text );
			}

			base.HandleDocumentChanged( prevDocument );
		}

		#region Properties
		/// <summary>
		/// Font to be used for displaying text.
		/// </summary>
		public override Font Font
		{
			set
			{
				base.Font = value;
				_MinimalWidth = Math.Max( _FullSpaceWidth, TabWidthInPx ) << 1;
			}
		}

		/// <summary>
		/// Gets number of the physical lines.
		/// </summary>
		public override int LineCount
		{
			get{ return PLHI.Count; }
		}

		/// <summary>
		/// Gets or sets width of the virtual text area (line number area is not included).
		/// </summary>
		public override int TextAreaWidth
		{
			set
			{
				// ignore if negative integer given.
				// (this case may occur when minimizing window)
				if( value < 0 )
				{
					return;
				}

				// if given value is too small, make it the lowest acceptable value
				// (to avoid infinite loop in painting logic)
				if( value <= _MinimalWidth )
				{
					value = _MinimalWidth;
				}

				if( base.TextAreaWidth != value )
				{
					// update property
					base.TextAreaWidth = value;

					// update physical line head indexes
					string text = Document.Text;
					PLHI.Clear();
					PLHI.Add( 0 );
					UpdatePLHI( 0, "", text );
				}
			}
		}
		#endregion

		#region Position / Index Conversion
		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override Point GetVirPosFromIndex( int index )
		{
			int line, column;

			LineLogic.GetLineColumnIndexFromCharIndex(
					Document.InternalBuffer,
					PLHI,
					index,
					out line,
					out column
				);

			return GetVirPosFromIndex( line, column );
		}

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override Point GetVirPosFromIndex( int lineIndex, int columnIndex )
		{
			if( lineIndex < 0 || columnIndex < 0 )
				throw new ArgumentException( "invalid index was given (minus value)", String.Format("lineIndex:{0} columnIndex:{1}", lineIndex, columnIndex) );

			Point pos = new Point( 0, LineSpacing*lineIndex ); // init value is for when the columnIndex is 0

			// if the location is not the head of the line, calculate x-coord.
			if( 0 < columnIndex )
			{
				// get partial content of the line which exists before the caret
				int lineBegin, lineEnd;
				LineLogic.GetLineRangeWithEol( Document.InternalBuffer, PLHI, lineIndex, out lineBegin, out lineEnd );
				string leftPart = Document.GetTextInRange( lineBegin, lineBegin+columnIndex );

				// measure the characters
				pos.X = MeasureTokenEndX( leftPart, pos.X );
			}

			return pos;
		}

		/// <summary>
		/// Gets char-index of the char at the point specified by location in the virtual space.
		/// </summary>
		/// <returns>the index of the char or -1 if invalid point was specified.</returns>
		public override int GetIndexFromVirPos( Point pt )
		{
			int lineIndex, columnIndex;
			int drawableTextLen;

			// calc line index
			lineIndex = (pt.Y / LineSpacing);
			if( lineIndex < 0 )
			{
				return -1;
			}
			else if( PLHI.Count <= lineIndex
				&& Document.LineCount != 0 )
			{
				// the point indicates beyond the final line.
				// treat as if the final line was specified
				lineIndex = PLHI.Count - 1;
			}

			// calc column index
			columnIndex = 0;
			if( 0 < pt.X )
			{
				int begin, end;
				string line;
				bool isWrapLine = false;
				
				// get content of the line
				LineLogic.GetLineRange( Document.InternalBuffer, PLHI, lineIndex, out begin, out end );
				line = Document.GetTextInRange( begin, end );
				if( end+1 < Document.Length
					&& !LineLogic.IsEolChar(Document.GetTextInRange(end, end+1)[0]) )
				{
					isWrapLine = true;
				}

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
				// if the whole line can be drawn and is a wrapped line,
				// decrease column to avoid indicating invalid position
				else if( isWrapLine )
				{
					columnIndex--;
				}
			}

			return LineLogic.GetCharIndexFromLineColumnIndex( Document.InternalBuffer, PLHI, lineIndex, columnIndex );
		}

		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override int GetLineHeadIndex( int lineIndex )
		{
			if( lineIndex < 0 || PLHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid index was given (lineIndex:"+lineIndex+", LineCount:"+LineCount+")." );

			return PLHI[ lineIndex ];
		}

		/// <summary>
		/// Gets the index of the first char in the physical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			if( charIndex < 0 || Document.Length < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", document.Length:"+Document.Length+")." );

			return LineLogic.GetLineHeadIndexFromCharIndex(
					Document.InternalBuffer, PLHI, charIndex
				);
		}

		/// <summary>
		/// Calculates physical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex )
		{
			if( charIndex < 0 || Document.Length < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", document.Length:"+Document.Length+")." );

			LineLogic.GetLineColumnIndexFromCharIndex(
					Document.InternalBuffer, PLHI, charIndex, out lineIndex, out columnIndex
				);
		}

		/// <summary>
		/// Calculates char-index from physical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex )
		{
			if( lineIndex < 0 || LineCount < lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid index was given (lineIndex:"+lineIndex+", LineCount:"+LineCount+")." );
			if( columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Invalid index was given (columnIndex:"+columnIndex+")." );

			return LineLogic.GetCharIndexFromLineColumnIndex(
					Document.InternalBuffer, PLHI, lineIndex, columnIndex
				);
		}
		#endregion

		#region Appearance Invalidating and Updating (these logic are copy to PropView's one)
		internal override void HandleContentChanged( object sender, ContentChangedEventArgs e )
		{
			Document doc = base.Document;
			int prevLineCount;
			Point oldCaretPos;
			Rectangle invalidRect1 = new Rectangle();
			Rectangle invalidRect2 = new Rectangle();

			// update physical line head indexes
			prevLineCount = LineCount;
			UpdatePLHI( e.Index, e.OldText, e.NewText );
#			if PLHI_DEBUG
			string __result_of_new_logic__ = PLHI.ToString();
			DoLayout();
			if( __result_of_new_logic__ != PLHI.ToString() )
			{
				System.Windows.Forms.MessageBox.Show("sync error");
				Console.Error.WriteLine( __result_of_new_logic__ );
				Console.Error.WriteLine( PLHI );
				Console.Error.WriteLine();
			}
#			endif

			// get position of the word replacement occured
			oldCaretPos = GetVirPosFromIndex( e.Index );

			// invalidate the part at right of the old selection
			invalidRect1.X = oldCaretPos.X -(ScrollPosX - TextAreaX);
			invalidRect1.Y = oldCaretPos.Y -(FirstVisibleLine * LineSpacing);
			invalidRect1.Width = VisibleSize.Width - invalidRect1.X;
			invalidRect1.Height = LineSpacing;

			// invalidate all lines below caret
			// if old text or new text contains multiple lines
			if( prevLineCount != PLHI.Count || 1 < LineLogic.CountLine(e.NewText) )
			{
				//NO_NEED//invalidRect2.X = 0;
				invalidRect2.Y = invalidRect1.Bottom;
				invalidRect2.Width = VisibleSize.Width;
				invalidRect2.Height = VisibleSize.Height - invalidRect2.Top;
			}
			else
			{
				// if the replacement changed physical line count,
				// invalidate this *logical* line
				int logLine;
				int logLineEnd;
				Point logLineEndPos;

				// get position of the char at the end of the logical line
				logLine = doc.GetLineIndexFromCharIndex( e.Index );
				logLineEnd = doc.GetLineHeadIndex( logLine ) + doc.GetLineLength( logLine );
				logLineEndPos = GetVirPosFromIndex( logLineEnd );

				// make a rectangle that covers the logical line area
				invalidRect2.X = 0;
				invalidRect2.Y = invalidRect1.Bottom;
				invalidRect2.Width = VisibleSize.Width;
				invalidRect2.Height = logLineEndPos.Y + LineSpacing - invalidRect2.Top;
			}

			// invalidate the range
			Invalidate( invalidRect1 );
			if( 0 < invalidRect2.Height )
			{
				Invalidate( invalidRect2 );
			}

			base.HandleContentChanged( sender, e );
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

			PLHI.Delete( PLHI.Count-1, PLHI.Count );
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

				// make drawable part of this line as a physical line
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
		void UpdatePLHI( int index, string oldText, string newText )
		{
			Debug.Assert( 0 < this.TabWidth );
			Document doc = Document;
			int delBeginL, delEndL;
			int reCalcBegin, reCalcEnd;
			int shiftBeginL;
			int diff = newText.Length - oldText.Length;

			int replaceEnd;
			int preTargetEndL;

			int firstDirtyLineIndex = LineLogic.GetLineIndexFromCharIndex( PLHI, index );
			if( firstDirtyLineIndex < 0 )
			{
				Debug.Fail( "unexpected error" );
				return;
			}

			// [phase 3] calculate range of indexes to be deleted
			delBeginL = firstDirtyLineIndex + 1;
			int lastDirtyLogLineIndex = doc.GetLineIndexFromCharIndex( index + newText.Length );
			if( lastDirtyLogLineIndex+1 < doc.LineCount )
			{
				int delEnd = doc.GetLineHeadIndex( lastDirtyLogLineIndex + 1 ) - diff;
				delEndL = LineLogic.GetLineIndexFromCharIndex( PLHI, delEnd );
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
				&& LineLogic.NextLineHead(doc.InternalBuffer, replaceEnd) == -1 )
			{
				// there exists more characters but no lines.
				shiftBeginL = Int32.MaxValue;
			}
			else
			{
				// there are following lines.
				shiftBeginL = LineLogic.GetLineIndexFromCharIndex( PLHI, reCalcEnd - diff );
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

			// [phase 2] delete LHI of affected physical lines except first one
			if( delBeginL < delEndL && delEndL <= PLHI.Count )
				PLHI.Delete( delBeginL, delEndL );

			// [phase 3] re-calculate physical line indexes
			// (here we should divide the text in the range into small segments
			// to avoid making unnecessary copy of the text so many times)
			const int segmentLen = 10;
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
					if( doc[end-1] == '\r'
						&& end < doc.Length && doc[end] == '\n' )
						end++;
					else if( Document.IsHighSurrogate(doc[end-1])
						&& end < doc.Length && Document.IsLowSurrogate(doc[end]) )
						end++;
				}
				else
				{
					end = reCalcEnd;
				}

				// get next segment
				string str = doc.GetTextInRange( begin, end );
				x = MeasureTokenEndX( str, x, TextAreaWidth, out drawableLen );

				// can this segment be written in this physical line?
				if( drawableLen < str.Length
					|| LineLogic.IsEolChar(str, drawableLen-1) )
				{
					// hit right limit. end this physical line
					PLHI.Insert( line, begin+drawableLen );
					line++;
					end = begin + drawableLen;
					x = 0;
				}
			}
			while( end < reCalcEnd );

			// then, remove extra last physical line index made as the result of phase 3
			if( line != delBeginL && line < PLHI.Count )
			{
				PLHI.Delete( line-1, line );
			}

			// remember the condition of the calculation
			Document.ViewParam.LastTextAreaWidth = TextAreaWidth;
			Document.ViewParam.LastFontHashCode = Font.GetHashCode();
			Document.ViewParam.LastModifiedTime = Document.LastModifiedTime;
		}
		#endregion

		#region Painting
		/// <summary>
		/// Paints content to a graphic device.
		/// </summary>
		/// <param name="clipRect">clipping rectangle that covers all invalidated region (in screen coord.)</param>
		public override void Paint( Rectangle clipRect )
		{
			Debug.Assert( Font != null, "invalid state; Font is null" );
			Debug.Assert( Document != null, "invalid state; Document is null" );

			int selBegin, selEnd;
			Point pos = new Point();

			// prepare off-screen buffer
#			if !DRAW_SLOWLY && !PocketPC
			_Gra.BeginPaint( clipRect );
#			endif

			// draw all lines
			for( int i=FirstVisibleLine; i<LineCount; i++ )
			{
				if( pos.Y < clipRect.Bottom && clipRect.Top <= pos.Y+LineHeight )
				{
					// reset x-coord of drawing position
					pos.X = -(ScrollPosX - TextAreaX);

					// draw this line
					DrawLine( i, ref pos, clipRect );
				}
				pos.Y += LineSpacing;
			}

			// fill area below of the text
			_Gra.BackColor = ColorScheme.BackColor;
			_Gra.FillRectangle( 0, pos.Y, VisibleSize.Width, VisibleSize.Height-pos.Y );

			// flush drawing results BEFORE updating current line highlight
			// because the highlight graphic is never limited to clipping rect
#			if !DRAW_SLOWLY && !PocketPC
			_Gra.EndPaint();
#			endif

			// draw underline to highlight current line if there is no selection
			Document.GetSelection( out selBegin, out selEnd );
			if( HighlightsCurrentLine && selBegin == selEnd )
			{
				int caretLine, caretPosY;

				// calculate position of the underline
				caretLine = LineLogic.GetLineIndexFromCharIndex( PLHI, Document.CaretIndex );
				caretPosY = caretLine * LineSpacing - (FirstVisibleLine * LineSpacing);
				
				// draw underline to current line
				DrawUnderLine( caretPosY, ColorScheme.HighlightColor );
			}
		}

		void DrawLine( int lineIndex, ref Point pos, Rectangle clipRect )
		{
			Debug.Assert( this.Font != null );
			Debug.Assert( this.Document != null );

			// note that given pos is NOT virtual position BUT screen position.
			string token;
			int lineHead, lineEnd;
			int begin, end; // range of the token in the text
			CharClass klass;
			Point tokenEndPos = pos;
			bool inSelection;

			int physTextAreaRight = TextAreaWidth + (TextAreaX - ScrollPosX);

			// calc position of head/end of this line
			lineHead = PLHI[ lineIndex ];
			if( lineIndex+1 < PLHI.Count )
				lineEnd = PLHI[ lineIndex + 1 ];
			else
				lineEnd = Document.Length;

			// adjust and set clipping rect
			if( clipRect.X < TextAreaX )
			{
				// given clip rect covers line number area.
				// redraw line nubmer and shrink clip rect
				// to avoid overwriting line number by text content

				// get logical line index from given given line head index
				int logLineIndex;
				int lineNumber;
				logLineIndex = Document.GetLineIndexFromCharIndex( lineHead );
				if( Document.GetLineHeadIndex(logLineIndex) == lineHead )
				{
					lineNumber = logLineIndex + 1;
				}
				else
				{
					// physical line head index is different from logical line head index.
					// this means this physical line was a wrapped line so do not draw foregorund.
					lineNumber = -1;
				}

				DrawLineNumber( pos.Y, lineNumber );
				clipRect.Width -= (TextAreaX - clipRect.X);
				clipRect.X = TextAreaX;
			}
			if( physTextAreaRight < clipRect.Right )
			{
				// given clip rect covers the area at right from text area.
				// fill right area and shrink clip rect
				int bottom;
				bottom = Math.Min( clipRect.Bottom, (LineCount - FirstVisibleLine)*LineSpacing );
				_Gra.BackColor = ColorScheme.LineNumberBack;
				_Gra.FillRectangle( physTextAreaRight+1, clipRect.Top, clipRect.Right-physTextAreaRight-1, bottom-clipRect.Top );
				_Gra.ForeColor = ColorScheme.LineNumberFore;
				_Gra.DrawLine( physTextAreaRight, clipRect.Top, physTextAreaRight, bottom );
				clipRect.Width = physTextAreaRight - clipRect.X;
			}
#			if !DRAW_SLOWLY
			_Gra.SetClipRect( clipRect );
#			endif

			// draw line text
			begin = lineHead;
			end = NextPaintToken( Document, begin, lineEnd, out klass, out inSelection );
			while( end <= lineEnd && end != -1 )
			{
				// get this token
				token = Document.GetTextInRange( begin, end );
				Debug.Assert( 0 < token.Length, "@View.Paint. NextPaintToken returns empty range." );

				// calc next drawing pos before drawing text
				{
					int virLeft = pos.X - (TextAreaX - ScrollPosX);
					tokenEndPos.X = MeasureTokenEndX( token, virLeft );
					tokenEndPos.X += (TextAreaX - ScrollPosX);
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
					int invisCharCount, invisWidth; // invisible char count / width
					int rightLimit = clipRect.Left - pos.X;

					// calculate how many chars will not be in the clip-rect
					invisWidth = MeasureTokenEndX( token, 0, rightLimit, out invisCharCount );
					if( invisCharCount < token.Length )
					{
						// cut extra (invisible) part of the token
						token = token.Substring( invisCharCount );

						// advance drawing position as if the cut part was actually drawn
						pos.X += invisWidth;
					}
				}

				// if the token area crosses the RIGHT boundary, cut off extra
				if( clipRect.Right < tokenEndPos.X )
				{
					int visCharCount; // visible char count
					int visPartRight;
					string peekingChar;
					int peekingCharRight = 0;

					// calculate number of chars which fits within the clip-rect
					visPartRight = MeasureTokenEndX( token, pos.X, clipRect.Right, out visCharCount );

					// (if the clip-rect's right boundary is NOT the text area's right boundary,
					// we must write one more char so that the peeking char appears at the boundary.)

					// try to get graphically peeking (drawn over the border line) char
					peekingChar = String.Empty;
					if( visCharCount+2 <= token.Length
						&& Document.IsHighSurrogate(token[visCharCount]) )
					{
						peekingChar = token.Substring( visCharCount, 2 );
					}
					else if( visCharCount+1 <= token.Length )
					{
						peekingChar = token.Substring( visCharCount, 1 );
					}

					// if there is a peeking char
					if( peekingChar != null )
					{
						peekingCharRight = MeasureTokenEndX( peekingChar, visPartRight );
					}

					// cut trailing extra
					token = token.Substring( 0, visCharCount+peekingChar.Length );
					tokenEndPos.X = (peekingCharRight != 0) ? peekingCharRight : visPartRight;

					// to terminate this loop, set token end position to invalid one
					end = Int32.MaxValue;
				}

				// draw this token
				DrawToken( token, klass, inSelection, ref pos, ref tokenEndPos, ref clipRect );

			next_token:
				// get next token
				pos.X = tokenEndPos.X;
				begin = end;
				end = NextPaintToken( Document, begin, lineEnd, out klass, out inSelection );
			}

			// fill right of the line text
			if( pos.X < clipRect.Right )
			{
				// to prevent drawing line number area,
				// make drawing pos to text area's left if the line end does not exceed left of text area
				if( pos.X < TextAreaX )
					pos.X = TextAreaX;
				_Gra.BackColor = ColorScheme.BackColor;
				_Gra.FillRectangle( pos.X, pos.Y, physTextAreaRight-pos.X, LineSpacing );
			}

#			if !DRAW_SLOWLY
			_Gra.RemoveClipRect();
#			endif
		}
		#endregion

		#region Utilities
		SplitArray<int> PLHI
		{
			get
			{
				Debug.Assert( Document != null );
				return Document.ViewParam.PLHI;
			}
		}
		#endregion
	}
}
