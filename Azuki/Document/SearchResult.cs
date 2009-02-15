// file: SearchResult.cs
// brief: Result of a text search.
// author: YAMAMOTO Suguru
// update: 2009-02-15
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
		/// Begin index of the first occurence of the search pattern.
		/// </summary>
		public int Begin
		{
			get{ return _Begin; }
		}

		/// <summary>
		/// End index of the first occurence of the search pattern.
		/// </summary>
		public int End
		{
			get{ return _End; }
			set{ _End = value; }
		}
	}
}
