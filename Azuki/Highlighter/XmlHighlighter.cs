// file: XmlHighlighter.cs
// brief: Highlighter for XML.
// author: YAMAMOTO Suguru
// update: 2008-11-03
//=========================================================
using System;
using System.Collections.Generic;
using System.Text;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// A highlighter to highlight XML.
	/// </summary>
	class XmlHighlighter : IHighlighter
	{
		#region Fields
		List<Enclosure> _Enclosures = new List<Enclosure>();
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public XmlHighlighter()
		{
			Enclosure doubleQuote = new Enclosure();
			doubleQuote.opener = doubleQuote.closer = "\"";
			doubleQuote.escape = '\\';
			doubleQuote.klass = CharClass.String;
			_Enclosures.Add( doubleQuote );

			Enclosure singleQuote = new Enclosure();
			singleQuote.opener = singleQuote.closer = "'";
			singleQuote.escape = '\\';
			singleQuote.klass = CharClass.String;
			_Enclosures.Add( singleQuote );

			Enclosure comment = new Enclosure();
			comment.opener = "<!--";
			comment.closer = "-->";
			comment.klass = CharClass.Comment;
			_Enclosures.Add( comment );
		}
		#endregion

		#region Highlighting Logic
		/// <summary>
		/// Parse and highlight keywords.
		/// </summary>
		/// <param name="doc">Document to highlight.</param>
		public void Highlight( Document doc )
		{
			int begin = 0;
			int end = doc.Length;
			Highlight( doc, ref begin, ref end );
		}

		/// <summary>
		/// Parse and highlight keywords.
		/// </summary>
		/// <param name="doc">Document to highlight.</param>
		/// <param name="dirtyBegin">Index to start highlighting. On return, start index of the range to be invalidated.</param>
		/// <param name="dirtyEnd">Index to end highlighting. On return, end index of the range to be invalidated.</param>
		public void Highlight( Document doc, ref int dirtyBegin, ref int dirtyEnd )
		{
			if( dirtyBegin < 0 || doc.Length < dirtyBegin )
				throw new ArgumentOutOfRangeException( "dirtyBegin" );
			if( dirtyEnd < 0 || doc.Length < dirtyEnd )
				throw new ArgumentOutOfRangeException( "dirtyEnd" );

			char nextCh;
			int index, nextIndex;

			// get index to start highlighting
			dirtyBegin = HighlighterUtl.FindLast( doc, "<", dirtyBegin );
			if( dirtyBegin == -1 )
			{
				dirtyBegin = 0;
			}
			dirtyEnd = HighlighterUtl.Find( doc, ">", dirtyEnd, doc.Length );
			if( dirtyEnd == -1 )
			{
				dirtyEnd = doc.Length;
			}

			// seek each tags
			index = HighlighterUtl.Find( doc, "<", dirtyBegin, doc.Length );
			while( 0 <= index && index < dirtyEnd )
			{
				if( HighlighterUtl.StartsWith(doc, "<!--", index) )
				{
					// highlight enclosing part if this token begins a part
					nextIndex = TryHighlightEnclosure( doc, _Enclosures, index, dirtyEnd );
					if( index < nextIndex )
					{
						// successfully highlighted. skip to next.
						index = nextIndex;
					}
				}
				else
				{
					// set class for '<'
					doc.SetCharClass( index, CharClass.Keyword );
					index++;
					if( doc.Length <= index )
					{
						return; // reached to the end
					}

					// if next char is '?' or '/', highlight it too
					nextCh = doc[ index ];
					if( nextCh == '?' || nextCh == '/' || nextCh == '!' )
					{
						doc.SetCharClass( index, CharClass.Keyword );
						index++;
						if( doc.Length <= index )
							return; // reached to the end
					}

					// skip whitespaces
					while( Char.IsWhiteSpace(doc[index]) )
					{
						index++;
						if( doc.Length <= index )
							return; // reached to the end
					}

					// highlight element name
					nextIndex = HighlighterUtl.FindNextToken( doc, index );
					for( int i=index; i<nextIndex; i++ )
					{
						doc.SetCharClass( i, CharClass.Keyword2 );
					}
					index = nextIndex;

					// highlight attributes
					while( index < doc.Length && doc[index] != '>' )
					{
						// highlight enclosing part if this token begins a part
						nextIndex = TryHighlightEnclosure( doc, _Enclosures, index, dirtyEnd );
						if( index < nextIndex )
						{
							// successfully highlighted. skip to next.
							index = nextIndex;
							continue;
						}

						// this token is normal class; reset classes and seek to next token
						nextIndex = HighlighterUtl.FindNextToken( doc, index );
						for( int i=index; i<nextIndex; i++ )
						{
							doc.SetCharClass( i, CharClass.Keyword3 );
						}
						index = nextIndex;
					}

					// highlight '>'
					if( index < doc.Length )
					{
						doc.SetCharClass( index, CharClass.Keyword );
						if( 1 <= index && doc[index-1] == '/' )
							doc.SetCharClass( index-1, CharClass.Keyword );
					}
				}

				// seek to next tag
				index = HighlighterUtl.Find( doc, "<", index, doc.Length );
			}

			// expand invalaidation range
			if( 0 <= index && index < doc.Length )
			{
				dirtyEnd = HighlighterUtl.Find( doc, ">", index, doc.Length );
			}
		}

		/// <summary>
		/// Highlight part between a enclosing pair registered.
		/// </summary>
		/// <returns>Index of next parse point if a pair was highlighted or startIndex</returns>
		static int TryHighlightEnclosure( Document doc, List<Enclosure> pairs, int startIndex, int endIndex )
		{
			Enclosure pair;
			int closePos;

			// get pair which begins from this position
			pair = HighlighterUtl.StartsWith( doc, pairs, startIndex );
			if( pair == null )
			{
				return startIndex; // no pair begins from here.
			}

			// find closing pair
			closePos = HighlighterUtl.FindCloser( doc, pair, startIndex+pair.opener.Length, endIndex );
			if( closePos == -1 )
			{
				// not found.
				// if this is an opener without closer, highlight
				if( endIndex == doc.Length )
				{
					for( int i=startIndex; i<doc.Length; i++ )
						doc.SetCharClass( i, pair.klass );
					return doc.Length;
				}
				else
				{
					return startIndex;
				}
			}

			// highlight enclosed part
			for( int i = 0; i < closePos + pair.closer.Length - startIndex; i++ )
			{
				doc.SetCharClass( startIndex+i, pair.klass );
			}
			return closePos + pair.closer.Length;
		}
		#endregion
	}
}
