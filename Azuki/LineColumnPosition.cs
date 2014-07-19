namespace Sgry.Azuki
{
	public struct LineColumnPosition
	{
		public int LineIndex;
		public int ColumnIndex;

		public LineColumnPosition( int lineIndex, int columnIndex )
		{
			LineIndex = lineIndex;
			ColumnIndex = columnIndex;
		}
	}
}
