// file: KeywordHighlighter.cs
// brief: Keyword based highlighter.
// author: YAMAMOTO Suguru
// update: 2011-07-10
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
	/// <remarks>
	/// <para>
	/// This class provides feature to highlight document in keyword oriented way.
	/// Users can create an instance of this class and customize it directly to achieve
	/// desired highlighting result, or can define a child class of this and customize it.
	/// </para>
	/// <para>
	/// KeywordHighlighter can highlight three types of text pattern as follows.
	/// </para>
	/// <list type="number">
	///		<item>keyword set</item>
	///		<item>line highlight</item>
	///		<item>enclosure</item>
	/// </list>
	/// <para>
	/// Keyword set is a set of keywords.
	/// KeywordHighlighter searches document for registered keywords and,
	/// for each found word, applies char-class associated with the keyword set
	/// to the characters consisting the word.
	/// For example, C/C++ source code includes keywords and pre-processor macro keywords
	/// so user may define one keyword set containing all C/C++ keywords and associate
	/// <see cref="Sgry.Azuki.CharClass">CharClass</see>.Keyword,
	/// and another keyword set containing all pre-processor macro keywords and associate
	/// <see cref="Sgry.Azuki.CharClass">CharClass</see>.Macro.
	/// To register keyword sets, use
	/// <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter.AddKeywordSet(String[], CharClass, Boolean)">
	/// AddKeywordSet</see> method.
	/// </para>
	/// <para>
	/// Line highlight is a text pattern that begins with particular pattern at anywhere in a line
	/// and ends at the end of the line.
	/// Line highlight is designed to highlight single line comment syntax
	/// that very many programming language defines.
	/// To register targets of line highlight, use
	/// <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter.AddLineHighlight"
	/// >AddLineHighlight</see> method.
	/// </para>
	/// <para>
	/// Enclosure is a text pattern that is enclosed with a beginning pattern and an ending pattern.
	/// Typical example of enclosure type is &quot;string literal&quot; and &quot;multiple line comment&quot;
	/// in programming languages.
	/// To register enclosure target, use
	/// <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter.AddEnclosure(String, String, CharClass, Boolean, Char)">
	/// AddEnclosure</see> method.
	/// </para>
	/// </remarks>
	/// <example>
	/// <para>
	/// Next example creates a highlighter object to highlight C# source code.
	/// </para>
	/// <code lang="C#">
	/// KeywordHighlighter kh = new KeywordHighlighter();
	/// 
	/// // register keyword set
	/// kh.AddKeywordSet( new string[]{
	/// 	"abstract", "as", "base", "bool", ...
	/// }, CharClass.Keyword );
	/// 
	/// // register pre-processor keywords
	/// kh.AddKeywordSet( new string[] {
	/// 	"#define", "#elif", "#else", "#endif", ...
	/// }, CharClass.Macro );
	/// 
	/// // register string literals and character literal
	/// kh.AddEnclosure( "'", "'", CharClass.String, false, '\\' );
	/// kh.AddEnclosure( "@\"", "\"", CharClass.String, true, '\"' );
	/// kh.AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );
	/// 
	/// // register comment
	/// kh.AddEnclosure( "/**", "*/", CharClass.DocComment, true );
	/// kh.AddEnclosure( "/*", "*/", CharClass.Comment, true );
	/// kh.AddLineHighlight( "///", CharClass.DocComment );
	/// kh.AddLineHighlight( "//", CharClass.Comment );
	/// </code>
	/// </example>
	public class KeywordHighlighter : IHighlighter
	{
		#region Inner Types and Fields
		class KeywordSet
		{
			public CharTreeNode root = new CharTreeNode();
			public CharClass klass = CharClass.Normal;
			public bool ignoresCase = false;
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

		const string DefaultWordCharSet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
		HighlightHook _HookProc = null;
		string _WordCharSet = null;
		List<KeywordSet> _Keywords = new List<KeywordSet>( 16 );
		List<Enclosure> _Enclosures = new List<Enclosure>( 2 );
		List<Enclosure> _LineHighlights = new List<Enclosure>( 2 );
#		if DEBUG
		internal
#		endif
		SplitArray<int> _ReparsePoints = new SplitArray<int>( 64 );
		#endregion

		#region Highlight Settings
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
		/// <seealso cref="Sgry.Azuki.Highlighter.IHighlighter.CanUseHook">IHighlighter.CanUseHook property</seealso>
		/// <seealso cref="Sgry.Azuki.Highlighter.HighlightHook">HighlightHook delegate</seealso>
		public HighlightHook HookProc
		{
			get{ return _HookProc; }
			set{ _HookProc = value; }
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure(
				string openPattern, string closePattern, CharClass klass
			)
		{
			AddEnclosure( openPattern, closePattern, klass, true, '\0' );
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure(
				string openPattern, string closePattern, CharClass klass, bool multiLine
			)
		{
			AddEnclosure( openPattern, closePattern, klass, multiLine, '\0' );
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure(
				string openPattern, string closePattern, CharClass klass, char escapeChar
			)
		{
			AddEnclosure( openPattern, closePattern, klass, true, escapeChar );
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure(
				string openPattern, string closePattern, CharClass klass, bool multiLine, char escapeChar
			)
		{
			Enclosure pair = new Enclosure();
			pair.opener = openPattern;
			pair.closer = closePattern;
			pair.klass = klass;
			pair.escape = escapeChar;
			pair.multiLine = multiLine;
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
		/// (Please use AddKeywordSet instead.)
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method is obsoleted.
		/// Please use
		/// <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter.AddKeywordSet(String[], CharClass)">
		/// AddKeywordSet</see> method instead.
		/// </para>
		/// </remarks>
		[Obsolete("Please use AddKeywordSet method instead.", true)]
		public void SetKeywords( string[] keywords, CharClass klass )
		{
			AddKeywordSet( keywords, klass, false );
		}

		/// <summary>
		/// Adds a set of keywords to be highlighted.
		/// </summary>
		/// <param name="keywords">Sorted array of keywords.</param>
		/// <param name="klass">Char-class to be applied to the keyword set.</param>
		/// <exception cref="System.ArgumentException">Parameter 'keywords' are not sorted alphabetically.</exception>
		/// <exception cref="System.ArgumentNullException">Parameter 'keywords' is null.</exception>
		/// <remarks>
		/// <para>
		/// This method registers a set of keywords to be highlighted.
		/// </para>
		/// <para>
		/// The keywords stored in <paramref name="keywords"/> parameter will be highlighted
		/// as a character class specified by <paramref name="klass"/> parameter.
		/// Please ensure that keywords in <paramref name="keywords"/> parameter
		/// must be alphabetically sorted.
		/// If they are not sorted, <see cref="ArgumentException"/> will be thrown.
		/// </para>
		/// <para>
		/// The keywords will be matched case sensitively
		/// and supposed to be consisted with only alphabets, numbers and underscore ('_').
		/// If other character must be considered as a part of keyword,
		/// use <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter.WordCharSet">WordCharSet</see>
		/// property.
		/// </para>
		/// </remarks>
		/// <seealso cref="AddKeywordSet(String[], CharClass, Boolean)">AddKeywordSet method (another overloaded method)</seealso>
		public void AddKeywordSet( string[] keywords, CharClass klass )
		{
			AddKeywordSet( keywords, klass, false );
		}

		/// <summary>
		/// Adds a set of keywords to be highlighted.
		/// </summary>
		/// <param name="keywords">Sorted array of keywords.</param>
		/// <param name="klass">Char-class to be applied to the keyword set.</param>
		/// <param name="ignoreCase">Whether case of the keywords should be ignored or not.</param>
		/// <exception cref="System.ArgumentException">Parameter 'keywords' are not sorted alphabetically.</exception>
		/// <exception cref="System.ArgumentNullException">Parameter 'keywords' is null.</exception>
		/// <remarks>
		/// <para>
		/// This method registers a set of keywords to be highlighted.
		/// </para>
		/// <para>
		/// The keywords stored in <paramref name="keywords"/> parameter will be highlighted
		/// as a character class specified by <paramref name="klass"/> parameter.
		/// Please ensure that keywords in <paramref name="keywords"/> parameter
		/// must be alphabetically sorted.
		/// If they are not sorted, <see cref="ArgumentException"/> will be thrown.
		/// </para>
		/// <para>
		/// If <paramref name="ignoreCase"/> is true,
		/// KeywordHighlighter ignores case of all given keywords on matching.
		/// Note that if <paramref name="ignoreCase"/> is true,
		/// all characters of keywords must be in lower case
		/// otherwise keywords may not be highlighted properly.
		/// </para>
		/// <para>
		/// If other character must be considered as a part of keyword,
		/// use <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter.WordCharSet">WordCharSet</see>
		/// property.
		/// </para>
		/// </remarks>
		public void AddKeywordSet( string[] keywords, CharClass klass, bool ignoreCase )
		{
			if( keywords == null )
				throw new ArgumentNullException("keywords");

			KeywordSet set = new KeywordSet();

			// ensure keywords are sorted alphabetically
#			if !PocketPC
			for( int i=0; i<keywords.Length-1; i++ )
				if( 0 <= keywords[i].CompareTo(keywords[i+1]) )
					throw new ArgumentException(
						String.Format("keywords must be sorted alphabetically; '{0}' is expected to be greater than '{1}' but not greater.", keywords[i+1], keywords[i]),
						"value"
					);
#			endif

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

			// set other attributes
			set.klass = klass;
			set.ignoresCase = ignoreCase;

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

		/// <summary>
		/// Gets or sets word-character set.
		/// </summary>
		/// <exception cref="ArgumentException">Characters in value are not sorted alphabetically.</exception>
		/// <remarks>
		/// <para>
		/// KeywordHighlighter treats a sequence of characters in a word-character set as a word.
		/// The word-character set must be an alphabetically sorted character sequence.
		/// Setting this property to a character sequence which is not sorted alphabetically,
		/// <see cref="System.ArgumentException"/> will be thrown.
		/// If this property was set to null, KeywordHighlighter uses
		/// internally defined default word-character set.
		/// Default word-character set is
		/// <c>0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz</c>.
		/// </para>
		/// <para>
		/// Word-character set affects keyword matching process.
		/// If a keyword partially matched to a token in a document,
		/// KeywordHighlighter checks whether the character at the place where the match ended
		/// is included in the word-character set or not.
		/// Then if it was NOT a one of the word-character set,
		/// KeywordHighlighter determines the token which ends there is a keyword
		/// and highlight the token.
		/// For example, if word-character set is &quot;abc_&quot; and document is
		/// &quot;abc-def abc_def&quot;, &quot;abc&quot; of &quot;abc-def&quot; will be highlighted
		/// but &quot;abc&quot; of &quot;abc_def&quot; will NOT be highlighted
		/// because following character for former one ('-') is not included in the word-character set
		/// but one of the latter pattern ('_') is included.
		/// Note that if there are keywords that contain characters not included in the word-character set,
		/// KeywordHighlighter will not highlight such keywords properly.
		/// </para>
		/// </remarks>
		public string WordCharSet
		{
			get
			{
				if( _WordCharSet != null )
					return _WordCharSet;
				else
					return DefaultWordCharSet;
			}
			set
			{
				// ensure word characters are sorted alphabetically
#				if !PocketPC
				for( int i=0; i<value.Length-1; i++ )
					if( value[i+1] < value[i] )
						throw new ArgumentException(
							String.Format("word character set must be a sequence of alphabetically sorted characters; '{0}' (U+{1:x4}) is expected to be greater than '{2}' (U+{3:x4}) but not greater.", value[i+1], (int)value[i+1], value[i], (int)value[i]),
							"value"
						);
#				endif

				_WordCharSet = value;
			}
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

			// seek each chars and do pattern matching
			index = dirtyBegin;
			while( 0 <= index && index < dirtyEnd )
			{
				// if the document has been shrunken while highlighting, shrunk the range too.
				// (although this is ad-hoc, but surely reduces risk of out-of-range exceptions)
				if( doc.Length < dirtyEnd )
				{
					dirtyEnd = doc.Length;
				}

				// highlight line-comment if this token starts one
				nextIndex = TryHighlightLineComment( doc, _LineHighlights, index, dirtyEnd );
				if( index < nextIndex )
				{
					// successfully highlighted. skip to next.
					HighlighterUtl.EntryReparsePoint( _ReparsePoints, index );
					index = nextIndex;
					continue;
				}

				// highlight enclosing part if this token begins a part
				nextIndex = TryHighlightEnclosure( doc, _Enclosures, index, dirtyEnd );
				if( index < nextIndex )
				{
					// successfully highlighted. skip to next.
					HighlighterUtl.EntryReparsePoint( _ReparsePoints, index );
					index = nextIndex;
					continue;
				}

				// highlight keyword if this token is a keyword
				highlighted = TryHighlightKeyword( doc, _Keywords, _WordCharSet, index, dirtyEnd, out nextIndex );
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
				nextIndex = HighlighterUtl.FindNextToken( doc, index, _WordCharSet );
				Highlight( doc, index, nextIndex, CharClass.Normal );
				index = nextIndex;
			}

			// report lastly parsed position
			if( dirtyEnd < index )
			{
				dirtyEnd = index;
			}
		}

		/// <summary>
		/// Highlight part between a enclosing pair registered.
		/// </summary>
		/// <returns>Index of next parse point if a pair was highlighted or startIndex</returns>
		int TryHighlightEnclosure( Document doc, List<Enclosure> pairs, int startIndex, int endIndex )
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
					Highlight( doc, startIndex, doc.Length, pair.klass );
					return doc.Length;
				}
				else
				{
					return startIndex;
				}
			}

			// highlight enclosed part
			Highlight( doc, startIndex, closePos + pair.closer.Length, pair.klass );
			return closePos + pair.closer.Length;
		}

		/// <summary>
		/// Highlight line comment.
		/// </summary>
		/// <returns>Index of next parse point if highlight succeeded or startIndex</returns>
		int TryHighlightLineComment( Document doc, List<Enclosure> pairs, int startIndex, int endIndex )
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
			Highlight( doc, startIndex, closePos, pair.klass );
			return closePos;
		}

		/// <summary>
		/// Do keyword matching in [startIndex, endIndex) through keyword char-tree.
		/// </summary>
		bool TryHighlightKeyword( Document doc, List<KeywordSet> keywords, string wordCharSet, int startIndex, int endIndex, out int nextSeekIndex )
		{
			bool highlighted = false;

			nextSeekIndex = startIndex;
			foreach( KeywordSet set in keywords )
			{
				highlighted = TryHighlightKeyword_One( doc, set, wordCharSet, startIndex, endIndex, out nextSeekIndex );
				if( highlighted )
				{
					break;
				}
			}

			return highlighted;
		}

		bool TryHighlightKeyword_One(
				Document doc, KeywordSet set, string wordCharSet,
				int startIndex, int endIndex, out int nextSeekIndex
			)
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
				if( Matches(node.ch, doc[index], set.ignoresCase) )
				{
					// matched.
					if( MatchedExactly(doc, node, index, wordCharSet) )
					{
						//--- the keyword exactly matched ---
						// (at least the keyword was partially matched,
						// and the token in document at this place ends exactly)
						// highlight and exit
						Highlight( doc, index-node.depth+1, index+1, set.klass );
						nextSeekIndex = index + 1;
						return true;
					}
					else
					{
						//--- the keyword not matched ---
						// continue matching process
						if( node.child != null && node.child.ch == '\0' )
							node = node.child.sibling;
						else
							node = node.child;
						index++;
					}
				}
				else
				{
					//--- unmatch char is found ---
					// try next keyword.
					node = node.sibling;
				}
			}

			nextSeekIndex = index;
			return false;
		}
		#endregion

		#region Utilities
		static bool Matches( char ch1, char ch2, bool ignoreCase )
		{
			if( ch1 == ch2 )
				return true;
			if( ignoreCase && Char.ToLower(ch1) == Char.ToLower(ch2) )
				return true;
			return false;
		}

		static bool MatchedExactly( Document doc, CharTreeNode node, int index, string wordChars )
		{
			// 'exact match' cases are next two:
			// 1) node.child is null, document token ends there
			// 2) node.child is '\0', document token ends there

			// document token ends there?
			if( index+1 == doc.Length
				|| (index+1 < doc.Length && HighlighterUtl.IsWordChar(wordChars, doc[index+1]) == false) )
			{
				// and, node.child is null or '\0'?
				if( node.child == null || node.child.ch == '\0' )
				{
					return true;
				}
			}
			return false;
		}

		void Highlight( Document doc, int begin, int end, CharClass klass )
		{
			// try to use hook if installed
			if( _HookProc != null )
			{
				string token = doc.GetTextInRange( begin, end );
				if( _HookProc(doc, token, begin, klass) == true )
				{
					return; // hook did something to this token.
				}
			}

			// normally highlight this token
			for( int i=begin; i<end; i++ )
			{
				doc.SetCharClass( i, klass );
			}
		}
		#endregion
	}
}
