// 2009-03-02
using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Windows.Forms;

namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// Shows the "About" dialog.
		/// </summary>
		public static AnnAction ShowAboutDialog
			= delegate( AppLogic app )
		{
			string fullName = Assembly.GetExecutingAssembly().FullName;
			Match match = Regex.Match( fullName, @"([^,]+)[^V]*Version=([0-9]\.[0-9]\.[0-9])" );
			if( match != null && 2 < match.Groups.Count )
			{
				string appName = match.Groups[1].Value;
				string version = match.Groups[2].Value;
				string message = String.Format( "{0} version {1}\n", appName, version );
				MessageBox.Show( message, appName );
			}
		};
	}
}
