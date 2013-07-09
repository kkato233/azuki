using System;
using System.Text.RegularExpressions;

namespace Sgry.Azuki.Highlighter
{
	class DiffHighlighter : KeywordHighlighter
	{
		public DiffHighlighter()
		{
			base.HighlightsNumericLiterals = false;

			AddRegex( @"^(Index:|diff|---(?![^-]+-{3,}$)|\+\+\+|\*\*\*(?! [0-9])|===) .*", CharClass.IndexLine );
			AddRegex( @"^={4,}$", CharClass.IndexLine );
			AddRegex( @"^([0-9,]+[acd][0-9,]+|@@\s-?[0-9]|\*\*\* [0-9,]+ \*|--- [0-9,]+ -).*", CharClass.ChangeCommandLine );
			AddRegex( @"^\*+$", CharClass.ChangeCommandLine );
			AddRegex( @"^[<+](?!\+\+ ).*", CharClass.AddedLine );
			AddRegex( @"^[>-](?!-- ).*", CharClass.RemovedLine );
			AddRegex( @"^!.*$", CharClass.ChangedLine );
			AddRegex( @"^[^-+ :I][^ :]+:.*", CharClass.Comment );
		}

		public override void Highlight( Document doc,
										ref int dirtyBegin,
										ref int dirtyEnd )
		{
			base.Highlight( doc, ref dirtyBegin, ref dirtyEnd );
		}
	}
}
