namespace Sgry.Azuki
{
	/// <summary>
	/// Pair of a line index and a column index.
	/// </summary>
	public struct LineColumnPosition
	{
		/// <summary>
		/// Gets or sets the line index.
		/// </summary>
		public int LineIndex { get; set; }

		/// <summary>
		/// Gets or sets the column index.
		/// </summary>
		public int ColumnIndex { get; set; }

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public LineColumnPosition( int lineIndex, int columnIndex )
			: this()
		{
			LineIndex = lineIndex;
			ColumnIndex = columnIndex;
		}
	}
}
