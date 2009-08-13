// file: CharClass.cs
// brief: Indicator for class of characters.
// author: YAMAMOTO Suguru
// update: 2009-08-13
//=========================================================

namespace Sgry.Azuki
{
	/// <summary>
	/// Class of characters for associating logical meaning for each tokens.
	/// </summary>
	/// <remarks>
	/// <para>
	/// CharClass enumeration specifies the class of characters.
	/// </para>
	/// <para>
	/// The 'class of characters' here is used to classify and associate
	/// logical meanings to each tokens in document.
	/// If the document is source code of some programming language,
	/// there are several types of tokens in it; string literals, comment blocks and so on.
	/// In this case, CharClass.String should be used to mark each string literals,
	/// CharClass.Comment should be used to mark each comment blocks/lines.
	/// If tokens were properly marked by appropriate char-classes,
	/// the document is programmatically accessible
	/// so Azuki (View objects) can render each token differently.
	/// </para>
	/// <para>
	/// To classify characters, use
	/// <see cref="Sgry.Azuki.Document.SetCharClass">Document.SetCharClass</see>
	/// method.
	/// </para>
	/// </remarks>
	/// <seealso cref="Sgry.Azuki.Document.SetCharClass">Document.SetCharClass method</seealso>
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
		Heading6
	}
}
