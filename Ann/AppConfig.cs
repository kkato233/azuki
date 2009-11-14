// 2009-11-14
using System;
using System.Collections.Generic;
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
		
		public static FontInfo FontInfo = new FontInfo( "Courier New", 11, FontStyle.Regular );
		public static Size WindowSize = new Size( 360, 400 );
		public static bool WindowMaximized = false;
		public static bool TabPanelEnabled = false;
		public static bool DrawsEolCode = true;
		public static bool DrawsFullWidthSpace = true;
		public static bool DrawsSpace = true;
		public static bool DrawsTab = true;
		public static bool DrawsEofMark = false;
		public static bool HighlightsCurrentLine = true;
		public static bool ShowsLineNumber = true;
		public static bool ShowsHRuler = false;
		public static bool ShowsDirtBar = true;
		public static int TabWidth = 8;
		public static int LinePadding = 1;
		public static int LeftMargin = 1;
		public static int TopMargin = 1;
		public static ViewType ViewType = ViewType.Proportional;
		public static bool UsesTabForIndent = true;
		public static bool ConvertsFullWidthSpaceToSpace = true;
		public static HRulerIndicatorType HRulerIndicatorType = HRulerIndicatorType.Segment;
		public static Ini Ini = new Ini();

		/// <summary>
		/// Loads application config file.
		/// </summary>
		public static void Load()
		{
			string str;
			int width, height;
			
			try
			{
				Ini.Load( IniFilePath, Encoding.UTF8 );

				int fontSize = Ini.GetInt( "Default", "FontSize", 1, Int32.MaxValue, 10 );
				str = Ini.Get( "Default", "Font", null );
				width = Ini.GetInt( "Default", "WindowWidth", 100, Int32.MaxValue, 300 );
				height = Ini.GetInt( "Default", "WindowHeight", 100, Int32.MaxValue, 400 );

				AppConfig.FontInfo				= new FontInfo( str, fontSize, FontStyle.Regular );
				AppConfig.WindowSize			= new Size( width, height );
				AppConfig.WindowMaximized		= Ini.Get( "Default", "WindowMaximized", false );
				AppConfig.TabPanelEnabled		= Ini.Get( "Default", "TabPanelEnabled", false );
				AppConfig.DrawsEolCode			= Ini.Get( "Default", "DrawsEolCode", true );
				AppConfig.DrawsFullWidthSpace	= Ini.Get( "Default", "DrawsFullWidthSpace", true );
				AppConfig.DrawsSpace			= Ini.Get( "Default", "DrawsSpace", true );
				AppConfig.DrawsTab				= Ini.Get( "Default", "DrawsTab", true );
				AppConfig.DrawsEofMark			= Ini.Get( "Default", "DrawsEofMark", false );
				AppConfig.HighlightsCurrentLine	= Ini.Get( "Default", "HighlightsCurrentLine", true );
				AppConfig.ShowsLineNumber		= Ini.Get( "Default", "ShowsLineNumber", true );
				AppConfig.ShowsHRuler			= Ini.Get( "Default", "ShowsHRuler", false );
				AppConfig.ShowsDirtBar			= Ini.Get( "Default", "ShowsDirtBar", false );
				AppConfig.TabWidth				= Ini.GetInt( "Default", "TabWidth", 0, 100, 8 );
				AppConfig.LinePadding			= Ini.GetInt( "Default", "LinePadding", 1, 100, 1 );
				AppConfig.LeftMargin			= Ini.GetInt( "Default", "LeftMargin", 0, 100, 1 );
				AppConfig.TopMargin				= Ini.GetInt( "Default", "TopMargin", 0, 100, 1 );
				AppConfig.ViewType				= Ini.Get( "Default", "ViewType", ViewType.Proportional );
				AppConfig.UsesTabForIndent		= Ini.Get( "Default", "UsesTabForIndent", true );
				AppConfig.ConvertsFullWidthSpaceToSpace = Ini.Get( "Default", "ConvertsFullWidthSpaceToSpace", false );
				AppConfig.HRulerIndicatorType	= Ini.Get( "Default", "HRulerIndicatorType", HRulerIndicatorType.Segment );
			}
			catch
			{}
		}

		/// <summary>
		/// Saves application configuration.
		/// </summary>
		public static void Save()
		{
			try
			{
				Ini.Set( "Default", "FontSize",				AppConfig.FontInfo.Size );
				Ini.Set( "Default", "Font",					AppConfig.FontInfo.Name );
				Ini.Set( "Default", "WindowWidth",			AppConfig.WindowSize.Width );
				Ini.Set( "Default", "WindowHeight",			AppConfig.WindowSize.Height );
				Ini.Set( "Default", "WindowMaximized",		AppConfig.WindowMaximized );
				Ini.Set( "Default", "TabPanelEnabled",		AppConfig.TabPanelEnabled );
				Ini.Set( "Default", "DrawsEolCode",			AppConfig.DrawsEolCode );
				Ini.Set( "Default", "DrawsFullWidthSpace",	AppConfig.DrawsFullWidthSpace );
				Ini.Set( "Default", "DrawsSpace",			AppConfig.DrawsSpace );
				Ini.Set( "Default", "DrawsTab",				AppConfig.DrawsTab );
				Ini.Set( "Default", "DrawsEofMark",			AppConfig.DrawsEofMark );
				Ini.Set( "Default", "HighlightsCurrentLine",AppConfig.HighlightsCurrentLine );
				Ini.Set( "Default", "ShowsLineNumber",		AppConfig.ShowsLineNumber );
				Ini.Set( "Default", "ShowsHRuler",			AppConfig.ShowsHRuler );
				Ini.Set( "Default", "ShowsDirtBar",			AppConfig.ShowsDirtBar );
				Ini.Set( "Default", "TabWidth",				AppConfig.TabWidth );
				Ini.Set( "Default", "LinePadding",			AppConfig.LinePadding );
				Ini.Set( "Default", "LeftMargin",			AppConfig.LeftMargin );
				Ini.Set( "Default", "TopMargin",			AppConfig.TopMargin );
				Ini.Set( "Default", "ViewType",				AppConfig.ViewType );
				Ini.Set( "Default", "UsesTabForIndent",		AppConfig.UsesTabForIndent );
				Ini.Set( "Default", "ConvertsFullWidthSpaceToSpace", AppConfig.ConvertsFullWidthSpaceToSpace );
				Ini.Set( "Default", "HRulerIndicatorType",	AppConfig.HRulerIndicatorType );

				Ini.Save( IniFilePath, Encoding.UTF8, "\r\n" );
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
