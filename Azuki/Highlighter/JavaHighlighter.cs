// file: JavaHighlighter.cs
// brief: Java highlighter.
// author: YAMAMOTO Suguru
// update: 2009-09-27
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
			AddKeywordSet( new string[] {
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

			AddEnclosure( "'", "'", CharClass.String, false, '\\' );
			AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );
			AddEnclosure( "/**", "*/", CharClass.DocComment, true );
			AddEnclosure( "/*", "*/", CharClass.Comment );
			AddLineHighlight( "//", CharClass.Comment );
		}
	}
}
