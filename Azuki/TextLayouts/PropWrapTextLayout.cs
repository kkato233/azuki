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

		public override int GetLineCount()
		{
			return _View.PLHI.Count;
		}
	}
}
