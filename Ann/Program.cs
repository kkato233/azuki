// 2010-02-13
using System;
using System.IO;
using System.Threading;
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
			MyMutex mutex;
			bool owned;
			string initOpenFilePath = null;

			// get mutex object to control application instance
			using( mutex = new MyMutex(true, AppLogic.AppInstanceMutexName) )
			{
				owned = mutex.WaitOne( 0 );

				// parse arguments
				if( 1 <= args.Length )
				{
					initOpenFilePath = args[0];
				}

				// if another instance already exists, activate it and exit
				if( owned )
				{
					Main_LaunchFirstInstance( initOpenFilePath );
					mutex.ReleaseMutex();
				}
				else
				{
					Main_ActivateFirstInstance( initOpenFilePath );
				}
			}
		}

		static void Main_LaunchFirstInstance( string initOpenFilePath )
		{
			AppLogic app;

			// launch new application instance
			using( app = new AppLogic(initOpenFilePath) )
			{
				app.MainForm = new AnnForm( app );
				app.LoadConfig();

#				if !PocketPC
				Application.EnableVisualStyles();
#				endif
				Application.Run( app.MainForm );
			}
		}

		static void Main_ActivateFirstInstance( string initOpenFilePath )
		{
			PseudoPipe pipe = new PseudoPipe();

			// write IPC file to tell existing instance what user wants to do
			try
			{
				pipe.Connect( AppLogic.IpcFilePath );
				pipe.WriteLine( "Activate", 1000 );
				if( initOpenFilePath != null )
				{
					pipe.WriteLine( "OpenDocument,"+initOpenFilePath, 1000 );
				}
			}
			catch
			{}
			return;
		}
	}
}
