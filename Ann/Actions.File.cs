// 2008-11-23
using System;
using System.IO;
using System.Windows.Forms;

namespace Sgry.Ann
{
	delegate void AnnAction( AppLogic app );

	static partial class Actions
	{
		#region Document
		/// <summary>
		/// Creates a new empty document.
		/// </summary>
		public static AnnAction CreateNewDocument
			= delegate( AppLogic app )
		{
			app.CreateNewDocument();
		};

		/// <summary>
		/// Shows a dialog and opens a file.
		/// </summary>
		public static AnnAction OpenDocument
			= delegate( AppLogic app )
		{
			app.OpenDocument();
		};

		/// <summary>
		/// Save document with another file name.
		/// </summary>
		public static AnnAction SaveDocumentAs
			= delegate( AppLogic app )
		{
			app.SaveDocumentAs( app.ActiveDocument );
		};

		/// <summary>
		/// Save file.
		/// </summary>
		public static AnnAction SaveDocument
			= delegate( AppLogic app )
		{
			app.SaveDocument( app.ActiveDocument );
		};

		/// <summary>
		/// Close active document.
		/// </summary>
		public static AnnAction CloseDocument
			= delegate( AppLogic app )
		{
			app.CloseDocument( app.ActiveDocument );
		};

		/// <summary>
		/// Close all documents and exit application.
		/// </summary>
		public static AnnAction Exit
			= delegate( AppLogic app )
		{
			app.MainForm.Close();
		};
		#endregion
	}
}
