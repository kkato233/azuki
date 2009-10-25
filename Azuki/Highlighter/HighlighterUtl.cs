// file: HighlighterUtl.cs
// brief: common utility for built-in highlighters.
// author: YAMAMOTO Suguru
// update: 2009-10-24
//=========================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Class which expresses an enclosing pair like '[' and ']'.
	/// </summary>
	class Enclosure
	{
		/// <summary>Token to open the enclosing pair.</summary>
		public string opener;

		/// <summary>Token to close the enclosing pair.</summary>
		public string closer;
		
		/// <summary>Char-class to be set for chars in the range of enclosing pair.</summary>
		public CharClass klass;
		
		/// <summary>Escape char used in the enclosing pair.</summary>
		public char escape;

		/// <summary>Whether this enclosure must exist in a line or not.</summary>
		public bool multiLine;

#		if DEBUG
		public override string ToString()
		{
			return opener + "..." + closer;
		}
#		endif
	}

	static class HighlighterUtl
	{
		#region Utilities
		/// <summary>
		/// Highlight a token consisted with only digits.
		/// </summary>
		/// <returns>Index of next parse point if a pair was highlighted or 'begin' index</returns>
		public static int TryHighlightNumberToken( Document doc, int startIndex, int endIndex )
		{
			Debug.Assert( endIndex <= doc.Length, "param endIndex is out of range (endIndex:"+endIndex+", doc.Length:"+doc.Length+")" );
			int begin = startIndex;
			int end = begin;
			char postfixCh;

			if( doc.Length <= end || doc[end] < '0' || '9' < doc[end] )
				return begin;

			// check whether this token is a hex-number literal or not
			if( begin+2 < doc.Length && doc[begin] == '0' && doc[begin+1] == 'x' ) // check begin"+2" to avoid highlight token "0x" (nothing trails)
			{
				end = begin + 2;

				// seek end of this hex-number token
				while( end < endIndex && Utl.IsHexDigitChar(doc[end]) )
				{
					end++;
				}
			}
			else
			{
				// seek end of this number token
				while( end < endIndex && Utl.IsDigitOrDot(doc[end]) )
				{
					end++;
				}

				// if next char is one of the alphabets in 'f', 'i', 'j', 'l',
				// treat it as a post-fix.
				if( end < endIndex )
				{
					postfixCh = Char.ToLower( doc[end] );
					if( postfixCh == 'f' || postfixCh == 'i' || postfixCh == 'j' || postfixCh == 'l' )
					{
						end++;
					}
				}
			}
			
			// ensure this token ends with NOT an alphabet
			if( end < endIndex && Utl.IsAlphabet(doc[end]) )
			{
				return begin; // not a number token
			}

			// highlight this token
			for( int i=begin; i<end; i++ )
			{
				doc.SetCharClass( i, CharClass.Number );
			}

			return end;
		}

		/// <summary>
		/// Find next token beginning position and return it's index.
		/// </summary>
		public static int FindNextToken( Document doc, int index, string wordCharSet )
		{
			Debug.Assert( doc != null );

			if( doc.Length <= index+1 )
				return doc.Length;

			if( IsWordChar(wordCharSet, doc[index]) )
			{
				do
				{
					index++;
					if( doc.Length <= index )
						return doc.Length;
				}
				while( IsWordChar(wordCharSet, doc[index]) );
			}
			else
			{
				index++;
			}
			
			return index;
		}

		/// <summary>
		/// Find previous token beginning position and return it's index.
		/// </summary>
		public static int FindPrevToken( Document doc, int index )
		{
			return WordLogic.PrevWordStartForMove( doc, index );
		}

		/// <summary>
		/// Find token.
		/// </summary>
		public static int Find( Document doc, string token, int startIndex, int endIndex )
		{
			Debug.Assert( doc != null && token != null );
			Debug.Assert( 0 <= startIndex && startIndex <= doc.Length );
			Debug.Assert( 0 <= endIndex && startIndex <= endIndex );

			for( int i=startIndex; i<endIndex; i++ )
			{
				int j = 0;
				for( ; j<token.Length && i+j<doc.Length; j++ )
				{
					if( doc[i+j] != token[j] )
					{
						break; // go to next position
					}
				}
				if( j == token.Length )
				{
					// found.
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Finds token backward.
		/// </summary>
		public static int FindLast( Document doc, string token, int startIndex )
		{
			Debug.Assert( doc != null && token != null );
			Debug.Assert( 0 <= startIndex );
			Debug.Assert( (doc.Length == 0 && startIndex == 0) || startIndex < doc.Length );

			for( int i=startIndex; 0<=i; i-- )
			{
				int j = 0;
				for( ; j<token.Length && i+j < doc.Length; j++ )
				{
					if( doc[i+j] != token[j] )
					{
						break; // go to next position
					}
				}
				if( j == token.Length )
				{
					// found.
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Returns closer pos or line-end if closer is null.
		/// </summary>
		public static int FindCloser( Document doc, Enclosure pair, int startIndex, int endIndex )
		{
			Debug.Assert( doc != null );
			Debug.Assert( pair != null );
			Debug.Assert( pair.opener != null && 0 < pair.opener.Length );
			Debug.Assert( 1 <= startIndex );
			Debug.Assert( endIndex <= doc.Length );
			int i;
			int lineEndIndex;

			// calculate line-end index
			lineEndIndex = GetLineEndIndexFromCharIndex( doc, startIndex );
			if( pair.closer == null )
			{
				// if closer is not specified, the line end is the end position
				return lineEndIndex;
			}

			// find closer
			for( i=startIndex; i<endIndex; i++ )
			{
				// if a closer was found, stop here
				if( StartsWith(doc, pair.closer, i) )
				{
					// but, if closer is exactly escape character it self,
					// this may be not closer but escape character to escape a closer.
					// if that case, skip this
					if( pair.closer == pair.escape.ToString()
						&& doc[i+1] == pair.escape )
					{
						i++;
						continue;
					}
					break;
				}

				// if an escape char was found, skip this and following escaped character(s).
				if( doc[i] == pair.escape )
				{
					if( i+2 < endIndex && doc[i+1] == '\r' && doc[i+2] == '\n' )
					{
						i++;
					}
					i++;
					continue;
				}

				// if an EOL char was found and it is single-line enclosure, stop here
				if( pair.multiLine == false && LineLogic.IsEolChar(doc[i]) )
				{
					i--;
					break;
				}
			}
			if( i == endIndex )
			{
				i = endIndex - 1;
			}

			return i;
		}

		/// <summary>
		/// Gets index of the end position of the line containing given index.
		/// </summary>
		public static int GetLineEndIndexFromCharIndex( Document doc, int index )
		{
			int lineIndex = doc.GetLineIndexFromCharIndex( index );
			if( lineIndex+1 < doc.LineCount )
				return doc.GetLineHeadIndex( lineIndex+1 );
			else
				return doc.Length;
		}

		/// <summary>
		/// Determine whether the token starts with given index in the document.
		/// </summary>
		public static bool StartsWith( Document doc, string token, int index )
		{
			int i = 0;

			for( ; i<token.Length && index+i<doc.Length; i++ )
			{
				if( token[i] != doc[index+i] )
					return false;
			}

			if( i == token.Length )
				return true;
			else
				return false;
		}

		/// <summary>
		/// Determine whether the enclosing pair starts with given index in the document.
		/// </summary>
		public static Enclosure StartsWith( Document doc, List<Enclosure> pairs, int index )
		{
			foreach( Enclosure pair in pairs )
			{
				if( StartsWith(doc, pair.opener, index) )
					return pair;
			}
			return null;
		}

		public static bool IsWordChar( string wordChars, char ch )
		{
			if( wordChars == null )
			{
				//--- use default word character set ---
				if( 'a' <= ch && ch <= 'z' )
					return true;
				if( 'A' <= ch && ch <= 'Z' )
					return true;
				if( '0' <= ch && ch <= '9' )
					return true;
				if( ch == '_' )
					return true;

				return false;
			}
			else
			{
				//--- use custom word character set ---
				int index = Array.BinarySearch<char>( wordChars.ToCharArray(), ch );
				return (0 <= index && index < wordChars.Length);
			}
		}

		static class Utl
		{
			public static bool IsAlphabet( char ch )
			{
				if( 'a' <= ch && ch <= 'z' )
					return true;
				if( 'A' <= ch && ch <= 'Z' )
					return true;

				return false;
			}

			public static bool IsDigitOrDot( char ch )
			{
				if( '0' <= ch && ch <= '9' )
					return true;
				if( ch == '.' )
					return true;

				return false;
			}

			public static bool IsHexDigitChar( char ch )
			{
				if( '0' <= ch && ch <= '9' )
					return true;
				if( 'A' <= ch && ch <= 'F' )
					return true;
				if( 'a' <= ch && ch <= 'f' )
					return true;

				return false;
			}
		}
		#endregion
	}
}
