using System;
using System.Drawing;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.TextLayouts
{
	class PropTextLayout : TextLayoutBase
	{
		readonly PropView _View;

		public PropTextLayout( PropView view )
		{
			_View = view;
		}

		public override Point GetVirPos( IGraphics g, LineColumnPosition lcPos )
		{
			if( lcPos.LineIndex < 0 || _View.LineCount <= lcPos.LineIndex )
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
				int begin = GetCharIndex( new LineColumnPosition(lcPos.LineIndex, 0) );
				int end = GetCharIndex( lcPos );
				pos.X = _View.MeasureTokenEndX( g, new TextSegment(begin, end), pos.X );
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
			else if( doc.LineCount <= lineIndex
				&& doc.LineCount != 0 )
			{
				// the point indicates beyond the final line.
				// treat as if the final line was specified
				lineIndex = doc.LineCount - 1;
			}

			// calc column index
			columnIndex = 0;
			if( 0 < virPos.X )
			{
				// get content of the line
				string line = doc.GetLineContent( lineIndex );

				// calc maximum length of chars in line
				int rightLimitX = virPos.X;
				int leftPartWidth = _View.MeasureTokenEndX( g, line, 0, rightLimitX,
															out drawableTextLen );
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
					int nextCharWidth = _View.MeasureTokenEndX( g,
																nextChar,
																leftPartWidth ) - leftPartWidth;
					if( leftPartWidth + nextCharWidth/2 < virPos.X ) // == "x of middle of next char" < "x of click in virtual text area"
					{
						columnIndex = drawableTextLen + 1;
						while( Document.IsNotDividableIndex(line, columnIndex) )
						{
							columnIndex++;
						}
					}
				}
			}

			return doc.GetCharIndexFromLineColumnIndex( lineIndex, columnIndex );
		}

		public override int GetLineHeadIndex( int lineIndex )
		{
			return _View.Document.GetLineHeadIndex( lineIndex );
		}

		public override int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			return _View.Document.GetLineHeadIndexFromCharIndex( charIndex );
		}

		public override LineColumnPosition GetLineColumnPosition( int charIndex )
		{
			int line, column;
			_View.Document.GetLineColumnIndexFromCharIndex( charIndex, out line, out column );
			return new LineColumnPosition( line, column );
		}

		public override int GetCharIndex( LineColumnPosition lcPos )
		{
			return _View.Document.GetCharIndexFromLineColumnIndex( lcPos.LineIndex,
																   lcPos.ColumnIndex );
		}

		public override int GetLineCount()
		{
			return _View.Document.LineCount;
		}
	}
}
