// 2009-11-14
using System;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;

namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// Shows a dialog to select visibility of each special chars.
		/// </summary>
		public static AnnAction SelectSpecialCharVisibility
			= delegate( AppLogic app )
		{
			DrawingOptionForm dialog;
			DialogResult result;
			AzukiControl azuki = app.MainForm.Azuki;

			using( dialog = new DrawingOptionForm() )
			{
				dialog.DrawingOption = azuki.DrawingOption;
				dialog.TabWidth = azuki.TabWidth;
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					return;
				}
				azuki.TabWidth = dialog.TabWidth;
				azuki.DrawingOption = dialog.DrawingOption;
			}
		};

		/// <summary>
		/// Toggles whether lines should be drawn wrapped or not.
		/// </summary>
		public static AnnAction ToggleWrapLines
			= delegate( AppLogic app )
		{
			AzukiControl azuki = app.MainForm.Azuki;
			if( azuki.ViewType == ViewType.Proportional )
			{
				azuki.ViewType = ViewType.WrappedProportional;
				azuki.ViewWidth = azuki.ClientSize.Width;
			}
			else
			{
				azuki.ViewType = ViewType.Proportional;
			}
			app.MainForm.UpdateUI(); // update check state of menu item
		};

		/// <summary>
		/// Toggles whether tab panel is enabled or not.
		/// </summary>
		public static AnnAction ToggleTabPanel
			= delegate( AppLogic app )
		{
			app.MainForm.TabPanelEnabled = !( app.MainForm.TabPanelEnabled );
		};
	}
}
