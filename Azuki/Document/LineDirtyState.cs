// file: LineDirtyState.cs
// brief: Indicator of dirty state of each lines.
// author: YAMAMOTO Suguru
// update: 2009-10-31
//=========================================================

namespace Sgry.Azuki
{
	/// <summary>
	/// State of 'dirtiness' of each logical line in document.
	/// </summary>
	public enum LineDirtyState : byte
	{
		/// <summary>
		/// The line is not modified yet.
		/// </summary>
		Clean = 0,

		/// <summary>
		/// The line was modified.
		/// </summary>
		Dirty = 1,

		/// <summary>
		/// The line was modified but it is now marked as 'not modified.'
		/// </summary>
		Cleaned = 2
	}
}
