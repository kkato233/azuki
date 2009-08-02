// 2009-08-02
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using Encoding = System.Text.Encoding;

namespace Sgry.Ann
{
	class AppConfig
	{
		static string _IniFilePath;
		
		public static Font Font;
		public static Size WindowSize;

		/// <summary>
		/// Loads application config file.
		/// </summary>
		public static void Load()
		{
			Ini ini = new Ini();
			string str;
			int width, height;
			
			try
			{
				ini.Load( IniFilePath, Encoding.UTF8 );

				int fontSize = ini.Get( "Default", "FontSize", 11 );
				str = ini.Get( "Default", "Font", null );
				AppConfig.Font = new Font( str, fontSize, FontStyle.Regular );
				width = ini.Get( "Default", "WindowWidth", 0 );
				height = ini.Get( "Default", "WindowHeight", 0 );
				AppConfig.WindowSize = new Size( width, height );
			}
			catch
			{
				AppConfig.Font = new Font( "Courier New", 11, FontStyle.Regular );
				AppConfig.WindowSize = new Size( 360, 400 );
			}
		}

		/// <summary>
		/// Saves application configuration.
		/// </summary>
		public static void Save( AppLogic app )
		{
			Ini ini = new Ini();

			try
			{
				ini.Set( "Default", "FontSize", app.MainForm.Azuki.Font.Size );
				ini.Set( "Default", "Font", app.MainForm.Azuki.Font.Name );
				ini.Set( "Default", "WindowWidth", app.MainForm.Width );
				ini.Set( "Default", "WindowHeight", app.MainForm.Height );

				ini.Save( IniFilePath, Encoding.UTF8, "\r\n" );
			}
			catch
			{}
		}

		#region Utilities
		/// <summary>
		/// Gets INI file path.
		/// </summary>
		static string IniFilePath
		{
			get
			{
				if( _IniFilePath == null )
				{
					Assembly exe = Assembly.GetExecutingAssembly();
					string exePath = exe.GetModules()[0].FullyQualifiedName;
					string exeDirPath = Path.GetDirectoryName( exePath );
					_IniFilePath = Path.Combine( exeDirPath, "Ann.ini" );
				}
				return _IniFilePath;
			}
		}
		#endregion
	}
}
