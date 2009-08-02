// 2009-08-02
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using Encoding = System.Text.Encoding;
using Sgry.Azuki;

namespace Sgry.Ann
{
	class AppConfig
	{
		static string _IniFilePath;
		
		public static Font Font = new Font( "Courier New", 11, FontStyle.Regular );
		public static Size WindowSize = new Size( 360, 400 );
		public static bool DrawsEolCode = true;
		public static bool DrawsFullWidthSpace = true;
		public static bool DrawsSpace = true;
		public static bool DrawsTab = true;
		public static bool HighlightsCurrentLine = true;
		public static bool ShowsLineNumber = true;
		public static int TabWidth = 8;
		public static ViewType ViewType = ViewType.Proportional;

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

				int fontSize = ini.GetInt( "Default", "FontSize", 8, Int32.MaxValue, 11 );
				str = ini.Get( "Default", "Font", null );
				width = ini.GetInt( "Default", "WindowWidth", 100, Int32.MaxValue, 300 );
				height = ini.GetInt( "Default", "WindowHeight", 100, Int32.MaxValue, 480 );

				AppConfig.Font					= new Font( str, fontSize, FontStyle.Regular );
				AppConfig.WindowSize			= new Size( width, height );
				AppConfig.DrawsEolCode			= ini.Get( "Default", "DrawsEolCode", true );
				AppConfig.DrawsFullWidthSpace	= ini.Get( "Default", "DrawsFullWidthSpace", true );
				AppConfig.DrawsSpace			= ini.Get( "Default", "DrawsSpace", true );
				AppConfig.DrawsTab				= ini.Get( "Default", "DrawsTab", true );
				AppConfig.HighlightsCurrentLine	= ini.Get( "Default", "HighlightsCurrentLine", true );
				AppConfig.ShowsLineNumber		= ini.Get( "Default", "ShowsLineNumber", true );
				AppConfig.TabWidth				= ini.GetInt( "Default", "TabWidth", 0, 100, 8 );
				AppConfig.ViewType				= ini.Get( "Default", "ViewType", ViewType.Proportional );
			}
			catch
			{}
		}

		/// <summary>
		/// Saves application configuration.
		/// </summary>
		public static void Save()
		{
			Ini ini = new Ini();

			try
			{
				ini.Set( "Default", "FontSize",				AppConfig.Font.Size );
				ini.Set( "Default", "Font",					AppConfig.Font.Name );
				ini.Set( "Default", "WindowWidth",			AppConfig.WindowSize.Width );
				ini.Set( "Default", "WindowHeight",			AppConfig.WindowSize.Height );
				ini.Set( "Default", "DrawsEolCode",			AppConfig.DrawsEolCode );
				ini.Set( "Default", "DrawsFullWidthSpace",	AppConfig.DrawsFullWidthSpace );
				ini.Set( "Default", "DrawsSpace",			AppConfig.DrawsSpace );
				ini.Set( "Default", "DrawsTab",				AppConfig.DrawsTab );
				ini.Set( "Default", "HighlightsCurrentLine",AppConfig.HighlightsCurrentLine );
				ini.Set( "Default", "ShowsLineNumber",		AppConfig.ShowsLineNumber );
				ini.Set( "Default", "TabWidth",				AppConfig.TabWidth );
				ini.Set( "Default", "ViewType",				AppConfig.ViewType );

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
