using System.Drawing;

namespace Sgry.Azuki.TextLayouts
{
	abstract class TextLayoutBase : ITextLayout
	{
		public virtual Point GetVirPos( IGraphics g, int index )
		{
			return GetVirPos( g, GetLineColumnPosition(index) );
		}

		public abstract Point GetVirPos( IGraphics g, LineColumnPosition lcPos );
		public abstract int GetCharIndex( IGraphics g, Point virPos );
		public abstract int GetLineHeadIndex( int lineIndex );
		public abstract int GetLineHeadIndexFromCharIndex( int charIndex );
		public abstract LineColumnPosition GetLineColumnPosition( int charIndex );
		public abstract int GetCharIndex( LineColumnPosition lcPos );
		public abstract int GetLineCount();
	}
}
