// file: CSharpHighlighter.cs
// brief: C# highlighter.
// author: YAMAMOTO Suguru
// update: 2008-11-03
//=========================================================
using System;
using Color = System.Drawing.Color;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Highlighter for C# language based on keyword matching.
	/// </summary>
	class CSharpHighlighter : KeywordHighlighter
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public CSharpHighlighter()
		{
			SetKeywords( new string[] {
				"abstract", "as", "base", "bool",
				"break", "byte", "case", "catch",
				"char", "checked", "class", "const",
				"continue", "decimal", "default", "delegate",
				"do", "double", "else", "enum", "event",
				"explicit", "extern", "false", "finally",
				"fixed", "float", "for", "foreach",
				"goto", "if", "implicit", "in",
				"int", "interface", "internal",
				"is", "lock", "long", "namespace",
				"new", "null", "object", "operator",
				"out", "override", "params", "private",
				"protected", "public", "readonly", "ref",
				"return", "sbyte", "sealed", "short",
				"sizeof", "stackalloc", "static", "string",
				"struct", "switch", "this", "throw",
				"true", "try", "typeof", "uint",
				"ulong", "unchecked", "unsafe", "ushort",
				"using", "virtual", "void", "volatile", "while"
			}, CharClass.Keyword );

			SetKeywords( new string[] {
				"#define", "#elif", "#else", "#endif",
				"#endregion", "#error", "#if", "#line",
				"#region", "#undef", "#warning"
			}, CharClass.PreProcessor );

			AddEnclosure( "'", "'", CharClass.String, '\\' );
			AddEnclosure( "@\"", "\"", CharClass.String, '\"' );
			AddEnclosure( "\"", "\"", CharClass.String, '\\' );
			AddEnclosure( "/**", "*/", CharClass.DocComment );
			AddEnclosure( "/*", "*/", CharClass.Comment );
			AddLineHighlight( "///", CharClass.DocComment );
			AddLineHighlight( "//", CharClass.Comment );
		}
	}
}
