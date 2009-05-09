// file: CharClass.cs
// brief: Indicator for class of characters.
// author: YAMAMOTO Suguru
// update: 2009-05-02
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

		/// <summary>Macro (C/C++, C#, TeX, ...).</summary>
		Macro,

		/// <summary>Character (C/C++, Java, ...).</summary>
		Character,

		/// <summary>Type (any).</summary>
		Type,

		/// <summary>Regular expression literal (Perl, Javascript...).</summary>
		Regex,

		/// <summary>Annotation (Java).</summary>
		Annotation,

		/// <summary>Command (TeX).</summary>
		Command,

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

		/// <summary>Delimitter.</summary>
		Delimitter,

		/// <summary>CDATA section (XML).</summary>
		CDataSection,

		/// <summary>!! DO NOT USE !!  Selected text.</summary>
		Selection	= 255
	}
}
