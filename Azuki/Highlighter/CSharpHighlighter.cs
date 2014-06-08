using System;

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
			// normal keywords
			AddKeywordSet( new string[] {
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

			// Context sensitive keywords
			AddKeywordSet( new string[] {
				"add", "alias", "ascending", "async", "await", "by",
				"descending", "dynamic", "equals", "from", "get", "global",
				"group", "in", "into", "join", "let", "on", "orderby",
				"partial", "remove", "select", "set", "value", "var",
				"where", "yield"
			}, CharClass.Keyword );

			// Preprocessor macro
			string[] words = new string[] {
				"define", "elif", "else", "endif", "endregion", "error", "if",
				"line", "pragma", "region", "undef", "warning"
			};
			AddRegex( @"^\s*#\s*(" + String.Join(@"\b|", words) + @"\b)",
					  CharClass.Macro );

			AddEnclosure( "'", "'", CharClass.String, false, '\\' );
			AddEnclosure( "@\"", "\"", CharClass.String, true, '\"' );
			AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );
			AddEnclosure( "/**", "*/", CharClass.DocComment, true );
			AddEnclosure( "/*", "*/", CharClass.Comment, true );
			AddLineHighlight( "///", CharClass.DocComment );
			AddLineHighlight( "//", CharClass.Comment );
		}
	}
}
