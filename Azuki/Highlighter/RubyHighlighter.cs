// file: RubyHighlighter.cs
// brief: Ruby highlighter.
// author: YAMAMOTO Suguru
// update: 2008-11-03
//=========================================================
using System;
using Color = System.Drawing.Color;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Highlighter for Ruby language based on keyword matching.
	/// </summary>
	class RubyHighlighter : KeywordHighlighter
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public RubyHighlighter()
		{
			SetKeywords( new string[] {
				"alias", "and", "BEGIN", "begin", "break", "case", "class",
				"def", "defined", "do", "else", "elsif", "end", "END", "ensure",
				"false", "for", "if", "in", "module", "next", "nil", "not",
				"or", "redo", "rescue", "retry", "return", "self", "super",
				"then", "true", "undef", "unless", "until", "when", "while", "yield"
			}, CharClass.Keyword );

			AddEnclosure( "'", "'", CharClass.String, '\\' );
			AddEnclosure( "\"", "\"", CharClass.String, '\\' );
			AddEnclosure( "=begin", "=end", CharClass.DocComment, '\0' );
			AddLineHighlight( "#", CharClass.Comment );
		}
	}
}
