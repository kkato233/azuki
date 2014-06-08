namespace Sgry.Azuki.Highlighter
{
	using CC = CharClass;

	class PythonHighlighter : KeywordHighlighter
	{
		public PythonHighlighter()
		{
			AddKeywordSet( new string[] {
				"and", "as", "assert", "break", "continue", "del", "elif",
				"else", "except", "finally", "for",
				"from", "global", "if", "import", "in", "lambda",
				"nonlocal", "not", "or", "pass", "raise", "return",
				"try", "while", "with" },
				CC.Keyword );

			AddKeywordSet( new string[] { "False", "None", "True" },
						   CC.Keyword2 );

			AddLineHighlight( "#", CC.Comment );

			AddRegex( @"(class)\s+(\w+)\(",
					  new CC[]{CC.Keyword, CC.Class} );

			AddRegex( @"(def)\s+(\w+)\(",
					  new CC[]{CC.Keyword, CC.Function} );

			AddEnclosure( "r\"\"\"", "\"\"\"", CC.String, true );
			AddEnclosure( "R\"\"\"", "\"\"\"", CC.String, true );
			AddEnclosure( "\"\"\"", "\"\"\"", CC.String, true );

			AddEnclosure( "r'''", "'''", CC.String, true );
			AddEnclosure( "R'''", "'''", CC.String, true );
			AddEnclosure( "'''", "'''", CC.String, true );

			AddEnclosure( "r\"", "\"", CC.String, false, '\\' );
			AddEnclosure( "R\"", "\"", CC.String, false, '\\' );
			AddEnclosure( "\"", "\"", CC.String, false, '\\' );

			AddEnclosure( "r'", "'", CC.String, false, '\\' );
			AddEnclosure( "R'", "'", CC.String, false, '\\' );
			AddEnclosure( "'", "'", CC.String, false, '\\' );
		}
	}
}
