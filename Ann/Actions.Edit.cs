// 2008-10-26
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
		/// Selects all text.
		/// </summary>
		public static AnnAction SelectAll
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.SelectAll();
		};
	}
}
