// 2009-04-20
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

		/// <summary>
		/// Loads application config file.
		/// </summary>
		public static void Load()
		{
			string str;
			Ini ini = new Ini();
			
			try
			{
				ini.Load( IniFilePath, Encoding.UTF8 );

				int fontSize = ini.Get( "Default", "FontSize", 11 );
				str = ini.Get( "Default", "Font", null );
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
