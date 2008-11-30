// file: DummyHighlighter.cs
// brief: Dummy highlighter which executes nothing.
// author: YAMAMOTO Suguru
// update: 2008-11-03
//=========================================================
using System;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Dummy highlighter which does nothing.
	/// </summary>
	class DummyHighlighter : IHighlighter
	{
		/// <summary>
		/// Does nothing.
		/// </summary>
		public void Highlight( Document doc )
		{}

		/// <summary>
		/// Does nothing.
		/// </summary>
		public void Highlight( Document doc, ref int dirtyBegin, ref int dirtyEnd )
		{}
	}
}
