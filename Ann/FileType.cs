// 2008-11-24
using System;
using Sgry.Azuki;
using IHighlighter = Sgry.Azuki.Highlighter.IHighlighter;
using Highlighters = Sgry.Azuki.Highlighter.Highlighters;

namespace Sgry.Ann
{
	class FileType
	{
		#region Fields
		static FileType _TextFileType = null;
		static FileType _CppFileType = null;
		static FileType _CSharpFileType = null;
		static FileType _JavaFileType = null;
		static FileType _RubyFileType = null;
		static FileType _XmlFileType = null;

		string _Name = null;
		IHighlighter _Highlighter = null;
		AutoIndentHook _AutoIndentHook = null;
		#endregion

		private FileType()
		{}

		#region Repository
		/// <summary>
		/// Text file type.
		/// </summary>
		public static FileType TextFileType
		{
			get
			{
				if( _TextFileType == null )
				{
					_TextFileType = new FileType();
					_TextFileType._Name = "Text";
				}
				return _TextFileType;
			}
		}

		/// <summary>
		/// C/C++ file type.
		/// </summary>
		public static FileType CppFileType
		{
			get
			{
				if( _CppFileType == null )
				{
					_CppFileType = new FileType();
					_CppFileType._Highlighter = Highlighters.Cpp;
					_CppFileType._AutoIndentHook = AutoIndentLogic.CHook;
					_CppFileType._Name = "C/C++";
				}
				return _CppFileType;
			}
		}

		/// <summary>
		/// C# file type.
		/// </summary>
		public static FileType CSharpFileType
		{
			get
			{
				if( _CSharpFileType == null )
				{
					_CSharpFileType = new FileType();
					_CSharpFileType._Highlighter = Highlighters.CSharp;
					_CSharpFileType._AutoIndentHook = AutoIndentLogic.CHook;
					_CSharpFileType._Name = "C#";
				}
				return _CSharpFileType;
			}
		}

		/// <summary>
		/// Java file type.
		/// </summary>
		public static FileType JavaFileType
		{
			get
			{
				if( _JavaFileType == null )
				{
					_JavaFileType = new FileType();
					_JavaFileType._Highlighter = Highlighters.Java;
					_JavaFileType._AutoIndentHook = AutoIndentLogic.CHook;
					_JavaFileType._Name = "Java";
				}
				return _JavaFileType;
			}
		}

		/// <summary>
		/// Ruby file type.
		/// </summary>
		public static FileType RubyFileType
		{
			get
			{
				if( _RubyFileType == null )
				{
					_RubyFileType = new FileType();
					_RubyFileType._Highlighter = Highlighters.Ruby;
					_RubyFileType._AutoIndentHook = AutoIndentLogic.GenericHook;
					_RubyFileType._Name = "Ruby";
				}
				return _RubyFileType;
			}
		}

		/// <summary>
		/// XML file type.
		/// </summary>
		public static FileType XmlFileType
		{
			get
			{
				if( _XmlFileType == null )
				{
					_XmlFileType = new FileType();
					_XmlFileType._Highlighter = Highlighters.Xml;
					_XmlFileType._Name = "XML";
				}
				return _XmlFileType;
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets highlighter.
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
		/// Gets mode name.
		/// </summary>
		public String Name
		{
			get{ return _Name; }
		}
		#endregion
	}
}
