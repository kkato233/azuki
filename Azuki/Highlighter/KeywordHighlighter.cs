// file: KeywordHighlighter.cs
// brief: Keyword based highlighter.
// author: YAMAMOTO Suguru
// update: 2009-01-12
//=========================================================
using System;
using System.Collections.Generic;
using System.Text;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// A keyword-based highlighter which can highlight
	/// matched keywords and parts being enclosed by specified pair.
	/// </summary>
	public class KeywordHighlighter : IHighlighter
	{
		#region Inner Types and Fields
		class KeywordSet
		{
			public CharTreeNode root = new CharTreeNode();
			public CharClass klass = CharClass.Normal;
		}

		class CharTreeNode
		{
			public char ch = '\0';
			public CharTreeNode sibling = null;
			public CharTreeNode child = null;
			public int depth = 0;

#			if DEBUG
			public override string ToString()
			{
				return ch.ToString();
			}
#			endif
		}

		List<KeywordSet> _Keywords = new List<KeywordSet>( 16 );
		List<Enclosure> _Enclosures = new List<Enclosure>( 2 );
		List<Enclosure> _LineHighlights = new List<Enclosure>( 2 );
#		if DEBUG
		internal
#		endif
		SplitArray<int> _EPI = new SplitArray<int>( 32, 32 );
		#endregion

		#region Highlight Settings
		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure( string openPattern, string closePattern, CharClass klass )
		{
			AddEnclosure( openPattern, closePattern, klass, '\0' );
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure( string openPattern, string closePattern, CharClass klass, char escapeChar )
		{
			Enclosure pair = new Enclosure();
			pair.opener = openPattern;
			pair.closer = closePattern;
			pair.klass = klass;
			pair.escape = escapeChar;
			_Enclosures.Add( pair );
		}

		/// <summary>
		/// Clears all registered enclosures.
		/// </summary>
		public void ClearEnclosures()
		{
			_Enclosures.Clear();
		}

		/// <summary>
		/// Adds a line-highlight entry.
		/// </summary>
		/// <param name="openPattern">Opening pattern of the line-comment.</param>
		/// <param name="klass">Class to apply to highlighted text.</param>
		public void AddLineHighlight( string openPattern, CharClass klass )
		{
			Enclosure pair;

			pair = new Enclosure();
			pair.opener = openPattern;
			pair.closer = null;
			pair.klass = klass;

			_LineHighlights.Add( pair );
		}

		/// <summary>
		/// Clears all registered line-highlight entries.
		/// </summary>
		public void ClearLineHighlight()
		{
			_LineHighlights.Clear();
		}

		/// <summary>
		/// Sets sorted array of keywords to be highlighted.
		/// </summary>
		/// <param name="keywords">Sorted array of keywords.</param>
		/// <param name="klass">Char-class to be applied to the keyword set.</param>
		/// <remarks>
		/// If array of keywords is not sorted alphabetically,
		/// highlighting will not work properly.
		/// </remarks>
		public void SetKeywords( string[] keywords, CharClass klass )
		{
			KeywordSet set = new KeywordSet();

			// sort keywords at first
			//Array.Sort<string>( keywords );

			// parse and generate keyword tree
			for( int i=0; i<keywords.Length; i++ )
			{
				if( i+1 < keywords.Length
					&& keywords[i+1].IndexOf(keywords[i]) == 0 )
				{
					AddCharNode( keywords[i]+'\0', 0, set.root, 1 );
				}
				else
				{
					AddCharNode( keywords[i], 0, set.root, 1 );
				}
			}
			set.klass = klass;

			// add to keyword list
			_Keywords.Add( set );
		}

		void AddCharNode( string keyword, int index, CharTreeNode parent, int depth )
		{
			CharTreeNode child, node;

			if( keyword.Length <= index )
				return;

			// get child
			child = parent.child;
			if( child == null )
			{
				// no child. create
				child = new CharTreeNode();
				child.ch = keyword[index];
				child.depth = depth;
				parent.child = child;
			}

			// if the child is the char, go down
			if( child.ch == keyword[index] )
			{
				AddCharNode( keyword, index+1, child, depth+1 );
				return;
			}

			// find the char from brothers
			node = child;
			while( node.sibling != null && node.sibling.ch <= keyword[index] )
			{
				// found a node having the char?
				if( node.sibling.ch == keyword[index] )
				{
					// go down
					AddCharNode( keyword, index+1, node.sibling, depth+1 );
					return;
				}

				// get next node
				node = node.sibling;
			}

			// no node having the char exists.
			// create and go down
			CharTreeNode tmp = node.sibling;
			node.sibling = new CharTreeNode();
			node.sibling.ch = keyword[index];
			node.sibling.depth = depth;
			node.sibling.sibling = tmp;
			AddCharNode( keyword, index+1, node.sibling, depth+1 );
		}

		/// <summary>
		/// Clears registered keywords.
		/// </summary>
		public void ClearKeywords()
		{
			_Keywords.Clear();
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

			int index, nextIndex;
			bool highlighted;
//			int lastChangedCharIndex = 0;

			// update EPI and get index to start highlighting
			UpdateEPI( doc, dirtyBegin, out dirtyBegin, out dirtyEnd );
dirtyEnd = doc.Length;

			// seek each chars and do pattern matching
			index = dirtyBegin;
			while( 0 <= index && index < dirtyEnd )
			{
				// highlight line-comment if this token is one
				nextIndex = TryHighlightLineComment( doc, _LineHighlights, index, dirtyEnd );
				if( index < nextIndex )
				{
					// successfully highlighted. skip to next.
					index = nextIndex;
					continue;
				}

				// highlight enclosing part if this token begins a part
				nextIndex = TryHighlightEnclosure( doc, _Enclosures, index, dirtyEnd );
				if( index < nextIndex )
				{
					// successfully highlighted. skip to next.
					index = nextIndex;
					continue;
				}

				// highlight keyword if this token is a keyword
				highlighted = TryHighlightKeyword( doc, _Keywords, index, dirtyEnd, out nextIndex );
				if( highlighted )
				{
					index = nextIndex;
					continue;
				}

				// highlight digit as number
				nextIndex = HighlighterUtl.TryHighlightNumberToken( doc, index, dirtyEnd );
				if( index < nextIndex )
				{
					index = nextIndex;
					continue;
				}
				
				// this token is normal class; reset classes and seek to next token
				nextIndex = HighlighterUtl.FindNextToken( doc, index );
				for( int i=index; i<nextIndex; i++ )
				{
					doc.SetCharClass( i, CharClass.Normal );
				}
//				lastChangedCharIndex = nextIndex-1;
				index = nextIndex;
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

		/// <summary>
		/// Highlight line comment.
		/// </summary>
		/// <returns>Index of next parse point if highlight succeeded or startIndex</returns>
		static int TryHighlightLineComment( Document doc, List<Enclosure> pairs, int startIndex, int endIndex )
		{
			int closePos;
			Enclosure pair;

			// get line comment opener
			pair = HighlighterUtl.StartsWith( doc, pairs, startIndex );
			if( pair == null )
			{
				return startIndex; // no line-comment begins from here.
			}

			// get line-end pos
			closePos = HighlighterUtl.GetLineEndIndexFromCharIndex( doc, startIndex );

			// highlight the line
			for( int i=startIndex; i<closePos; i++ )
			{
				doc.SetCharClass( i, pair.klass );
			}
			return closePos;
		}

		/// <summary>
		/// Do keyword matching in [startIndex, endIndex) through keyword char-tree.
		/// </summary>
		static bool TryHighlightKeyword( Document doc, List<KeywordSet> keywords, int startIndex, int endIndex, out int nextSeekIndex )
		{
			bool highlighted = false;

			nextSeekIndex = startIndex;
			foreach( KeywordSet set in keywords )
			{
				highlighted = TryHighlightKeyword_One( doc, set, startIndex, endIndex, out nextSeekIndex );
				if( highlighted )
				{
					break;
				}
			}

			return highlighted;
		}

		static bool TryHighlightKeyword_One( Document doc, KeywordSet set, int startIndex, int endIndex, out int nextSeekIndex )
		{
			CharTreeNode node;
			int index;

			// keyword char-tree made with "char", "if", "int", "interface", "long"
			// looks like (where * means a node with null-character):
			//
			//  *-c-h-a-r
			//    |
			//    i-f
			//    | |
			//    | n-t-*
			//    |     |
			//    |     e-r-f-a-c-e
			//    |
			//    l-o-n-g
			//
			// basic matching process:
			// - compares each chars in document to
			//   root child node, root grandchild node and so on
			// - if a node does not match, try next sibling
			//   without advancing seek point of document
			node = set.root.child;
			index = startIndex;
			while( node != null && index < endIndex )
			{
				// is this node matched to the char?
				if( node.ch == doc[index] )
				{
					// matched.
					if( Matched_Case1(doc, node, index) )
					{
						// next node is null; reached to the end of keyword.
						// highlight and exit
						Utl.Highlight( doc, index, node, set.klass );
						nextSeekIndex = index + 1;
						return true;
					}
					else if( node.child != null && node.child.ch == '\0' )
					{
						// next node is a null-char.
						if(	index+1 == doc.Length
							|| (index+1 < doc.Length && !Char.IsLetterOrDigit(doc[index+1])) )
						{
							// keyword terminated by null-char in tree was matched.
							Utl.Highlight( doc, index, node, set.klass );
							nextSeekIndex = index + 1;
							return true;
						}
						else
						{
							// there are following chars.
							// so we should continue matching process for next keyword
							node = node.child.sibling;
							index++;
						}
					}
					else
					{
						// more chars needed to be matched.
						// continue matching process
						node = node.child;
						index++;
					}
				}
				else
				{
					// not matched.
					// try next keyword.
					node = node.sibling;
				}
			}

			nextSeekIndex = index;
			return false;
		}
		#endregion

		#region Management of Enclosing Pair Indexes
		/// <summary>
		/// This method maintains enlosing pair indexes and
		/// returns range of text to be highlighted.
		/// </summary>
		void UpdateEPI( Document doc, int dirtyBegin, out int begin, out int end )
		{
			int epiIndex;
			int closePos;
			Enclosure pair;

			// calculate re-parse begin index
			epiIndex = Utl.FindLeastMaximum( _EPI, dirtyBegin );
			if( epiIndex < 0 )
			{
				epiIndex = 0;
				begin = doc.GetLineHeadIndexFromCharIndex( dirtyBegin );
			}
			else if( epiIndex % 2 == 0 )
			{
				begin = _EPI[epiIndex];
			}
			else
			{
				begin = _EPI[epiIndex];
				epiIndex++;
			}
			end = doc.Length;

			// remove deleted pair indexes in removed range
			if( epiIndex < _EPI.Count )
			{
				_EPI.Delete( epiIndex, _EPI.Count );
			}

			// find pairs
			for( int i=begin; i<end; i++ )
			{
				// ensure a pair begins from here
				pair = HighlighterUtl.StartsWith( doc, _Enclosures, i );
				if( pair == null )
				{
					pair = HighlighterUtl.StartsWith( doc, _LineHighlights, i );
					if( pair == null )
					{
						continue; // no pair matched
					}
				}

				// remember opener index
				_EPI.Insert( epiIndex, i );
				epiIndex++;

				// find closing pair
				closePos = HighlighterUtl.FindCloser( doc, pair, i+pair.opener.Length, end );
				if( closePos == -1 )
				{
					break; // no matching closer
				}

				// remember closer index and skip to the closer
				if( pair.closer != null )
				{
					_EPI.Insert( epiIndex, closePos + pair.closer.Length );
					i = closePos + pair.closer.Length;
				}
				else
				{
					_EPI.Insert( epiIndex, closePos );
					i = closePos;
				}
				epiIndex++;
			}
		}
		#endregion

		#region Utilities
		static bool Matched_Case1( Document doc, CharTreeNode node, int index )
		{
			if( node.child != null )
				return false;

			if( doc.Length < index+1 )
				return false;

			if( doc.Length == index+1 )
				return true;
			
			if( Char.IsLetterOrDigit(node.ch)
				&& Char.IsLetterOrDigit(doc[index+1]) )
			{
				return false;
			}

			return true;
		}

		static class Utl
		{
			public static int FindLeastMaximum( SplitArray<int> numbers, int value )
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

			public static void Highlight( Document doc, int index, CharTreeNode node, CharClass klass )
			{
				for( int i=0; i<node.depth; i++ )
				{
					doc.SetCharClass( index-i, klass );
				}
			}
		}
		#endregion
	}
}
