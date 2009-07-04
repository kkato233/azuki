// file: CharClass.cs
// brief: Indicator for class of characters.
// author: YAMAMOTO Suguru
// update: 2009-07-04
//=========================================================

namespace Sgry.Azuki
{
	/// <summary>
	/// Class of characters mainly for syntax highlighting.
	/// </summary>
	public enum CharClass : byte
	{
		/// <summary>Normal character.</summary>
		Normal = 0,

		/// <summary>Number literal.</summary>
		Number,

		/// <summary>String.</summary>
		String,

		/// <summary>Comment.</summary>
		Comment,

		/// <summary>Document Comment.</summary>
		DocComment,

		/// <summary>Keyword.</summary>
		Keyword,

		/// <summary>Additional keyword set.</summary>
		Keyword2,

		/// <summary>Another Additional keyword set.</summary>
		Keyword3,

		/// <summary>Macro (C/C++, C#, ...).</summary>
		Macro,

		/// <summary>Character (C/C++, Java, ...).</summary>
		Character,

		/// <summary>Type (any).</summary>
		Type,

		/// <summary>Regular expression literal (Perl, Javascript...).</summary>
		Regex,

		/// <summary>Annotation (Java).</summary>
		Annotation,

		/// <summary>Selector (CSS).</summary>
		Selecter,

		/// <summary>Property name (CSS).</summary>
		Property,

		/// <summary>Value (CSS, ...).</summary>
		Value,

		/// <summary>Element name (XML).</summary>
		ElementName,

		/// <summary>Entity (XML).</summary>
		Entity,

		/// <summary>Attribute (XML).</summary>
		Attribute,

		/// <summary>Attribute value (XML).</summary>
		AttributeValue,

		/// <summary>Embedded script block (XML).</summary>
		EmbededScript,

		/// <summary>Delimiter.</summary>
		Delimiter,

		/// <summary>CDATA section. (XML)</summary>
		CDataSection,

		/// <summary>LaTeX command. (LaTeX)</summary>
		LatexCommand,

		/// <summary>Brackets used in LaTeX. (LaTeX)</summary>
		LatexBracket,

		/// <summary>Curly brackets used in LaTeX. (LaTeX)</summary>
		LatexCurlyBracket,

		/// <summary>Equation. (LaTeX)</summary>
		LatexEquation,

		/// <summary>Heading 1 (LaTeX, Wiki, HTML).</summary>
		Heading1,

		/// <summary>Heading 2 (LaTeX, Wiki, HTML).</summary>
		Heading2,

		/// <summary>Heading 3 (LaTeX, Wiki, HTML).</summary>
		Heading3,

		/// <summary>Heading 4 (LaTeX, Wiki, HTML).</summary>
		Heading4,

		/// <summary>Heading 5 (LaTeX, Wiki, HTML).</summary>
		Heading5,

		/// <summary>Heading 6 (LaTeX, Wiki, HTML).</summary>
		Heading6,

		/// <summary>!! DO NOT USE !!  Selected text.</summary>
		Selection	= 255
	}
}
