// file: IHighlighter.cs
// brief: Interface of highlighter object for Azuki.
// author: YAMAMOTO Suguru
// update: 2008-10-13
//=========================================================
using System;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Interface of highlighter object for Azuki.
	/// </summary>
	public interface IHighlighter
	{
		/// <summary>
		/// Highlight whole document.
		/// </summary>
		/// <param name="doc">Document to highlight.</param>
		void Highlight( Document doc );

		/// <summary>
		/// Highlight document part.
		/// </summary>
		/// <param name="doc">Document to highlight.</param>
		/// <param name="dirtyBegin">Index to start highlighting. On return, start index of the range to be invalidated.</param>
		/// <param name="dirtyEnd">Index to end highlighting. On return, end index of the range to be invalidated.</param>
		void Highlight( Document doc, ref int dirtyBegin, ref int dirtyEnd );
	}
}
