using System;
using System.Text.RegularExpressions;

namespace Sgry.Azuki.Highlighter
{
	class IniHighlighter : KeywordHighlighter
	{
		public IniHighlighter()
		{
			AddRegex(
				new Regex(@"^\s*(\[[^\]]+\])", RegexOptions.Singleline),
				new CharClass[]{
					CharClass.Heading1			// group 1
				}
			);
			AddRegex(
				new Regex(@"^\s*([^=]+)\s*[=:]", RegexOptions.Singleline),
				new CharClass[]{
					CharClass.Property			// group 1
				}
			);
			AddRegex(
				new Regex(@"^\s*([;#!].*)", RegexOptions.Singleline),
				new CharClass[]{
					CharClass.Comment			// group 1
				}
			);
		}
	}
}
