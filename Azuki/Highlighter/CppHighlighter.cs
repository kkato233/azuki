// file: CppHighlighter.cs
// brief: C/C++ highlighter.
// author: YAMAMOTO Suguru
// update: 2009-01-12
//=========================================================
using System;
using Color = System.Drawing.Color;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Highlighter for C/C++ language based on keyword matching.
	/// </summary>
	class CppHighlighter : KeywordHighlighter
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public CppHighlighter()
		{
			SetKeywords( new string[] {
				"asm", "auto", "bool", "break", "case", "catch", "char",
				"class", "const", "const_cast", "continue", "default",
				"delete", "do", "double", "dynamic_cast", "else",
				"enum", "explicit", "export", "extern", "false",
				"float", "for", "friend", "goto", "if", "inline",
				"int", "long", "mutable", "namespace", "new",
				"operator", "private", "protected", "public", "register",
				"reinterpret_cast", "return", "short",
				"signed", "sizeof", "static", "static_cast",
				"struct", "switch", "template", "this", "throw", "true", "try",
				"typedef", "typeid", "typename", "union", "unsigned", "using",
				"virtual", "void", "volatile", "while"
			}, CharClass.Keyword );

			SetKeywords( new string[] {
				"NULL", "offsetof", "ptrdiff_t", "size_t", "wchar_t"
			}, CharClass.Keyword2 );

			SetKeywords( new string[] {
				"BOOL", "BYTE", "DWORD", "DWORD_PTR", "FALSE", "HANDLE", "HRESULT", "HWND",
				"INT_PTR", "LONG_PTR", "LPARAM", "LRESULT", "NULL", "TCHAR", "TRUE",
				"WORD", "WPARAM"
			}, CharClass.Keyword3 );

			SetKeywords( new string[] {
				"__FILE__", "__LINE__",
				"#define", "#elif", "#else", "#endif", "#error",
				"#if", "#ifdef", "#ifndef", "#include", "#import",
				"#line", "#pragma", "#undef"
			}, CharClass.Macro );

			AddEnclosure( "'", "'", CharClass.String, '\\' );
			AddEnclosure( "\"", "\"", CharClass.String, '\\' );
			AddEnclosure( "/*", "*/", CharClass.Comment );
			AddLineHighlight( "//", CharClass.Comment );
		}
	}
}
