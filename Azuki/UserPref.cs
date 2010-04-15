// file: UserPref.cs
// brief: User preferences that affects all Azuki instances.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2010-04-15
//=========================================================
using System;
using System.Text;

namespace Sgry.Azuki
{
	/// <summary>
	/// User preferences.
	/// </summary>
	/// <remarks>
	/// <para>
	/// UserPref class is a collection of fields which customizes Azuki's behavior.
	/// All items customizable with this class affects all Azuki instances.
	/// </para>
	/// </remarks>
	public static class UserPref
	{
		static bool _CopyLineWhenNoSelection = true;
		static bool _AutoScrollNearWindowBorder = true;
		static bool _UseTextForEofMark = true;
		static Antialias _TextRenderingMode = Antialias.Default;

		/// <summary>
		/// If true, cut/copy action targets the current line if nothing selected.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If this property is set true,
		/// then copy action without any selection will copy
		/// the line itself which the caret is on.
		/// Note that this case copies &quot;a line&quot;
		/// and the copied data will be slightly different from
		/// mere text data containing all character sequence of that line.
		/// If a line was copied by this case,
		/// pasting it when the caret is at middle of a line
		/// will insert the copied line before the current line.
		/// </para>
		/// <para>
		/// This property affects both cut and copy action.
		/// </para>
		/// </remarks>
		public static bool CopyLineWhenNoSelection
		{
			get{ return _CopyLineWhenNoSelection; }
			set{ _CopyLineWhenNoSelection = value; }
		}

		/// <summary>
		/// If true, Azuki automatically scrolls when the caret goes near window border.
		/// </summary>
		/// <remarks>
		/// If true, when the caret is near window border Azuki automatically scrolls
		/// to ensure that at least one more line is visible between the caret and border.
		/// </remarks>
		public static bool AutoScrollNearWindowBorder
		{
			get{ return _AutoScrollNearWindowBorder; }
			set{ _AutoScrollNearWindowBorder = value; }
		}

		/// <summary>
		/// If true, Azuki draws EOF mark as text "[EOF]".
		/// </summary>
		public static bool UseTextForEofMark
		{
			get{ return _UseTextForEofMark; }
			set{ _UseTextForEofMark = value; }
		}

		/// <summary>
		/// Gets or sets how Azuki anti-aliases text.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property determines the anti-aliase method that Azuki uses
		/// on rendering text.
		/// </para>
		/// </remarks>
		public static Antialias Antialias
		{
			get{ return _TextRenderingMode; }
			set{ _TextRenderingMode = value; }
		}
	}
}
