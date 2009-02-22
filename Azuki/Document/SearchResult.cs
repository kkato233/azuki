// file: SearchResult.cs
// brief: Result of a text search.
// author: YAMAMOTO Suguru
// update: 2009-02-22
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Result of a text search.
	/// </summary>
	public class SearchResult
	{
		int _Begin;
		int _End;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public SearchResult( int begin, int end )
		{
			_Begin = begin;
			_End = end;
		}

		/// <summary>
		/// Begin index of the search pattern found.
		/// </summary>
		public int Begin
		{
			get{ return _Begin; }
		}

		/// <summary>
		/// End index of the search pattern found.
		/// </summary>
		public int End
		{
			get{ return _End; }
		}
	}
}
