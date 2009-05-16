// 2009-05-16
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Windows.Forms;
using Path = System.IO.Path;

namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// Shows current memory usage.
		/// </summary>
		public static AnnAction ShowMemoryUsage
			= delegate( AppLogic app )
		{
			StringBuilder message = new StringBuilder( 256 );

			// make message (getting total memory also invokes GC)
			message.AppendFormat( null, "Total memory: {0} KB\n", GC.GetTotalMemory(true)/1024 );
			message.AppendFormat( null, "----\n" );
			foreach( Document doc in app.Documents )
			{
				message.AppendFormat( null, "  {0}: {1} KB\n", doc.DisplayName, doc.AzukiDoc.MemoryUsage/1024 );
			}

			// show usage
			MessageBox.Show( message.ToString(), "Estimated memory usage" );
		};

		/// <summary>
		/// Shows the "About" dialog.
		/// </summary>
		public static AnnAction ShowAboutDialog
			= delegate( AppLogic app )
		{
			const string regex_pattern = @"([^,]+)[^V]*Version=([0-9]\.[0-9]\.[0-9])";
			Match match;
			string annAsmFullName;
			string annAsmName;
			string annVersion;
			string azukiAsmFullName;
			string azukiAsmName;
			string azukiVersion;
			string message;

			// get "full name" of the assemblies
			try
			{
				annAsmFullName = typeof(Sgry.Ann.Document).Assembly.FullName;
				azukiAsmFullName = typeof(Sgry.Azuki.Document).Assembly.FullName;
				
				// extract file name and version of Ann
				match = Regex.Match( annAsmFullName, regex_pattern );
				annAsmName = match.Groups[1].Value;
				annVersion = match.Groups[2].Value;

				// extract file name and version of Azuki
				match = Regex.Match( azukiAsmFullName, regex_pattern );
				azukiAsmName = match.Groups[1].Value;
				azukiVersion = match.Groups[2].Value;

				message = String.Format( "{0} version {1}\n(with {2} {3})", annAsmName, annVersion, azukiAsmName, azukiVersion );
				MessageBox.Show( message, annAsmName );
			}
			catch( Exception ex )
			{
				MessageBox.Show( "failed to get assembly versions!\n" + ex.ToString(), "Ann bug",
					MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1 );
			}
		};
	}
}
