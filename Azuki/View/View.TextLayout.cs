using System;
using System.Drawing;

namespace Sgry.Azuki
{
	abstract partial class View : IViewInternal, IDisposable
	{
		#region Delegated methods of ITextLayout
		/// <exception cref="ArgumentOutOfRangeException"/>
		public Point GetVirPosFromIndex( int index )
		{
			using( var g = _UI.GetIGraphics() )
				return GetVirPosFromIndex( g, index );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public Point GetVirPosFromIndex( IGraphics g, int index )
		{
			return Layout.GetVirPos( g, index );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public Point GetVirPosFromIndex( int lineIndex, int columnIndex )
		{
			using( var g = _UI.GetIGraphics() )
				return GetVirPosFromIndex( g, lineIndex, columnIndex );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public Point GetVirPosFromIndex( IGraphics g, int lineIndex, int columnIndex )
		{
			return Layout.GetVirPos( g, new LineColumnPosition(lineIndex, columnIndex) );
		}

		public int GetIndexFromVirPos( Point virPos )
		{
			using( var g = _UI.GetIGraphics() )
				return GetIndexFromVirPos( g, virPos );
		}

		public int GetIndexFromVirPos( IGraphics g, Point virPos )
		{
			return Layout.GetCharIndex( g, virPos );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public int GetLineHeadIndex( int lineIndex )
		{
			return Layout.GetLineHeadIndex( lineIndex );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			return Layout.GetLineHeadIndexFromCharIndex( charIndex );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public int GetLineIndexFromCharIndex( int charIndex )
		{
			int lineIndex, columnIndex;
			GetLineColumnIndexFromCharIndex( charIndex, out lineIndex, out columnIndex );
			return lineIndex;
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public void GetLineColumnIndexFromCharIndex( int charIndex,
													 out int lineIndex, out int columnIndex )
		{
			var lcPos = Layout.GetLineColumnPosition( charIndex );
			lineIndex = lcPos.LineIndex;
			columnIndex = lcPos.ColumnIndex;
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex )
		{
			return Layout.GetCharIndex( new LineColumnPosition(lineIndex, columnIndex) );
		}
		#endregion
	}
}
