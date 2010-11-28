// file: MouseCursor.cs
// brief: Type of mouse cursor's graphic.
// author: YAMAMOTO Suguru
// update: 2010-11-28
//=========================================================

namespace Sgry.Azuki
{
	/// <summary>
	/// Type of mouse cursor's graphic.
	/// </summary>
	public enum MouseCursor
	{
		/// <summary>
		/// Arrow pointing up and to the left.
		/// Typically used when none of other cursor type is apropriate to be used.
		/// </summary>
		Arrow,

		/// <summary>
		/// Graphic of capital alhpabet letter 'I'.
		/// Typical usage is expressing clicking can set caret to where the cursor is at.
		/// </summary>
		IBeam,

		/// <summary>
		/// Hand pointing up.
		/// Typical usage is expressing the cursor is on something which can react on click.
		/// </summary>
		Hand,

		/// <summary>
		/// Arrow pointing up.
		/// Typical usage is expressing moving data.
		/// </summary>
		DragAndDrop
	}
}