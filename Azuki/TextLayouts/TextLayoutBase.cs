using System.Drawing;

namespace Sgry.Azuki.TextLayouts
{
	abstract class TextLayoutBase : ITextLayout
	{
		public abstract int GetLineHeadIndex( int lineIndex );
		public abstract int GetLineHeadIndexFromCharIndex( int charIndex );
		public abstract LineColumnPosition GetLineColumnPosition( int charIndex );
		public abstract int GetCharIndex( LineColumnPosition lcPos );
		public abstract int GetLineCount();
	}
}
