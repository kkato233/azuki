// 2008-10-26
using System;
using System.Windows.Forms;

namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// Activate next document.
		/// </summary>
		public static AnnAction ActivateNextDocument
			= delegate( AppLogic app )
		{
			app.ActivateNextDocument();
		};

		/// <summary>
		/// Activate previous document.
		/// </summary>
		public static AnnAction ActivatePrevDocument
			= delegate( AppLogic app )
		{
			app.ActivatePrevDocument();
		};

		/// <summary>
		/// Shows a dialog listing documents.
		/// </summary>
		public static AnnAction ShowDocumentList
			= delegate( AppLogic app )
		{
			app.ShowDocumentList();
		};
	}
}
