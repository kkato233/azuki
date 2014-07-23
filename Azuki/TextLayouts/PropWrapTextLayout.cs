using System;
using System.Drawing;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.TextLayouts
{
	class PropWrapTextLayout : TextLayoutBase
	{
		readonly PropWrapView _View;

		public PropWrapTextLayout( PropWrapView view )
		{
			_View = view;
		}

		public override Point GetVirPos( IGraphics g, LineColumnPosition lcPos )
		{
			if( lcPos.LineIndex < 0 )
				throw new ArgumentOutOfRangeException( "lcPos", "Specified line index is out of"
													   + "range. (value:" + lcPos.LineIndex
													   + ", line count:" + GetLineCount() + ")" );
			if( lcPos.ColumnIndex < 0 )
				throw new ArgumentOutOfRangeException( "lcPos", "Specified column index is out of"
													   + " range. (value:" + lcPos.ColumnIndex + ")" );

			Point pos = new Point();

			// set value for when the columnIndex is 0
			pos.X = 0;
			pos.Y = (lcPos.LineIndex * _View.LineSpacing) + (_View.LinePadding >> 1);

			// if the location is not the head of the line, calculate x-coord.
			if( 0 < lcPos.ColumnIndex )
			{
				// get partial content of the line which exists before the caret
				var lineBegin = TextUtil.GetLineRange( _View.Document.InternalBuffer,
													   _View.PLHI,
													   lcPos.LineIndex,
													   true ).Begin;

				// measure the characters
				pos.X = _View.MeasureTokenEndX( g,
												new TextSegment(lineBegin,
																lineBegin + lcPos.ColumnIndex),
												pos.X );
			}

			return pos;
		}

		public override int GetCharIndex( IGraphics g, Point virPos )
		{
			var doc = _View.Document;
			int lineIndex, columnIndex;
			int drawableTextLen;

			// calc line index
			lineIndex = (virPos.Y / _View.LineSpacing);
			if( lineIndex < 0 )
			{
				lineIndex = 0;
			}
			else if( GetLineCount() <= lineIndex
				&& doc.LineCount != 0 )
			{
				// the point indicates beyond the final line.
				// treat as if the final line was specified
				lineIndex = GetLineCount() - 1;
			}

			// calc column index
			columnIndex = 0;
			if( 0 < virPos.X )
			{
				string line;
				bool isWrapLine = false;

				// get content of the line
				var range = TextUtil.GetLineRange( doc.InternalBuffer,
												   _View.PLHI, lineIndex, false );
				line = doc.GetTextInRange( range.Begin, range.End );
				if( range.End+1 < doc.Length
					&& !TextUtil.IsEolChar(doc[range.End]) )
				{
					isWrapLine = true;
				}

				// calc maximum length of chars in line
				int rightLimitX = virPos.X;
				int leftPartWidth = _View.MeasureTokenEndX( g, line, 0, rightLimitX,
															out drawableTextLen );
				columnIndex = drawableTextLen;

				// if the location is nearer to the NEXT of that char,
				// we should return the index of next one.
				if( drawableTextLen < range.Length )
				{
					var nextChar = new TextSegment() {
						Begin = range.Begin + drawableTextLen,
						End = TextUtil.NextGraphemeClusterIndex(doc.InternalBuffer,
																range.Begin + drawableTextLen)
					};
					int nextCharWidth = _View.MeasureTokenEndX( g, nextChar, leftPartWidth )
										- leftPartWidth;
					if( leftPartWidth + nextCharWidth/2 < virPos.X ) // == "x of middle of next char" < "x of click in virtual text area"
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

			return TextUtil.GetCharIndexFromLineColumnIndex( doc.InternalBuffer,
															 _View.PLHI, lineIndex, columnIndex );
		}

		public override int GetLineHeadIndex( int lineIndex )
		{
			if( lineIndex < 0 || _View.PLHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid index was given"
													   + " (lineIndex:" + lineIndex + ","
													   + " LineCount:" + GetLineCount() + ")." );

			return _View.PLHI[ lineIndex ];
		}

		public override int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			if( charIndex < 0 || _View.Document.Length < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given"
													   + " (charIndex:" + charIndex + ","
													   + " document.Length:" + _View.Document.Length
													   + ")." );

			return TextUtil.GetLineHeadIndexFromCharIndex( _View.Document.InternalBuffer,
														   _View.PLHI, charIndex );
		}

		public override LineColumnPosition GetLineColumnPosition( int charIndex )
		{
			if( charIndex < 0 || _View.Document.Length < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given"
													   + " (charIndex:" + charIndex + ","
													   + " document.Length:" + _View.Document.Length
													   + ")." );

			int line, column;
			TextUtil.GetLineColumnIndexFromCharIndex( _View.Document.InternalBuffer,
													  _View.PLHI, charIndex,
													  out line, out column );
			return new LineColumnPosition( line, column );
		}

		public override int GetCharIndex( LineColumnPosition lcPos )
		{
			if( lcPos.LineIndex < 0 || _View.LineCount < lcPos.LineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid index was given"
													   + " (lineIndex:" + lcPos.LineIndex + ","
													   + " LineCount:" + _View.LineCount + ")." );
			if( lcPos.ColumnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Invalid index was given"
													   + " (columnIndex:" + lcPos.ColumnIndex
													   + ")." );

			return TextUtil.GetCharIndexFromLineColumnIndex( _View.Document.InternalBuffer,
															 _View.PLHI,
															 lcPos.LineIndex,
															 lcPos.ColumnIndex );
		}

		public override int GetLineCount()
		{
			return _View.PLHI.Count;
		}
	}
}
