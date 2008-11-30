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
			AppConfig.Load();
			AppLogic app = new AppLogic();
			app.MainForm = new AnnForm( app );

#			if !PocketPC
			Application.EnableVisualStyles();
#			endif
			Application.Run( app.MainForm );
		}
	}
}
