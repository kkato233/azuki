// file: KeywordHighlighter.cs
// brief: Keyword based highlighter.
//=========================================================
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// A keyword-based highlighter which can highlight keywords, text parts
	/// enclosed with specific patterns, line comment, and regular expressions.
	/// </summary>
	/// <remarks>
	/// <para>
	/// KeywordHighlighter highlights keywords, enclosed parts, and regular
	/// expressions. To make basic syntax highlighter, you can create an
	/// instance and register highlighting targets, or make a child class and
	/// register highlighting targets.
	/// </para>
	/// <para>
	/// KeywordHighlighter can highlight four types of text patterns.
	/// </para>
	/// <list type="number">
	///		<item>Keyword set</item>
	///		<item>Line highlight</item>
	///		<item>Enclosure</item>
	///		<item>Regular expression</item>
	/// </list>
	/// <para>
	/// Keyword set is a set of keywords.
	/// KeywordHighlighter searches a document for registered keywords and
	/// applies char-class associated with the keyword set.
	/// For example, you may create two keyword sets for highlighting C# source
	/// code. One of the them contains every keywords of C# and is associated
	/// with <see cref="CharClass"/>.Keyword. Another one contains every
	/// preprocessor macro keywords and is associated with <see
	/// cref="CharClass"/>.Macro. To register keyword sets, use <see
	/// cref="AddKeywordSet(String[],CharClass,Boolean)">AddKeywordSet</see>
	/// method.
	/// </para>
	/// <para>
	/// Line highlight is a feature to highlight text patterns which begins
	/// with particular pattern and continues until the end of line.
	/// This feature is designed to highlight single line comment found in
	/// many programming language. To register targets of line highlight, use
	/// <see cref="AddLineHighlight(String,CharClass,Boolean)"
	/// >AddLineHighlight</see> method.
	/// </para>
	/// <para>
	/// Enclosure is a text pattern that is enclosed with particular patterns.
	/// Typical example of enclosure type is &quot;string literal&quot; and
	/// &quot;multiple line comment&quot; found in many programming languages.
	/// To register enclosure target, use <see
	/// cref="AddEnclosure(String,String,CharClass,Boolean,Char,Boolean)"
	/// >AddEnclosure</see> method.
	/// </para>
	/// <para>
	/// Regular expression is one of the most flexible and popular method to
	/// express character sequence pattern. To register a regular expression
	/// to specify highlighting targets, give <see
	/// cref="AddRegex(String,Boolean,CharClass)">AddRegex</see> method a
	/// regular expression and a <see cref="CharClass"/>.
	/// Note that there is another overloaded method <see
	/// cref="AddRegex(String,Boolean,IList&lt;CharClass&gt;)"/>
	/// which takes not a CharClass but a list of them. This version applies
	/// the char-classes to each group captured in every matched patterns.
	/// If you need to highlight complex patterns consisting of sub-patterns
	/// each of which should be highlighted differently, this method will be
	/// useful.
	/// </para>
	/// <para>
	/// Here are some notes about highlighting with regular expressions.
	/// </para>
	/// <list type="bullet">
	///		<item>
	///		If you need to specify preceding or following text patterns for
	///		specifying highlighting targets strictly, consider using
	///		zero-width assertions such as <c>(?=...)</c> and <c>(?!...)</c>.
	///		For example, regular expression literals used in Perl may be
	///		specified as <c>(?&lt;!\w\s*)/([^/\\]|\\.)+/[a-z]*</c>,
	///		which uses a negative lookbehind assertion to prevent highlighting
	///		patterns look like <c>/.../</c> whose preceding non-whitespace
	///		character is a letter or digit. By the assertion, <c>/3+2/</c>
	///		inside <c>$x=/3+2/;</c> will be highlighted but <c>$x=1/3+2/3;</c>
	///		will not be highlighted.
	///		</item>
	///		<item>
	///		The back-end of this feature is System. Text. RegularExpressions.
	///		Regex, which is provided by .NET Framework. For detail of regular
	///		expression, refer to the reference manual of that class.
	///		</item>
	/// </list>
	/// <para>
	/// There is one more note about this class. KeywordHighlighter highlights
	/// numeric literals like 3.14 or 0xFFFE by default. To disable this
	/// feature, set false to <see cref="HighlightsNumericLiterals"/> property.
	/// There is no customization option for this feature so if you want to
	/// highlight numeric literals in a way different from this class's,
	/// disable this feature and define regular expressions for numeric
	/// literals by your own. For more detail of this feature, see the document
	/// of <see cref="HighlightsNumericLiterals"/> property.
	/// </para>
	/// </remarks>
	/// <example>
	/// <para>
	/// Next example creates a highlighter object to highlight C# source code.
	/// </para>
	/// <code lang="C#">
	/// KeywordHighlighter kh = new KeywordHighlighter();
	/// 
	/// // Registering keyword set
	/// kh.AddKeywordSet( new string[]{
	/// 	"abstract", "as", "base", "bool", ...
	/// }, CharClass.Keyword );
	/// 
	/// // Registering pre-processor keywords
	/// string[] words = new string[] {
	/// 	"define", "elif", "else", "endif", "endregion", "error", "if",
	/// 	"line", "pragma", "region", "undef", "warning"
	/// };
	/// AddRegex( @"^\s*#\s*(" + String.Join(@"\b|", words) + @"\b)",
	/// 		  CharClass.Macro );
	/// 
	/// // Registering string literals and character literal
	/// kh.AddEnclosure( "'", "'", CharClass.String, false, '\\' );
	/// kh.AddEnclosure( "@\"", "\"", CharClass.String, true, '\"' );
	/// kh.AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );
	/// 
	/// // Registering comment
	/// kh.AddEnclosure( "/**", "*/", CharClass.DocComment, true );
	/// kh.AddEnclosure( "/*", "*/", CharClass.Comment, true );
	/// kh.AddLineHighlight( "///", CharClass.DocComment );
	/// kh.AddLineHighlight( "//", CharClass.Comment );
	/// </code>
	/// </example>
	public class KeywordHighlighter : IHighlighter
	{
		#region Inner Types and Fields
		class RegexPattern
		{
			public Regex regex;
			public IList<CharClass> klassList;
			public bool groupMatch;
			public RegexPattern( Regex regex,
								 bool groupMatch,
								 IList<CharClass> klassList )
			{
				Debug.Assert( 0 < klassList.Count );
				this.regex = regex;
				this.groupMatch = groupMatch;
				this.klassList = klassList;
			}
		}

		class LineContentCache
		{
			public int lineBegin = -1;
			public string lineContent;
		}

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

		const string DefaultWordCharSet =
			"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
		HighlightHook _HookProc = null;
		bool _HighlightsNumericLiterals = true;
		string _WordCharSet = null;
		List<KeywordSet> _Keywords = new List<KeywordSet>( 8 );
		List<Enclosure> _Enclosures = new List<Enclosure>( 8 );
		List<Enclosure> _LineHighlights = new List<Enclosure>( 8 );
		List<RegexPattern> _RegexPatterns = new List<RegexPattern>( 8 );
#		if DEBUG
		internal
#		endif
		SplitArray<int> _ReparsePoints = new SplitArray<int>( 64 );
		#endregion

		#region Highlight Settings
		/// <summary>
		/// Gets whether a highlighter hook procedure can be installed or not.
		/// </summary>
		public bool CanUseHook
		{
			get{ return true; }
		}

		/// <summary>
		/// Gets or sets highlighter hook procedure.
		/// </summary>
		/// <seealso cref="Sgry.Azuki.Highlighter.IHighlighter.CanUseHook">
		/// IHighlighter.CanUseHook property
		/// </seealso>
		/// <seealso cref="Sgry.Azuki.Highlighter.HighlightHook">
		/// HighlightHook delegate
		/// </seealso>
		public HighlightHook HookProc
		{
			get{ return _HookProc; }
			set{ _HookProc = value; }
		}

		/// <summary>
		/// Gets or sets whether to enable built-in logic to recognize numeric
		/// literals or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// By default, KeywordHighlighter recognizes numeric literals (such as
		/// 3.14, 0xfffe) automatically and highlights them. This built-in
		/// logic highlights:
		/// </para>
		/// <list style="bullet">
		/// 	<item>
		/// 	tokens starting with '0x' and every following character are one
		/// 	of '0123456789abcdefABCDEF', and
		/// 	</item>
		/// 	<item>
		/// 	tokens starting with digits or dot (period) and ends with one
		/// 	of 'fijlFIJL'.
		/// 	</item>
		/// </list>
		/// <para>
		/// This feature is a kind of legacy implemented back when this class
		/// cannot highlight patterns specified with regular expressions.
		/// Because there is no customization option, if you want to highlight
		/// numeric literals which cannot be highlighted by this logic, disable
		/// this feature and define regular expressions for numeric literals by
		/// your own.
		/// </para>
		/// </remarks>
		public bool HighlightsNumericLiterals
		{
			get{ return _HighlightsNumericLiterals; }
			set{ _HighlightsNumericLiterals = value; }
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure( string openPattern,
								  string closePattern,
								  CharClass klass )
		{
			AddEnclosure( openPattern, closePattern, klass, true, '\0' );
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure( string openPattern,
								  string closePattern,
								  CharClass klass,
								  bool multiLine )
		{
			AddEnclosure( openPattern, closePattern, klass, multiLine, '\0' );
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure( string openPattern,
								  string closePattern,
								  CharClass klass,
								  char escapeChar )
		{
			AddEnclosure( openPattern, closePattern, klass, true, escapeChar, false );
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure( string openPattern,
								  string closePattern,
								  CharClass klass,
								  bool multiLine,
								  char escapeChar )
		{
			AddEnclosure( openPattern, closePattern, klass, true, escapeChar, false );
		}

		/// <summary>
		/// Adds a pair of strings and character-class
		/// that characters between the pair will be classified as.
		/// </summary>
		public void AddEnclosure( string openPattern,
								  string closePattern,
								  CharClass klass,
								  bool multiLine,
								  char escapeChar,
								  bool ignoreCase )
		{
			Enclosure pair = new Enclosure( openPattern,
											closePattern,
											klass,
											escapeChar,
											multiLine,
											ignoreCase );
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
		/// <param name="openPattern">
		/// Opening pattern of the line-comment.
		/// </param>
		/// <param name="klass">
		/// Class to apply to highlighted text.
		/// </param>
		public void AddLineHighlight( string openPattern, CharClass klass )
		{
			AddLineHighlight( openPattern, klass, false );
		}

		/// <summary>
		/// Adds a line-highlight entry.
		/// </summary>
		/// <param name="openPattern">
		/// Opening pattern of the line-comment.
		/// </param>
		/// <param name="klass">
		/// Class to apply to highlighted text.
		/// </param>
		/// <param name="ignoreCase">
		/// Whether the opening pattern should be matched case-insensitively.
		/// </param>
		public void AddLineHighlight( string openPattern,
									  CharClass klass,
									  bool ignoreCase )
		{

			Enclosure pair = new Enclosure( openPattern,
											null,
											klass,
											'\0',
											false,
											ignoreCase );

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
		/// This method is obsoleted. Please use
		/// <see cref="AddKeywordSet(string[],CharClass)"/>
		/// method instead.
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
		/// <param name="keywords">
		/// Sorted array of keywords.
		/// </param>
		/// <param name="klass">
		/// Char-class to be applied to the keyword set.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// Parameter 'keywords' are not sorted alphabetically.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// Parameter 'keywords' is null.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method registers a set of keywords to be highlighted.
		/// </para>
		/// <para>
		/// The keywords stored in <paramref name="keywords"/> parameter will
		/// be highlighted as a character class specified by <paramref
		/// name="klass"/> parameter. Please ensure that keywords in <paramref
		/// name="keywords"/> parameter must be alphabetically sorted. If they
		/// are not sorted, <see cref="ArgumentException"/> will be thrown.
		/// </para>
		/// <para>
		/// The keywords will be matched case sensitively and supposed to be
		/// consisted with only alphabets, numbers and underscore ('_'). If
		/// other character must be considered as a part of keyword, use <see
		/// cref="Sgry.Azuki.Highlighter.KeywordHighlighter.WordCharSet">
		/// WordCharSet</see> property.
		/// </para>
		/// </remarks>
		/// <seealso cref="AddKeywordSet(String[],CharClass,bool)">
		/// AddKeywordSet method (another overloaded method)
		/// </seealso>
		public void AddKeywordSet( string[] keywords, CharClass klass )
		{
			AddKeywordSet( keywords, klass, false );
		}

		/// <summary>
		/// Adds a set of keywords to be highlighted.
		/// </summary>
		/// <param name="keywords">Sorted array of keywords.</param>
		/// <param name="klass">
		/// Char-class to be applied to the keyword set.
		/// </param>
		/// <param name="ignoreCase">
		/// Whether case of the keywords should be ignored or not.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// Parameter 'keywords' are not sorted alphabetically.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// Parameter 'keywords' is null.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method registers a set of keywords to be highlighted.
		/// </para>
		/// <para>
		/// The keywords stored in <paramref name="keywords"/> parameter will
		/// be highlighted as a character class specified by <paramref
		/// name="klass"/> parameter. Please ensure that keywords in <paramref
		/// name="keywords"/> parameter must be alphabetically sorted. If they
		/// are not sorted, <see cref="ArgumentException"/> will be thrown.
		/// </para>
		/// <para>
		/// If <paramref name="ignoreCase"/> is true, KeywordHighlighter
		/// ignores case of all given keywords on matching. Note that if
		/// <paramref name="ignoreCase"/> is true, all characters of keywords
		/// must be in lower case otherwise keywords may not be highlighted
		/// properly.
		/// </para>
		/// <para>
		/// If other character must be considered as a part of keyword, use
		/// <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter.WordCharSet"
		/// >WordCharSet</see> property.
		/// </para>
		/// </remarks>
		public void AddKeywordSet( string[] keywords,
								   CharClass klass,
								   bool ignoreCase )
		{
			if( keywords == null )
				throw new ArgumentNullException("keywords");

			KeywordSet set = new KeywordSet();

			// ensure keywords are sorted alphabetically
			for( int i=0; i<keywords.Length-1; i++ )
				if( 0 <= keywords[i].CompareTo(keywords[i+1]) )
					throw new ArgumentException(
						String.Format(
							"Keywords must be sorted alphabetically;"
							+ " '{0}' is expected to be greater than"
							+ " '{1}' but not greater.",
							keywords[i+1], keywords[i]),
						"value" );

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

		void AddCharNode( string keyword,
						  int index,
						  CharTreeNode parent,
						  int depth )
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
		/// <exception cref="ArgumentException">
		/// Characters in value are not sorted alphabetically.
		/// </exception>
		/// <remarks>
		/// <para>
		/// KeywordHighlighter treats a sequence of characters in a
		/// word-character set as a word. The word-character set must be an
		/// alphabetically sorted character sequence. Setting this property to
		/// a character sequence which is not sorted alphabetically, <see
		/// cref="System.ArgumentException"/> will be thrown. If this property
		/// was set to null, KeywordHighlighter uses internally defined default
		/// word-character set. Default word-character set is <c
		/// >0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz</c
		/// >.
		/// </para>
		/// <para>
		/// Word-character set affects keyword matching process. If a keyword
		/// partially matched to a token in a document, KeywordHighlighter
		/// checks whether the character at the place where the match ended is
		/// included in the word-character set or not. Then if it was NOT a one
		/// of the word-character set, KeywordHighlighter determines the token
		/// which ends there is a keyword and highlight the token. For example,
		/// if word-character set is &quot;abc_&quot; and document is
		/// &quot;abc-def abc_def&quot;, &quot;abc&quot; of &quot;abc-def&quot;
		/// will be highlighted but &quot;abc&quot; of &quot;abc_def&quot; will
		/// NOT be highlighted because following character for former one ('-')
		/// is not included in the word-character set but one of the latter
		/// pattern ('_') is included. Note that if there are keywords that
		/// contain characters not included in the word-character set,
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
				for( int i=0; i<value.Length-1; i++ )
					if( value[i+1] < value[i] )
						throw new ArgumentException(
							String.Format(
								"word character set must be a sequence of"
								+ " alphabetically sorted characters; '{0}'"
								+ " (U+{1:x4}) is expected to be greater than"
								+ " '{2}' (U+{3:x4}) but not greater.",
								value[i+1], (int)value[i+1],
								value[i], (int)value[i] ),
							"value"
						);

				_WordCharSet = value;
			}
		}
		
		/// <summary>
		/// Entry a pattern specified with a regular expression (case
		/// sensitive) to be highlighted.
		/// </summary>
		/// <param name="regex">
		/// A regular expression expressing a text pattern to be highlighted.
		/// </param>
		/// <param name="klass">
		/// Character class to be assigned for each characters
		/// consisting the pattern matched with the regular expression.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Parameter 'regex' was null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Parameter 'regex' was not a valid regular expression.
		/// </exception>
		public void AddRegex( string regex, CharClass klass )
		{
			AddRegex( regex, false, klass );
		}

		/// <summary>
		/// Entry a pattern specified with a regular expression
		/// to be highlighted.
		/// </summary>
		/// <param name="regex">
		/// A regular expression expressing a text pattern to be highlighted.
		/// </param>
		/// <param name="ignoreCase">
		/// Whether the regular expression should be matched
		/// case-insensitively or not.
		/// </param>
		/// <param name="klass">
		/// Character class to be assigned for each characters
		/// consisting the pattern matched with the regular expression.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Parameter 'regex' was null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Parameter 'regex' was not a valid regular expression.
		/// </exception>
		public void AddRegex( string regex,
							  bool ignoreCase,
							  CharClass klass )
		{
			if( regex == null )
				throw new ArgumentNullException( "regex" );

			RegexOptions opt = RegexOptions.Compiled;
			if( ignoreCase )
				opt |= RegexOptions.IgnoreCase;
			Regex r = new Regex( regex, opt );

			AddRegex( r, klass );
		}

		/// <summary>
		/// Entry a pattern specified with a regular expression
		/// to be highlighted.
		/// </summary>
		/// <param name="regex">
		/// A regular expression expressing a text pattern to be highlighted.
		/// </param>
		/// <param name="klass">
		/// Character class to be assigned for each characters
		/// consisting the pattern matched with the regular expression.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Parameter 'regex' was null.
		/// </exception>
		public void AddRegex( Regex regex,
							  CharClass klass )
		{
			if( regex == null )
				throw new ArgumentNullException( "regex" );

			_RegexPatterns.Add( new RegexPattern( regex,
												  false,
												  new CharClass[]{klass} ) );
		}

		/// <summary>
		/// Entry a pattern specified with a regular expression (case
		/// sensitive) containing capturing groups which will be highlighted.
		/// </summary>
		/// <param name="regex">
		/// A regular expression containing capturing groups
		/// to be highlighted.
		/// </param>
		/// <param name="klassList">
		/// A list of character classes to be assigned,
		/// for each captured groups in the regular expression.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Parameter 'regex' or 'klassList' was null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Parameter 'regex' was not a valid regular expression.
		/// </exception>
		public void AddRegex( string regex,
							  IList<CharClass> klassList )
		{
			AddRegex( regex, false, klassList );
		}

		/// <summary>
		/// Entry a pattern specified with a regular expression containing
		/// capturing groups which will be highlighted.
		/// </summary>
		/// <param name="regex">
		/// A regular expression containing capturing groups
		/// to be highlighted.
		/// </param>
		/// <param name="ignoreCase">
		/// Whether the regular expression should be matched
		/// case-insensitively or not.
		/// </param>
		/// <param name="klassList">
		/// A list of character classes to be assigned,
		/// for each captured groups in the regular expression.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Parameter 'regex' or 'klassList' was null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Parameter 'regex' was not a valid regular expression.
		/// </exception>
		public void AddRegex( string regex,
							  bool ignoreCase,
							  IList<CharClass> klassList )
		{
			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( klassList == null )
				throw new ArgumentNullException( "klassList" );
			if( klassList.Count == 0 )
				throw new ArgumentException( "klassList" );

			RegexOptions opt = RegexOptions.Compiled;
			if( ignoreCase )
				opt |= RegexOptions.IgnoreCase;
			Regex r = new Regex( regex, opt );

			AddRegex( r, klassList );
		}

		/// <summary>
		/// Entry a pattern specified with a regular expression containing
		/// capturing groups which will be highlighted.
		/// </summary>
		/// <param name="regex">
		/// A regular expression containing capturing groups
		/// to be highlighted.
		/// </param>
		/// <param name="klassList">
		/// A list of character classes to be assigned,
		/// for each captured groups in the regular expression.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Parameter 'regex' or 'klassList' was null.
		/// </exception>
		public void AddRegex( Regex regex, IList<CharClass> klassList )
		{
			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( klassList == null )
				throw new ArgumentNullException( "klassList" );

			_RegexPatterns.Add( new RegexPattern(regex, true, klassList) );
		}

		/// <summary>
		/// Removes all entry of patterns specified with a regular expression
		/// to be highlighted.
		/// </summary>
		public void ClearRegex()
		{
			_RegexPatterns.Clear();
		}
		#endregion

		#region Highlighting Logic
		/// <summary>
		/// Parse and highlight keywords.
		/// </summary>
		/// <param name="doc">Document to highlight.</param>
		public virtual void Highlight( Document doc )
		{
			int begin = 0;
			int end = doc.Length;
			Highlight( doc, ref begin, ref end );
		}

		/// <summary>
		/// Parse and highlight keywords.
		/// </summary>
		/// <param name="doc">Document to highlight.</param>
		/// <param name="dirtyBegin">
		/// Index to start highlighting.
		/// On return, start index of the range to be invalidated.
		/// </param>
		/// <param name="dirtyEnd">
		/// Index to end highlighting.
		/// On return, end index of the range to be invalidated.
		/// </param>
		public virtual void Highlight( Document doc,
									   ref int dirtyBegin,
									   ref int dirtyEnd )
		{
			if( dirtyBegin < 0 || doc.Length < dirtyBegin )
				throw new ArgumentOutOfRangeException( "dirtyBegin" );
			if( dirtyEnd < 0 || doc.Length < dirtyEnd )
				throw new ArgumentOutOfRangeException( "dirtyEnd" );

			int index, nextIndex;
			bool highlighted;
			LineContentCache cache = new LineContentCache();

			// Determine range to highlight
			dirtyBegin = Utl.FindReparsePoint( _ReparsePoints, dirtyBegin );
			dirtyEnd = Utl.FindReparseEndPoint( doc, dirtyEnd );

			// seek each chars and do pattern matching
			index = dirtyBegin;
			while( 0 <= index && index < dirtyEnd )
			{
				// highlight line-comment if this token starts one
				Utl.TryHighlight( doc, _LineHighlights,
								  index, dirtyEnd,
								  _HookProc, out nextIndex );
				if( index < nextIndex )
				{
					// successfully highlighted. skip to next.
					Utl.EntryReparsePoint( _ReparsePoints, index );
					index = nextIndex;
					continue;
				}

				// highlight enclosing part if this token begins a part
				Utl.TryHighlight( doc, _Enclosures,
								  index, dirtyEnd,
								  _HookProc, out nextIndex );
				if( index < nextIndex )
				{
					// successfully highlighted. skip to next.
					Utl.EntryReparsePoint( _ReparsePoints, index );
					index = nextIndex;
					continue;
				}

				// highlight keyword if this token is a keyword
				highlighted = TryHighlight( doc, _Keywords, _WordCharSet,
											index, dirtyEnd, out nextIndex );
				if( highlighted )
				{
					index = nextIndex;
					continue;
				}

				// highlight digit as number
				if( _HighlightsNumericLiterals )
				{
					nextIndex = Utl.TryHighlightNumberToken( doc,
															 index, dirtyEnd,
															 _HookProc );
					if( index < nextIndex )
					{
						index = nextIndex;
						continue;
					}
				}

				// highlight regular expressions
				highlighted = TryHighlight( doc, _RegexPatterns, cache,
											index, dirtyEnd, out nextIndex );
				if( highlighted )
				{
					Utl.EntryReparsePoint( _ReparsePoints, index );
					index = nextIndex;
					continue;
				}

				// This is not a token to be highlighted.
				// Reset classes and seek to next token.
				nextIndex = Utl.FindNextToken( doc, index, _WordCharSet );
				Utl.Highlight( doc, index, nextIndex,
							   CharClass.Normal, _HookProc );
				index = nextIndex;
			}

			// report lastly parsed position
			if( dirtyEnd < index )
			{
				dirtyEnd = index;
			}
		}

		/// <summary>
		/// Do keyword matching in [startIndex, endIndex) through keyword
		/// char-tree.
		/// </summary>
		bool TryHighlight( Document doc,
						   List<KeywordSet> keywords,
						   string wordCharSet,
						   int startIndex,
						   int endIndex,
						   out int nextSeekIndex )
		{
			bool highlighted = false;

			nextSeekIndex = startIndex;
			foreach( KeywordSet set in keywords )
			{
				highlighted = TryHighlight_OneKeyword( doc, set, wordCharSet,
													   startIndex, endIndex,
													   out nextSeekIndex );
				if( highlighted )
				{
					break;
				}
			}

			return highlighted;
		}

		bool TryHighlight_OneKeyword(
				Document doc, KeywordSet set, string wordCharSet,
				int startIndex, int endIndex, out int nextSeekIndex
			)
		{
			CharTreeNode node;
			int index;

			// keyword char-tree made with "char", "if", "int", "interface",
			// "long"looks like (where * means a node with null-character):
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
						// (at least the keyword was partially matched, and
						// the token in document at this place ends exactly)
						// highlight and exit
						Utl.Highlight( doc, index-node.depth+1, index+1,
									   set.klass, _HookProc );
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

		bool TryHighlight( Document doc,
						   IList<RegexPattern> patterns,
						   LineContentCache cache,
						   int begin, int end,
						   out int nextSeekIndex )
		{
			Debug.Assert( doc != null );
			Debug.Assert( patterns != null );
			Debug.Assert( cache != null );
			Debug.Assert( 0 <= begin );
			Debug.Assert( begin < end );

			nextSeekIndex = begin;

			// Do nothing if no regular expressions registered
			if( patterns.Count == 0 )
			{
				return false;
			}

			// Because KeywordHighlighter applies regular expressions line per
			// line basis, do nothing in a middle of lines.
			if( TextUtil.IsEolChar(doc[begin]) )
			{
				return false;
			}

			// Get the content of the line
			int lineHeadIndex = doc.GetLineHeadIndexFromCharIndex( begin );
			if( cache.lineBegin != lineHeadIndex )
			{
				cache.lineBegin = lineHeadIndex;
				int lineIndex = doc.GetLineIndexFromCharIndex(cache.lineBegin);
				cache.lineContent = doc.GetLineContent( lineIndex );
			}
			int offset = begin - cache.lineBegin;

			// Evaluate regular expressions
			foreach( RegexPattern pattern in patterns )
			{
				Match match = pattern.regex.Match( cache.lineContent, offset );
				if( match.Success == false || match.Index != offset )
				{
					continue;
				}

				if( pattern.groupMatch )
				{
					for( int i=1; i<match.Groups.Count; i++ )
					{
						Group g = match.Groups[i];
						int patBegin = cache.lineBegin + g.Index;
						int patEnd = cache.lineBegin + g.Index + g.Length;
						if( patBegin < patEnd
							&& i-1 < pattern.klassList.Count )
						{
							Utl.Highlight( doc, patBegin, patEnd,
										   pattern.klassList[i-1], _HookProc );
							nextSeekIndex = Math.Max( nextSeekIndex, patEnd );
						}
					}
				}
				else
				{
					int patBegin = cache.lineBegin + match.Index;
					int patEnd = cache.lineBegin + match.Index + match.Length;
					if( patBegin < patEnd )
					{
						Utl.Highlight( doc, patBegin, patEnd,
									   pattern.klassList[0], _HookProc );
						nextSeekIndex = Math.Max( nextSeekIndex, patEnd );
					}
				}
			}

			return (begin < nextSeekIndex);
		}
		#endregion

		#region Utilities
		static bool Matches( char ch1, char ch2, bool ignoreCase )
		{
			if( ignoreCase )
			{
				int c1 = ('A' <= ch1 && ch1 <= 'Z') ? ('a' + ch1 - 'A') : ch1;
				int c2 = ('A' <= ch2 && ch2 <= 'Z') ? ('a' + ch2 - 'A') : ch2;
				return (c1 == c2);
			}
			else
			{
				return (ch1 == ch2);
			}
		}

		static bool MatchedExactly( Document doc,
									CharTreeNode node,
									int index,
									string wordChars )
		{
			// 'exact match' cases are next two:
			// 1) node.child is null, document token ends there
			// 2) node.child is '\0', document token ends there

			// document token ends there?
			if( index+1 == doc.Length
				|| (index+1 < doc.Length
					&& Utl.IsWordChar(wordChars, doc[index+1]) == false) )
			{
				// and, node.child is null or '\0'?
				if( node.child == null || node.child.ch == '\0' )
				{
					return true;
				}
			}
			return false;
		}
		#endregion
	}
}
