// file: UserPref.cs
// brief: User preferences that affects all Azuki instances.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-05-03
//=========================================================
using System;
using System.Text;

namespace Sgry.Azuki
{
	/// <summary>
	/// User preference.
	/// </summary>
	public static class UserPref
	{
		static bool _CopyLineWhenNoSelection = true;
		static bool _AutoScrollNearWindowBorder = true;

		/// <summary>
		/// Whether cut/copy action targets the current line or not.
		/// </summary>
		public static bool CopyLineWhenNoSelection
		{
			get{ return _CopyLineWhenNoSelection; }
			set{ _CopyLineWhenNoSelection = value; }
		}

		/// <summary>
		/// Automatically scroll when the caret is near window border
		/// to ensure that at least one more lines is visible between the caret and border.
		/// </summary>
		public static bool AutoScrollNearWindowBorder
		{
			get { return _AutoScrollNearWindowBorder; }
			set { _AutoScrollNearWindowBorder = value; }
		}
	}
}
