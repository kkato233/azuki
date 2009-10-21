// file: DrawingOption.cs
// brief: Enum to describe view's option.
// author: YAMAMOTO Suguru
// update: 2009-10-21
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

		/// <summary>Draws EOL (End Of Line) code.</summary>
		DrawsEol				= 0x08,

		/// <summary>Shows line number area.</summary>
		HighlightCurrentLine	= 0x10,

		/// <summary>Shows line number area.</summary>
		ShowsLineNumber			= 0x20,

		/// <summary>Shows horizontal ruler.</summary>
		ShowsHRuler				= 0x40,

		/// <summary>Draws EOF (End Of File) mark.</summary>
		DrawsEof				= 0x80
	}
}
