// 2008-10-26
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

			AppConfig.Load();
			app = new AppLogic( initOpenFilePath );
			app.MainForm = new AnnForm( app );

#			if !PocketPC
			Application.EnableVisualStyles();
#			endif
			Application.Run( app.MainForm );
		}
	}
}
