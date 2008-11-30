// 2008-11-24
using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Sgry.Ann
{
	class AppConfig
	{
		static string _IniFilePath;
		
		public static Font Font;

		/// <summary>
		/// Loads application config file.
		/// </summary>
		public static void Load()
		{
			string str;
			Ini ini = new Ini();
			
			try
			{
				ini.LoadFromFile( IniFilePath );

				int fontSize = ini.TryGetInt( "Default", "FontSize", 11 );
				str = ini.TryGetString( "Default", "Font", null );
				AppConfig.Font = new Font( str, fontSize, FontStyle.Regular );
			}
			catch
			{
				AppConfig.Font = new Font( "Courier New", 11, FontStyle.Regular );
			}
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
