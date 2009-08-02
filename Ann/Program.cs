// 2008-08-02
using System;
using System.Windows.Forms;

namespace Sgry.Ann
{
	class Program
	{
#		if !PocketPC
		[STAThread]
#		endif
		static void Main( string[] args )
		{
			AppLogic app;
			string initOpenFilePath = null;

			if( 1 <= args.Length )
			{
				initOpenFilePath = args[0];
			}

			app = new AppLogic( initOpenFilePath );
			app.MainForm = new AnnForm( app );
			app.LoadConfig();

#			if !PocketPC
			Application.EnableVisualStyles();
#			endif
			Application.Run( app.MainForm );
		}
	}
}
