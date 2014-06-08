namespace Sgry.Azuki.Highlighter
{
	class JavaScriptHighlighter : KeywordHighlighter
	{
		public JavaScriptHighlighter()
		{
			AddKeywordSet( new string[] {
				"break", "case", "catch", "class", "const", "continue",
				"debugger", "default", "delete", "do", "else", "enum",
				"export", "extends", "finally", "for", "function", "if",
				"implements", "import", "in", "instanceof", "interface",
				"let", "new", "package", "private", "protected", "prototype", "public",
				"return", "static", "super", "switch", "this", "throw",
				"try", "typeof", "var", "void", "while", "with"
			}, CharClass.Keyword );

			AddKeywordSet( new string[] {
				"false", "null", "true", "undefined"
			}, CharClass.Keyword2 );

			AddEnclosure( "'", "'", CharClass.String, false, '\\' );
			AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );
			AddEnclosure( "/*", "*/", CharClass.Comment, true );
			AddLineHighlight( "//", CharClass.Comment );

			AddRegex( @"(?<!\w\s*)/([^/\\]|\\.)+/[a-z]*", true, CharClass.Regex );
		}
	}
}
