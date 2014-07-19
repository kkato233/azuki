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
