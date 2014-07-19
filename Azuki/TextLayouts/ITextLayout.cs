using System;
using System.Drawing;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.TextLayouts
{
	interface ITextLayout
	{
		Point GetVirPos( IGraphics g, int index );
		Point GetVirPos( IGraphics g, LineColumnPosition lcPos );
		int GetIndex( IGraphics g, Point virPos );
		int GetLineHeadIndex( int lineIndex );
		int GetLineHeadIndexFromCharIndex( int charIndex );
		LineColumnPosition GetLineColumnPosition( int charIndex );
		int GetCharIndex( LineColumnPosition lcPos );
		int GetLineCount();
	}
}
