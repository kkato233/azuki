// 2010-07-04
using System;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.WinForms;

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
			DialogResult result;
			AzukiControl azuki = app.MainForm.Azuki;
			EventHandler previewHandler;
			DrawingOption orgDrawingOption;
			int orgTabWidth;
			FontInfo orgFontInfo;
			
			// define preview event handler
			previewHandler = delegate( object sender, EventArgs e ) {
				DrawingOptionForm dialog = (DrawingOptionForm)sender;
				azuki.TabWidth = dialog.TabWidth;
				azuki.DrawingOption = dialog.DrawingOption;
				azuki.FontInfo = dialog.FontInfo;
			};

			// backup original options
			orgDrawingOption = azuki.DrawingOption;
			orgTabWidth = azuki.TabWidth;
			orgFontInfo = azuki.FontInfo;

			using( DrawingOptionForm dialog = new DrawingOptionForm() )
			{
				// prepare dialog
				dialog.DrawingOption = azuki.DrawingOption;
				dialog.TabWidth = azuki.TabWidth;
				dialog.FontInfo = azuki.FontInfo;
				dialog.OptionChangedHandler = previewHandler;

				// show dialog
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					// restore to original
					azuki.DrawingOption = orgDrawingOption;
					azuki.TabWidth = orgTabWidth;
					azuki.FontInfo = orgFontInfo;
					return;
				}
				
				// apply result
				azuki.DrawingOption = dialog.DrawingOption;
				azuki.TabWidth = dialog.TabWidth;
				azuki.FontInfo = dialog.FontInfo;
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
				app.ApplyNewTextAreaWidth();
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
