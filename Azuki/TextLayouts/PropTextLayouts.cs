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

		public override int GetLineCount()
		{
			return _View.Document.LineCount;
		}
	}
}
