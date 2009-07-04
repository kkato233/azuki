// 2009-07-04
using System;
using Sgry.Azuki;
using IHighlighter = Sgry.Azuki.Highlighter.IHighlighter;
using Highlighters = Sgry.Azuki.Highlighter.Highlighters;

namespace Sgry.Ann
{
	class FileType
	{
		#region Fields
		string _Name = null;
		IHighlighter _Highlighter = null;
		AutoIndentHook _AutoIndentHook = null;
		#endregion

		private FileType()
		{}

		#region Factory
		/// <summary>
		/// Gets a new Text file type.
		/// </summary>
		public static FileType TextFileType
		{
			get
			{
				FileType fileType = new FileType();
				fileType._AutoIndentHook = AutoIndentHooks.GenericHook;
				fileType._Name = "Text";
				return fileType;
			}
		}

		/// <summary>
		/// Gets a new LaTeX file type.
		/// </summary>
		public static FileType LatexFileType
		{
			get
			{
				FileType fileType = new FileType();
				fileType._Highlighter = Highlighters.Latex;
				fileType._AutoIndentHook = AutoIndentHooks.GenericHook;
				fileType._Name = "LaTeX";
				return fileType;
			}
		}

		/// <summary>
		/// Gets a new C/C++ file type.
		/// </summary>
		public static FileType CppFileType
		{
			get
			{
				FileType fileType = new FileType();
				fileType._Highlighter = Highlighters.Cpp;
				fileType._AutoIndentHook = AutoIndentHooks.CHook;
				fileType._Name = "C/C++";
				return fileType;
			}
		}

		/// <summary>
		/// Gets a new C# file type.
		/// </summary>
		public static FileType CSharpFileType
		{
			get
			{
				FileType fileType = new FileType();
				fileType._Highlighter = Highlighters.CSharp;
				fileType._AutoIndentHook = AutoIndentHooks.CHook;
				fileType._Name = "C#";
				return fileType;
			}
		}

		/// <summary>
		/// Gets a new Java file type.
		/// </summary>
		public static FileType JavaFileType
		{
			get
			{
				FileType fileType = new FileType();
				fileType._Highlighter = Highlighters.Java;
				fileType._AutoIndentHook = AutoIndentHooks.CHook;
				fileType._Name = "Java";
				return fileType;
			}
		}

		/// <summary>
		/// Gets a new Ruby file type.
		/// </summary>
		public static FileType RubyFileType
		{
			get
			{
				FileType fileType = new FileType();
				fileType._Highlighter = Highlighters.Ruby;
				fileType._AutoIndentHook = AutoIndentHooks.GenericHook;
				fileType._Name = "Ruby";
				return fileType;
			}
		}

		/// <summary>
		/// Gets a new XML file type.
		/// </summary>
		public static FileType XmlFileType
		{
			get
			{
				FileType fileType = new FileType();
				fileType._Highlighter = Highlighters.Xml;
				fileType._AutoIndentHook = AutoIndentHooks.GenericHook;
				fileType._Name = "XML";
				return fileType;
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets an associated highlighter object.
		/// </summary>
		public IHighlighter Highlighter
		{
			get{ return _Highlighter; }
		}

		/// <summary>
		/// Gets key-hook procedure for Azuki's auto-indent associated with this file-type.
		/// </summary>
		public AutoIndentHook AutoIndentHook
		{
			get{ return _AutoIndentHook; }
		}

		/// <summary>
		/// Gets the name of the file mode.
		/// </summary>
		public String Name
		{
			get{ return _Name; }
		}
		#endregion
	}
}
