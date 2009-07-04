// file: LatexHighlighter.cs
// brief: Highlighter for LaTeX.
// author: YAMAMOTO Suguru
// update: 2009-06-17
//=========================================================
using System;
using Sgry.Azuki.Highlighter.Coco.Latex;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// A highlighter to highlight LaTeX.
	/// </summary>
	class LatexHighlighter : IHighlighter
	{
		Parser _Parser;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public void Highlight( Document doc )
		{
			int begin = 0;
			int end = doc.Length;
			Highlight( doc, ref begin, ref end );
		}

		/// <summary>
		/// Highlightes a LaTeX document.
		/// </summary>
		/// <param name="doc">Document to highlight.</param>
		/// <param name="dirtyBegin">Index to start highlighting. On return, start index of the range to be invalidated.</param>
		/// <param name="dirtyEnd">Index to end highlighting. On return, end index of the range to be invalidated.</param>
		public void Highlight( Document doc, ref int dirtyBegin, ref int dirtyEnd )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( dirtyBegin < 0 || doc.Length < dirtyBegin )
				throw new ArgumentOutOfRangeException( "dirtyBegin" );
			if( dirtyEnd < 0 || doc.Length < dirtyEnd )
				throw new ArgumentOutOfRangeException( "dirtyEnd" );

			// set re-highlight range
			dirtyBegin = doc.GetLineHeadIndexFromCharIndex( dirtyBegin );
			dirtyEnd = doc.Length;

			// highlight with generated parser
			_Parser = new Parser( doc, dirtyBegin );
			try
			{
				_Parser.Parse();
			}
			catch( FatalError )
			{}
		}
	}
}
