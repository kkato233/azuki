using System;
using System.Text.RegularExpressions;

namespace Sgry.Azuki.Highlighter
{
	class IniHighlighter : KeywordHighlighter
	{
		public IniHighlighter()
		{
			AddRegex( @"^\s*(\[[^\]]+\])",
					  false,
					  new CharClass[]{ CharClass.Heading1 } );
			AddRegex( @"^\s*([^=]+)\s*[=:]",
					  false,
					  new CharClass[]{ CharClass.Property } );
			AddRegex( @"^\s*([;#!].*)",
					  false,
					  new CharClass[]{ CharClass.Comment } );
			HighlightsNumericLiterals = false;
		}
	}
}
