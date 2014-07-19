using System;
using System.Drawing;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.TextLayouts
{
	interface ITextLayout
	{
		int GetLineHeadIndex( int lineIndex );
		int GetLineHeadIndexFromCharIndex( int charIndex );
		LineColumnPosition GetLineColumnPosition( int charIndex );
		int GetCharIndex( LineColumnPosition lcPos );
		int GetLineCount();
	}
}
