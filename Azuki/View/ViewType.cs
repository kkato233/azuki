// file: ViewType.cs
// brief: Enumeration to specify type of the text view.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2009-05-16
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Type of the text views.
	/// Each view renders text content differently
	/// and is optimized for the rendering method.
	/// </summary>
	public enum ViewType
	{
		//Fixed,

		/// <summary>
		/// View type which renders text as non-wrapped lines with proportional font.
		/// </summary>
		Proportional,

		/// <summary>
		/// View type which renders text as wrapped lines with proportional font.
		/// This type of the view is most versatile but may be *heavy* for portable devices.
		/// </summary>
		WrappedProportional
	}
}
