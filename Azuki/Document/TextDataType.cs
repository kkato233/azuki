// file: TextDataType.cs
// brief: Enumeration type representing text data type.
// author: YAMAMOTO Suguru
// update: 2009-08-09
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Specifies type of text data.
	/// </summary>
	public enum TextDataType
	{
		/// <summary>
		/// Normal text data; a stream of characters.
		/// </summary>
		Normal,

		/// <summary>
		/// Line text data; not a stream but a line.
		/// </summary>
		Line,

		/// <summary>
		/// Rectangle text data; graphically layouted text.
		/// </summary>
		Rectangle
	}
}

