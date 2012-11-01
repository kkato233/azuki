// file: Utl.cs
// brief: common utility for built-in highlighters.
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

	static class Utl
	{
		public const int ReparsePointMinimumDistance = 1024;
		delegate bool ClassifyCharProc( char ch );

		#region Utilities
		/// <summary>
		/// Highlight an enclosed part with specified patterns.
		/// </summary>
		/// <returns>
		/// Whether any characters are highlighted or not.
		/// </returns>
		public static bool TryHighlight( Document doc,
										 List<Enclosure> pairs,
										 int startIndex,
										 int endIndex,
										 out int nextParsePos )
		{
			return TryHighlight( doc, pairs, startIndex, endIndex, null, out nextParsePos );
		}

		/// <summary>
		/// Highlight an enclosed part with specified patterns.
		/// </summary>
		/// <returns>
		/// Whether any characters are highlighted or not.
		/// </returns>
		public static bool TryHighlight( Document doc,
										 List<Enclosure> pairs,
										 int startIndex,
										 int endIndex,
										 HighlightHook proc,
										 out int nextParsePos )
		{
			Debug.Assert( doc != null );
			Debug.Assert( pairs != null );
			Debug.Assert( 0 <= startIndex );
			Debug.Assert( startIndex < endIndex );

			// get pair which begins from this position
			foreach( Enclosure pair in pairs )
			{
				if( TryHighlight(doc, pair, startIndex, endIndex, proc, out nextParsePos) )
				{
					return true;
				}
			}
			nextParsePos = startIndex;
			return false;
		}

		/// <summary>
		/// Highlight an enclosed part with specified patterns.
		/// </summary>
		/// <returns>
		/// Whether any characters are highlighted or not.
		/// </returns>
		public static bool TryHighlight( Document doc,
										 Enclosure pair,
										 int startIndex,
										 int endIndex,
										 out int nextParsePos )
		{
			return TryHighlight( doc, pair, startIndex, endIndex, null, out nextParsePos );
		}

		/// <summary>
		/// Highlight an enclosed part with specified patterns.
		/// </summary>
		/// <returns>
		/// Whether any characters are highlighted or not.
		/// </returns>
		public static bool TryHighlight( Document doc,
										 Enclosure pair,
										 int startIndex,
										 int endIndex,
										 HighlightHook proc,
										 out int nextParsePos )
		{
			Debug.Assert( doc != null );
			Debug.Assert( pair != null );
			Debug.Assert( 0 <= startIndex );
			Debug.Assert( startIndex < endIndex );
			int closerPos;
			int closerEndPos;
			bool openerFound;

			// Search the closer
			closerPos = FindCloser( doc, pair, startIndex, endIndex, out openerFound );
			if( closerPos == -1 )
			{
				// Highlight all the followings if reached to the EOF
				if( openerFound )
				{
					Highlight( doc, startIndex, doc.Length, pair.klass, proc );
					nextParsePos = doc.Length;
					return true;
				}
				else
				{
					nextParsePos = startIndex;
					return false;
				}
			}

			// highlight enclosed part
			closerEndPos = (pair.closer == null) ? closerPos : closerPos + pair.closer.Length;
			Highlight( doc, startIndex, closerEndPos, pair.klass, proc );
			nextParsePos = closerEndPos;
			return true;
		}

		/// <summary>
		/// Highlight a token consisted with only digits.
		/// </summary>
		/// <returns>Index of next parse point if a pair was highlighted or 'begin' index</returns>
		public static int TryHighlightNumberToken( Document doc,
												   int startIndex,
												   int endIndex,
												   HighlightHook proc )
		{
			Debug.Assert( endIndex <= doc.Length, "param endIndex is out of range (endIndex:"+endIndex+", doc.Length:"+doc.Length+")" );
			int begin = startIndex;
			int end = begin;
			char postfixCh;
			ClassifyCharProc isalpha = delegate( char ch ) {
					return ('a'<=ch && ch<='z') || ('A'<=ch && ch<='Z');
				};
			ClassifyCharProc ishex = delegate( char ch ) {
					return ('0'<=ch && ch<='9') || ('A'<=ch && ch<='F') || ('a'<=ch && ch<='f');
				};
			ClassifyCharProc isdigitdot = delegate( char ch ) {
					return ('0'<=ch && ch<='9') || (ch=='.');
				};

			if( doc.Length <= end || doc[end] < '0' || '9' < doc[end] )
				return begin;

			// check whether this token is a hex-number literal or not
			if( begin+2 < doc.Length && doc[begin] == '0' && doc[begin+1] == 'x' ) // check begin"+2" to avoid highlight token "0x" (nothing trails)
			{
				end = begin + 2;

				// seek end of this hex-number token
				while( end < endIndex && ishex(doc[end]) )
				{
					end++;
				}
			}
			else
			{
				// seek end of this number token
				while( end < endIndex && isdigitdot(doc[end]) )
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
			if( end < endIndex && isalpha(doc[end]) )
			{
				return begin; // not a number token
			}

			// highlight this token
			Highlight( doc, begin, end, CharClass.Number, proc );

			return end;
		}

		/// <summary>
		/// Highlights characters in specified range.
		/// </summary>
		public static void Highlight( Document doc,
									  int begin,
									  int end,
									  CharClass klass,
									  HighlightHook hookProc )
		{
			Debug.Assert( doc != null );
			Debug.Assert( 0 <= begin );
			Debug.Assert( begin < end );
			Debug.Assert( end <= doc.Length );

			// Call the hook if installed
			if( hookProc != null )
			{
				string token = doc.GetTextInRange( begin, end );
				if( hookProc(doc, token, begin, klass) == true )
				{
					return; // hook did something to this token.
				}
			}

			// Highlight the range
			for( int i=begin; i<end; i++ )
			{
				doc.SetCharClass( i, klass );
			}
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
		public static int FindCloser( Document doc,
									  Enclosure pair,
									  int openerIndex,
									  int endIndex )
		{
			bool x;
			return FindCloser( doc, pair, openerIndex, endIndex, out x );
		}

		/// <summary>
		/// Returns closer pos or line-end if closer is null.
		/// </summary>
		public static int FindCloser( Document doc,
									  Enclosure pair,
									  int openerIndex,
									  int endIndex,
									  out bool openerFound )
		{
			Debug.Assert( doc != null );
			Debug.Assert( pair != null );
			Debug.Assert( pair.opener != null && 0 < pair.opener.Length );
			Debug.Assert( 0 <= openerIndex );
			Debug.Assert( endIndex <= doc.Length );

			int i;
			int openerEndIndex;

			// Check whether there is the opener at specified position
			if( StartsWith(doc, pair.opener, openerIndex) == false )
			{
				openerFound = false;
				return -1;
			}
			openerFound = true;

			// If no closer is specified, the end of line will be the end position
			openerEndIndex = openerIndex + pair.opener.Length;
			if( pair.closer == null )
			{
				int lineEndIndex = GetLineEndIndexFromCharIndex( doc, openerEndIndex );
				return Math.Min( lineEndIndex, endIndex );
			}

			// Closer never exists if the search range is shorter than the closer token
			if( endIndex < openerEndIndex + pair.closer.Length )
			{
				return -1;
			}

			// find closer
			for( i=openerEndIndex; i<endIndex; i++ )
			{
				// if a closer was found, stop here
				if( StartsWith(doc, pair.closer, i) )
				{
					// If the closer is an escape character, this might not be a closer
					// but an escape character (placed here to escape a closer).
					// Skip this token in that case.
					if( pair.closer == pair.escape.ToString()
						&& doc[i+1] == pair.escape )
					{
						i++;
						continue;
					}
					return i;
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

			return -1;
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

		public static int FindLeastMaximum( IList<int> numbers, int value )
		{
			if( numbers.Count == 0 )
			{
				return -1;
			}

			for( int i=0; i<numbers.Count; i++ )
			{
				if( value <= numbers[i] )
				{
					return i - 1; // this may return -1 but it's okay.
				}
			}

			return numbers.Count - 1;
		}

		public static void EntryReparsePoint( SplitArray<int> reparsePoints, int index )
		{
			int count;
			int leastMaximumIndex;

			// manage reparse point index list
			count = reparsePoints.Count;
			if( 0 < count )
			{
				// if there are remembered positions larger than current token's one,
				// drop them
				if( index < reparsePoints[count-1] )
				{
					leastMaximumIndex = Utl.FindLeastMaximum(
							reparsePoints, index
						);
					reparsePoints.RemoveRange( leastMaximumIndex+1, reparsePoints.Count );
				}
				// if current token is not so far from currently largest position,
				// position of current token is not worth to remember
				else if( index < reparsePoints[count-1] + ReparsePointMinimumDistance )
				{
					return;
				}
			}
			reparsePoints.Add( index );
		}
		#endregion
	}
}
