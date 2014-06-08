using System;
using Sgry.Azuki.Highlighter.Coco.Latex;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// A highlighter to highlight LaTeX.
	/// </summary>
	class LatexHighlighter : IHighlighter
	{
		HighlightHook _Hook = null;
		SplitArray<int> _ReparsePoints = new SplitArray<int>( 64 );

		#region Properties
		/// <summary>
		/// Gets or sets whether a highlighter hook procedure can be installed or not.
		/// </summary>
		public bool CanUseHook
		{
			get{ return true; }
		}

		/// <summary>
		/// Gets or sets highlighter hook procedure.
		/// </summary>
		/// <exception cref="System.NotSupportedException">This highlighter does not support hook procedure.</exception>
		public HighlightHook HookProc
		{
			get{ return _Hook; }
			set{ _Hook = value; }
		}
		#endregion

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
			dirtyBegin = Utl.FindReparsePoint( _ReparsePoints, dirtyBegin );
			//NO_NEED//dirtyEnd = something

			// highlight with generated parser
			Parser parser = new Parser( doc, dirtyBegin, dirtyEnd );
			parser._Hook = this._Hook;
			parser._ReparsePoints = _ReparsePoints;
			try
			{
				parser.Parse();
			}
			catch( FatalError )
			{}
		}
	}
}
