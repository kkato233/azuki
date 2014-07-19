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
