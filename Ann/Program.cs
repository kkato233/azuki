using System;
using System.Collections.Generic;
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
			List<string> initOpenFilePaths = new List<string>();

			// get mutex object to control application instance
			using( mutex = new MyMutex(true, AppLogic.AppInstanceMutexName) )
			{
				owned = mutex.WaitOne( 0 );

				// parse arguments
				for( int i=0; i<args.Length; i++ )
				{
					initOpenFilePaths.Add( args[i] );
				}

				// if another instance already exists, activate it and exit
				if( owned )
				{
					Main_LaunchFirstInstance( initOpenFilePaths.ToArray() );
					mutex.ReleaseMutex();
				}
				else
				{
					Main_ActivateFirstInstance( initOpenFilePaths.ToArray() );
				}
			}
		}

		static void Main_LaunchFirstInstance( string[] initOpenFilePaths )
		{
			AppLogic app;

			// launch new application instance
			using( app = new AppLogic(initOpenFilePaths) )
			{
				app.MainForm = new AnnForm( app );
				app.LoadConfig( true );

#				if !PocketPC
				Application.EnableVisualStyles();
#				endif
				Application.Run( app.MainForm );
			}
		}

		static void Main_ActivateFirstInstance( string[] initOpenFilePaths )
		{
			PseudoPipe pipe = new PseudoPipe();

			// write IPC file to tell existing instance what user wants to do
			try
			{
				pipe.Connect( AppLogic.IpcFilePath );
				pipe.WriteLine( "Activate", 1000 );
				foreach( string path in initOpenFilePaths )
				{
					pipe.WriteLine( "OpenDocument," + path, 1000 );
				}
			}
			catch
			{}
			finally
			{
				pipe.Dispose();
			}
		}
	}
}
