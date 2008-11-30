// file: DrawingOption.cs
// brief: Enum to describe view's option.
// author: YAMAMOTO Suguru
// update: 2008-11-01
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Option flags to specify how Azuki draws text area.
	/// </summary>
	[Flags]
	public enum DrawingOption : int
	{
		/// <summary>Draws space character.</summary>
		DrawsSpace				= 0x01,

		/// <summary>Draws full-width space character.</summary>
		DrawsFullWidthSpace		= 0x02,

		/// <summary>Draws tab character.</summary>
		DrawsTab				= 0x04,

		/// <summary>Draws EOL code.</summary>
		DrawsEol				= 0x08,

		/// <summary>Shows line number area.</summary>
		HighlightCurrentLine	= 0x10,

		/// <summary>Shows line number area.</summary>
		ShowsLineNumber			= 0x20
	}
}
