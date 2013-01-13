using System;
using System.IO;
using System.Text;
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

		public static void ChangeEncoding_Auto( AppLogic app )
		{
			ChangeEncoding( app, null );
		}

		public static void ChangeEncoding_SJIS( AppLogic app )
		{
			ChangeEncoding( app, Encoding.GetEncoding("Shift_JIS") );
		}

		public static void ChangeEncoding_JIS( AppLogic app )
		{
			ChangeEncoding( app, Encoding.GetEncoding("iso-2022-jp") );
		}

		public static void ChangeEncoding_EUCJP( AppLogic app )
		{
			ChangeEncoding( app, Encoding.GetEncoding("EUC-JP") );
		}

		public static void ChangeEncoding_UTF8( AppLogic app )
		{
			ChangeEncoding( app, Encoding.UTF8 );
		}

		public static void ChangeEncoding_UTF16LE( AppLogic app )
		{
			ChangeEncoding( app, Encoding.Unicode );
		}

		public static void ChangeEncoding_UTF16BE( AppLogic app )
		{
			ChangeEncoding( app, Encoding.BigEndianUnicode );
		}

		/// <summary>
		/// Close active document.
		/// </summary>
		public static AnnAction CloseDocument
			= delegate( AppLogic app )
		{
			app.CloseDocument( app.ActiveDocument );
		};

		/// <summary>
		/// Toggles read-only mode on or off.
		/// </summary>
		public static AnnAction ToggleReadOnlyMode
			= delegate( AppLogic app )
		{
			// toggle read-only mode
			app.ActiveDocument.IsReadOnly = !( app.ActiveDocument.IsReadOnly );

			// update menu item's check state
			app.MainForm.UpdateUI();
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

		static void ChangeEncoding( AppLogic app, Encoding enc )
		{
			Document doc = app.ActiveDocument;

			doc.Encoding = enc;
			if( enc != null && doc.FilePath != null )
			{
				DialogResult result = app.ConfirmReloadOrJustChangeEncoding(doc, enc);
				if( result != DialogResult.Yes )
				{
					app.MainForm.UpdateUI();
					return;
				}
			}

			if( doc.IsDirty )
			{
				DialogResult result = app.AlertBeforeDiscarding( doc );
				if( result != DialogResult.Yes )
				{
					app.MainForm.UpdateUI();
					return;
				}
			}
			app.ReloadDocument( app.ActiveDocument, enc, false );
		}
	}
}
