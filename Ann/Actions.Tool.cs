using System;
using System.Windows.Forms;

namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// Set editing mode to Text mode.
		/// </summary>
		public static AnnAction SetToTextMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.TextFileType );
		};

		/// <summary>
		/// Set editing mode to LaTeX mode.
		/// </summary>
		public static AnnAction SetToLatexMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.LatexFileType );
		};

		/// <summary>
		/// Set editing mode to batch file mode.
		/// </summary>
		public static AnnAction SetToBatchFileMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.BatchFileType );
		};

		/// <summary>
		/// Set editing mode to C/C++ mode.
		/// </summary>
		public static AnnAction SetToCppMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.CppFileType );
		};

		/// <summary>
		/// Set editing mode to C# mode.
		/// </summary>
		public static AnnAction SetToCSharpMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.CSharpFileType );
		};

		/// <summary>
		/// Set editing mode to Java mode.
		/// </summary>
		public static AnnAction SetToJavaMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.JavaFileType );
		};

		/// <summary>
		/// Set editing mode to Python mode.
		/// </summary>
		public static AnnAction SetToPythonMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.PythonFileType );
		};

		/// <summary>
		/// Set editing mode to Ruby mode.
		/// </summary>
		public static AnnAction SetToRubyMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.RubyFileType );
		};

		/// <summary>
		/// Set editing mode to JavaScript mode.
		/// </summary>
		public static AnnAction SetToJavaScriptMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.JavaScriptFileType );
		};

		/// <summary>
		/// Set editing mode to Ini mode.
		/// </summary>
		public static AnnAction SetToIniMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.IniFileType );
		};

		/// <summary>
		/// Set editing mode to XML mode.
		/// </summary>
		public static AnnAction SetToXmlMode
			= delegate( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.XmlFileType );
		};
	}
}
