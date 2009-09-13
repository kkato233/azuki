// file: HRulerIndicatorType.cs
// brief: indicator type on horizontal ruler
// author: YAMAMOTO Suguru
// update: 2009-09-13
//=========================================================
namespace Sgry.Azuki
{
	/// <summary>
	/// Type of the indicator on horizontal ruler to indicate caret position.
	/// </summary>
	public enum HRulerIndicatorType
	{
		/// <summary>
		/// Draws a bar just above the caret position, on the ruler.
		/// </summary>
		Position,

		/// <summary>
		/// Fills a segment of ruler which covers x-coordinate of the caret position.
		/// </summary>
		Segment,

		/// <summary>
		/// Fills a segment of ruler corresponded with how many characters exist at left of the caret.
		/// </summary>
		CharCount
	}
}
