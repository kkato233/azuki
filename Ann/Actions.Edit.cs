// 2009-02-01
using System;
using System.Windows.Forms;

namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// Cuts currently selected text or current line if nothing selected.
		/// </summary>
		public static AnnAction Cut
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.Cut();
		};

		/// <summary>
		/// Copies currently selected text or current line if nothing selected.
		/// </summary>
		public static AnnAction Copy
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.Copy();
		};

		/// <summary>
		/// Pastes clipboard text and replace to currently selected text.
		/// </summary>
		public static AnnAction Paste
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.Paste();
		};

		/// <summary>
		/// Shows find dialog.
		/// </summary>
		public static AnnAction Find
			= delegate( AppLogic app )
		{
			app.MainForm.ActivateSearchPanel();
		};

		/// <summary>
		/// Finds next matching pattern.
		/// </summary>
		public static AnnAction FindNext
			= delegate( AppLogic app )
		{
			app.FindNext();
		};

		/// <summary>
		/// Finds previous matching pattern.
		/// </summary>
		public static AnnAction FindPrev
			= delegate( AppLogic app )
		{
			app.FindPrev();
		};

		/// <summary>
		/// Selects all text.
		/// </summary>
		public static AnnAction SelectAll
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.SelectAll();
		};
	}
}
