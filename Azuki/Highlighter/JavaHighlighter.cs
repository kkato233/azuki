// file: JavaHighlighter.cs
// brief: Java highlighter.
// author: YAMAMOTO Suguru
// update: 2008-11-03
//=========================================================
using System;
using Color = System.Drawing.Color;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Highlighter for Java language based on keyword matching.
	/// </summary>
	class JavaHighlighter : KeywordHighlighter
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public JavaHighlighter()
		{
			SetKeywords( new string[] {
				"abstract", "assert", "boolean", "break", "byte",
				"case", "catch", "char", "class", "const", "continue",
				"default", "do", "double", "else", "enum", "extends",
				"final", "finally", "float", "for", "goto", "if",
				"implements", "import", "instanceof", "int", "interface",
				"long", "native", "new", "package", "private", "protected",
				"public", "return", "short", "static", "strictfp", "super",
				"switch", "synchronized", "this", "throw", "throws", "transient",
				"try", "void", "volatile", "while"
			}, CharClass.Keyword );

			AddEnclosure( "'", "'", CharClass.String, '\\' );
			AddEnclosure( "\"", "\"", CharClass.String, '\\' );
			AddEnclosure( "/**", "*/", CharClass.DocComment );
			AddEnclosure( "/*", "*/", CharClass.Comment );
			AddLineHighlight( "//", CharClass.Comment );
		}
	}
}
