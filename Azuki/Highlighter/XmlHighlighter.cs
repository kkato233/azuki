// file: XmlHighlighter.cs
// brief: Highlighter for XML.
// author: YAMAMOTO Suguru
// update: 2011-07-07
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
		static readonly string DefaultWordCharSet = null;
		List<Enclosure> _Enclosures = new List<Enclosure>();
		SplitArray<int> _ReparsePoints = new SplitArray<int>( 64 );
		Enclosure _CDataEnclosure;
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets whether a highlighter hook procedure can be installed or not.
		/// </summary>
		public bool CanUseHook
		{
			get{ return false; }
		}

		/// <summary>
		/// Gets or sets highlighter hook procedure.
		/// </summary>
		/// <exception cref="System.NotSupportedException">This highlighter does not support hook procedure.</exception>
		public HighlightHook HookProc
		{
			get{ throw new NotSupportedException(); }
			set{ throw new NotSupportedException(); }
		}
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public XmlHighlighter()
		{
			Enclosure doubleQuote = new Enclosure();
			doubleQuote.opener = doubleQuote.closer = "\"";
			doubleQuote.klass = CharClass.String;
			doubleQuote.multiLine = true;
			_Enclosures.Add( doubleQuote );

			Enclosure singleQuote = new Enclosure();
			singleQuote.opener = singleQuote.closer = "'";
			singleQuote.klass = CharClass.String;
			singleQuote.multiLine = true;
			_Enclosures.Add( singleQuote );

			_CDataEnclosure = new Enclosure();
			_CDataEnclosure.opener = "<![CDATA[";
			_CDataEnclosure.closer = "]]>";
			_CDataEnclosure.klass = CharClass.CDataSection;
			_CDataEnclosure.multiLine = true;
			_Enclosures.Add( _CDataEnclosure );

			Enclosure comment = new Enclosure();
			comment.opener = "<!--";
			comment.closer = "-->";
			comment.klass = CharClass.Comment;
			comment.multiLine = true;
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

			// determine where to start highlighting
			index = HighlighterUtl.FindLeastMaximum( _ReparsePoints, dirtyBegin );
			if( 0 <= index )
			{
				dirtyBegin = _ReparsePoints[index];
			}
			else
			{
				dirtyBegin = 0;
			}

			// determine where to end highlighting
			int x = HighlighterUtl.ReparsePointMinimumDistance;
			dirtyEnd += x - (dirtyEnd % x); // next multiple of x
			if( doc.Length < dirtyEnd )
			{
				dirtyEnd = doc.Length;
			}

			// seek each tags
			index = 0;
			while( 0 <= index && index < dirtyEnd )
			{
				if( HighlighterUtl.StartsWith(doc, "<!--", index) )
				{
					HighlighterUtl.EntryReparsePoint( _ReparsePoints, index );

					// highlight enclosing part if this token begins a part
					nextIndex = TryHighlightEnclosure( doc, _Enclosures, index, dirtyEnd );
					if( index < nextIndex )
					{
						// successfully highlighted. skip to next.
						HighlighterUtl.EntryReparsePoint( _ReparsePoints, index );
						index = nextIndex;
						continue;
					}
				}

				if( HighlighterUtl.StartsWith(doc, "<![CDATA[", index) )
				{
					int closePos;

					HighlighterUtl.EntryReparsePoint( _ReparsePoints, index );

					// highlight the tag which starts this CDATA section
					for( int i=0; i<_CDataEnclosure.opener.Length; i++ )
					{
						doc.SetCharClass( index+i, CharClass.CDataSection );
					}
					index += 9;

					// highlight enclosing part if this token begins a part
					closePos = HighlighterUtl.FindCloser(
							doc, _CDataEnclosure, index, dirtyEnd
						);
					if( closePos == -1 )
					{
						// not found.
						for( int i=index; i<doc.Length; i++ )
							doc.SetCharClass( i, CharClass.Normal );
						index = doc.Length;
						return;
					}

					// highlight CDATA content as normal text
					for( int i=index; i<closePos; i++ )
					{
						doc.SetCharClass( i, CharClass.Normal );
					}
					index = closePos;

					// highlight the tag which ends CDATA section
					for( int i=index; i<index+_CDataEnclosure.closer.Length; i++ )
					{
						doc.SetCharClass( i, CharClass.CDataSection );
					}
					index += _CDataEnclosure.closer.Length;
				}

				if( doc[index] == '<' )
				{
					HighlighterUtl.EntryReparsePoint( _ReparsePoints, index );

					// set class for '<'
					doc.SetCharClass( index, CharClass.Delimiter );
					index++;
					if( doc.Length <= index )
					{
						return; // reached to the end
					}

					// if next char is '?' or '/', highlight it too
					nextCh = doc[ index ];
					if( nextCh == '?' || nextCh == '/' || nextCh == '!' )
					{
						doc.SetCharClass( index, CharClass.Delimiter );
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
					nextIndex = HighlighterUtl.FindNextToken( doc, index, DefaultWordCharSet );
					for( int i=index; i<nextIndex; i++ )
					{
						doc.SetCharClass( i, CharClass.ElementName );
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
						nextIndex = HighlighterUtl.FindNextToken( doc, index, DefaultWordCharSet );
						for( int i=index; i<nextIndex; i++ )
						{
							doc.SetCharClass( i, CharClass.Attribute );
						}
						index = nextIndex;
					}

					// highlight '>'
					if( index < doc.Length )
					{
						doc.SetCharClass( index, CharClass.Delimiter );
						if( 1 <= index && doc[index-1] == '/' )
							doc.SetCharClass( index-1, CharClass.Delimiter );
						index++;
					}
				}
				else if( doc[index] == '&' )
				{
					int seekEndIndex;
					bool wasEntity;
					CharClass klass;

					// find end position of this token
					FindEntityEnd( doc, index, out seekEndIndex, out wasEntity );
					DebugUtl.Assert( 0 <= seekEndIndex && seekEndIndex <= doc.Length );

					// highlight this token
					klass = wasEntity ? CharClass.Entity : CharClass.Normal;
					for( int i=index; i<seekEndIndex; i++ )
					{
						doc.SetCharClass( i, klass );
					}
					index = seekEndIndex;
				}
				else
				{
					// normal character.
					doc.SetCharClass( index, CharClass.Normal );
					index++;
				}
			}
		}

		static void FindEntityEnd( Document doc, int startIndex, out int endIndex, out bool wasEntity )
		{
			DebugUtl.Assert( startIndex < doc.Length );
			DebugUtl.Assert( doc[startIndex] == '&' );

			endIndex = startIndex + 1;
			while( endIndex < doc.Length )
			{
				char ch = doc[endIndex];

				if( (ch < 'A' || 'Z' < ch)
					&& (ch < 'a' || 'z' < ch)
					&& (ch < '0' || '9' < ch)
					&& (ch != '#') )
				{
					if( ch == ';' )
					{
						endIndex++;
						wasEntity = true;
						return;
					}
					else
					{
						wasEntity = false;
						return;
					}
				}

				endIndex++;
			}

			wasEntity = false;
			return;
		}

		/// <summary>
		/// Highlight part between a enclosing pair registered.
		/// </summary>
		/// <returns>Index of next parse point if a pair was highlighted or startIndex</returns>
		static int TryHighlightEnclosure( Document doc, List<Enclosure> pairs, int startIndex, int endIndex )
		{
			Enclosure pair;
			int closePos;
			int highlightEndIndex;

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

				return startIndex;
			}

			// calculate end index of the enclosed part
			highlightEndIndex = Math.Min( endIndex, closePos + pair.closer.Length );
			if( doc.Length <= highlightEndIndex )
			{
				highlightEndIndex = doc.Length - 1;
			}

			// highlight enclosed part
			for( int i=startIndex; i<highlightEndIndex; i++ )
			{
				doc.SetCharClass( i, pair.klass );
			}
			return closePos + pair.closer.Length;
		}
		#endregion
	}
}
