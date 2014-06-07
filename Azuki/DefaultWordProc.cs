// file: DefaultWordProc.cs
// brief: built-in word processor for well Japanese handling
//=========================================================
using System;
using System.Collections.Generic;
using System.Text;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// Built-in word processor which can handle Japanese kinsoku-shori.
	/// </summary>
	/// <remarks>
	/// <para>
	/// DefaultWordProc is the only one built-in word processor. This class provides word handling
	/// logic which is specially adapted for Japanese language including 'kinsoku shori;' special
	/// prohibition rule of word-wrapping used in Japanese.
	/// </para>
	/// <seealso cref="IWordProc"/>
	/// <seealso cref="Document.WordProc"/>
	/// </remarks>
	public class DefaultWordProc : IWordProc
	{
		const int MaxHexIntLiteralLen = 32;
		delegate bool ClassifyCharProc( Document doc, int index );

		#region Fields
		int _KinsokuDepth = 8;
		bool _EnableLineEndRestriction = true;
		bool _EnableLineHeadRestriction = true;
		bool _EnableCharacterHanging = true;
		bool _EnableEolHanging = true;
		bool _EnableWordWrap = true;

		char[] _CharsForbiddenToEndLine = new char[] {
			'(', '[', '{',
			'\x00ab', // left-pointing double angle quotation mark
			'\x2014', // em dash (full-width dash)
			'\x2018', // left single quotation mark
			'\x201c', // left double quotation mark
			'\x2025', // two dot leader
			'\x2026', // horizontal ellipsis (three dot leader)
			'\x3008', // left angle bracket
			'\x300a', // left double angle bracket
			'\x300c', // left corner bracket
			'\x300e', // left white corner bracket
			'\x3010', // left black lenticular bracket
			'\x3014', // left tortoise shell bracket
			'\x3016', // left white lenticular bracket
			'\x3018', // left white tortoise shell bracket
			'\x301a', // left white square bracket
			'\x301d', // reversed double prime quotation mark
			'\x3033', // vertical kana repeat mark upper half
			'\x3034', // vertical kana repeat with voiced sound mark upper half
			'\xff08', '\xff3b', '\xff5b', '\xff5f' // full-width forms
		};
		char[] _CharsForbiddenToStartLine = new char[] {
			')', ',', '.', ':', ';', ']',
			'\x00bb', // right-pointing double angle quotation mark
			'\x2010', // hyphen
			'\x2013', // dash
			'\x2019', // right single quotation mark
			'\x201d', // right double quotation mark
			'\x3001', // ideographic comma (ja:tou ten)
			'\x3002', // ideographic full stop (ja:ku ten)
			'\x3005', // ideographic iteration mark
			'\x3009', // right angle bracket
			'\x300b', // right double angle bracket
			'\x300d', // right corner bracket
			'\x300f', // right white corner bracket
			'\x3011', // right black lenticular bracket
			'\x3015', // right tortoise shell bracket
			'\x3017', // right white lenticular bracket
			'\x3019', // right white tortoise shell bracket
			'\x301b', // right white square bracket
			'\x301c', // wave dash
			'\x301f', // low double prime quotation mark
			'\x3035', // vertical kana repeat mark lower half
			'\x303b', // vertical ideographic iteration mark
			'\x3041', '\x3043', '\x3045', '\x3047', '\x3049', '\x3063',
			'\x3083', '\x3085', '\x3087', '\x308e', '\x3095', '\x3096', // small hiragana
			'\x30a0', // katakana-hiragana double hyphen
			'\x30a1', '\x30a3', '\x30a5', '\x30a7', '\x30a9', '\x30c3',
			'\x30e3', '\x30e5', '\x30e7', '\x30ee', '\x30f5', '\x30f6', // small katakana
			'\x30fb', // katakana middle dot
			'\x30fc', // katakana-hiragana prolonged sound mark
			'\x30fd', // katakana iteration mark
			'\x30fe', // katakana voiced iteration mark
			'\x31f0', '\x31f1', '\x31f2', '\x31f3', '\x31f4', '\x31f5',
			'\x31f6', '\x31f7', '\x31f8', '\x31f9', '\x31fa', '\x31fb',
			'\x31fc', '\x31fd', '\x31fe', '\x31ff', // supplemental small katakana
			'\xff09', '\xff0c', '\xff0d', '\xff1a', '\xff1b', '\xff1d', '\xff3d', '\xff5d', '\xff60' // full-width form
		};
		char[] _CharsToBeHanged = new char[] {
			',', '.',
			'\x3001', // ideographic comma (ja: tou ten)
			'\x3002', // ideographic full stop (ja: ku ten)
			'\xff0c', // full-width comma
			'\xff0e'  // full-width full stop
		};
		#endregion

		#region IWordProc - Properties
		/// <summary>
		/// Gets or sets whether to avoid wrapping screen lines
		/// in the middle of an alphabet word or not.
		/// </summary>
		public virtual bool EnableWordWrap
		{
			get{ return _EnableWordWrap; }
			set{ _EnableWordWrap = value; }
		}

		/// <summary>
		/// Gets or sets whether to restrict characters which can end a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If this property was set true, Azuki tries to avoid placing characters specified by
		/// <see cref="CharsForbiddenToEndLine"/> at ends of a screen line.
		/// This is one of the restriction rules in kinsoku shori.
		/// </para>
		/// </remarks>
		/// <seealso cref="CharsForbiddenToEndLine"/>
		public virtual bool EnableLineEndRestriction
		{
			get{ return _EnableLineEndRestriction; }
			set{ _EnableLineEndRestriction = value; }
		}

		/// <summary>
		/// Gets or sets characters which are forbidden to end a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property is a set of characters. All characters included in the value will be
		/// avoided to be placed at the end of a screen line.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException"/>
		/// <seealso cref="EnableLineEndRestriction"/>
		public virtual char[] CharsForbiddenToEndLine
		{
			get{ return _CharsForbiddenToEndLine; }
			set
			{
				if( value == null )
					throw new ArgumentNullException( "value" );

				_CharsForbiddenToEndLine = (char[])value.Clone();
				Array.Sort( _CharsForbiddenToEndLine );
			}
		}

		/// <summary>
		/// Gets or sets whether to restrict characters which can start a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If this property was set true, Azuki tries to avoid placing characters specified by
		/// <see cref="CharsForbiddenToStartLine"/> at start of a screen line.
		/// This is one of the restriction rules in kinsoku shori.
		/// </para>
		/// </remarks>
		/// <seealso cref="CharsForbiddenToStartLine"/>
		public virtual bool EnableLineHeadRestriction
		{
			get{ return _EnableLineHeadRestriction; }
			set{ _EnableLineHeadRestriction = value; }
		}

		/// <summary>
		/// Gets or sets characters which are forbidden to start a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property is a set of characters. All characters included in the value will be
		/// avoided to be placed at start of a screen line.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException"/>
		/// <seealso cref="EnableLineHeadRestriction"/>
		public virtual char[] CharsForbiddenToStartLine
		{
			get{ return _CharsForbiddenToStartLine; }
			set
			{
				if( value == null )
					throw new ArgumentNullException( "value", "DefaultWordProc.CharsForbiddenToStartLine must not be null." );

				_CharsForbiddenToStartLine = (char[])value.Clone();
				Array.Sort( _CharsForbiddenToStartLine );
			}
		}

		/// <summary>
		/// Gets or sets whether to 'hang' specified characters on the end of screen lines.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether to 'hang' specified characters on the end of screen
		/// lines.
		/// </para>
		/// <para>
		/// The term 'hang' here means placing a character beyond the end of screen line. Hanged
		/// character will be drawn out of text area and not be sent to next screen line.
		/// </para>
		/// <para>
		/// Which characters are hanged is determined by <see cref="CharsToBeHanged"/>. To hang EOL
		/// code graphics, use <see cref="EnableEolHanging"/> instead.
		/// </para>
		/// </remarks>
		/// <seealso cref="CharsToBeHanged"/>
		/// <seealso cref="EnableEolHanging"/>
		public virtual bool EnableCharacterHanging
		{
			get{ return _EnableCharacterHanging; }
			set{ _EnableCharacterHanging = value; }
		}

		/// <summary>
		/// Gets or sets characters which will be 'hanged' on a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property is a set of characters. All characters included in the value will be
		/// avoided to be placed at start of a screen line, and will be 'hanged.' The term 'hang'
		/// means placing a character beyond the end of screen line. Hanged character will be drawn
		/// out of text area and will not be the starting character of the next screen line.
		/// </para>
		/// <para>
		/// Note that CR (U+000d) and LF (U+000a) must not be included in the value of this
		/// property. To hang graphics of CR, LF, or CR+LF, use <see cref="EnableEolHanging"/>.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException">Specified value contains one or more EOL characters.</exception>
		/// <seealso cref="EnableCharacterHanging"/>
		/// <seealso cref="EnableEolHanging"/>
		public virtual char[] CharsToBeHanged
		{
			get{ return _CharsToBeHanged; }
			set
			{
				if( value == null )
					throw new ArgumentNullException( "value" );
				if( 0 <= new String(value).IndexOfAny(TextUtil.EolChars) )
					throw new ArgumentException( "DefaultWordProc.CharsToBeHanged must not contain"
												 + " EOL codes.", "value" );

				// keep a sorted copy of the value
				_CharsToBeHanged = (char[])value.Clone();
				Array.Sort( _CharsToBeHanged );
			}
		}

		/// <summary>
		/// Gets or sets whether to 'hang' EOL characters on the end of screen lines.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether to 'hang' EOL graphics on the end of screen lines.
		/// </para>
		/// <para>
		/// The term 'hang' here means placing a character beyond the end of screen line. Hanged
		/// character will be drawn out of text area and will not be the starting character of the
		/// next screen line.
		/// </para>
		/// <para>
		/// To hang characters which is not EOL code, use <see cref="EnableCharacterHanging"/>
		/// instead.
		/// </para>
		/// </remarks>
		/// <seealso cref="EnableCharacterHanging"/>
		public virtual bool EnableEolHanging
		{
			get{ return _EnableEolHanging; }
			set{ _EnableEolHanging = value; }
		}
		#endregion

		#region IWordProc methods
		/// <summary>
		/// Searches document for start position of a word.
		/// </summary>
		/// <param name="doc">The document in which to search.</param>
		/// <param name="startIndex">The index to start the search from.</param>
		/// <returns>
		///   Index of start position of the found word, or length of the document if no word was
		///   found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="startIndex"/> is less than 0 or greater than length of the document.
		/// </exception>
		/// <seealso cref="IWordProc.NextWordStart">IWordProc.NextWordStart method</seealso>
		public virtual int NextWordStart( Document doc, int startIndex )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( startIndex < 0 || doc.Length < startIndex )
				throw new ArgumentOutOfRangeException( "startIndex" );

			int index;
			ClassifyCharProc isSameClass;
			
			// check start index
			index = doc.PrevGraphemeClusterIndex( startIndex );
			Debug.Assert( index < startIndex );
			if( index < 0 )
			{
				return 0;
			}
			
			// proceed until the char category changes
			isSameClass = ClassifyChar( doc, index );
			do
			{
				index = doc.NextGraphemeClusterIndex( index );
				if( doc.Length <= index )
					return doc.Length;
			}
			while( isSameClass(doc, index) );
			
			return index;
		}

		/// <summary>
		/// Searches document for end position of a word.
		/// </summary>
		/// <param name="doc">The document in which to search.</param>
		/// <param name="startIndex">The index to start the search from.</param>
		/// <returns>
		///   Index of end position of the found word, or length of the document if no word was found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="startIndex"/> is less than 0 or greater than length of the document.
		/// </exception>
		/// <seealso cref="IWordProc.NextWordEnd">IWordProc.NextWordEnd method</seealso>
		public virtual int NextWordEnd( Document doc, int startIndex )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( startIndex < 0 || doc.Length < startIndex )
				throw new ArgumentOutOfRangeException( "startIndex" );

			int index;
			ClassifyCharProc isSameClass;

			// check start index
			if( doc.Length <= startIndex )
			{
				return doc.Length;
			}

			// set seek starting index
			index = doc.PrevGraphemeClusterIndex( startIndex );
			if( index < 0 )
			{
				index = 0;
			}

			// proceed until the char category changes
			isSameClass = ClassifyChar( doc, index );
			do
			{
				index = doc.NextGraphemeClusterIndex( index );
				if( doc.Length <= index )
					return doc.Length;
			}
			while( isSameClass(doc, index) );
			
			return index;
		}

		/// <summary>
		/// Searches document backward for start position of a word.
		/// </summary>
		/// <param name="doc">The document in which to search.</param>
		/// <param name="startIndex">The index to start the search from.</param>
		/// <returns>
		///   Index of start position of a word if found, or 0 if no word was found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="startIndex"/> is less than 0 or greater than length of the document.
		/// </exception>
		/// <seealso cref="IWordProc.PrevWordStart"/>
		public virtual int PrevWordStart( Document doc, int startIndex )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( startIndex < 0 || doc.Length < startIndex )
				throw new ArgumentOutOfRangeException( "startIndex" );

			int index;
			ClassifyCharProc isSameClass;
			
			// check start index
			if( startIndex <= 0 )
			{
				return 0;
			}
			else if( doc.Length <= startIndex )
			{
				return doc.Length;
			}

			// set seek starting index
			index = startIndex;
			while( doc.IsNotDividableIndex(index) )
			{
				index--;
			}

			// proceed until the char category changes
			isSameClass = ClassifyChar( doc, index );
			do
			{
				index = doc.PrevGraphemeClusterIndex( index );
				if( index < 0 )
					return 0;
			}
			while( isSameClass(doc, index) );
			
			return doc.NextGraphemeClusterIndex( index );
		}

		/// <summary>
		/// Searches document backward for end position of a word.
		/// </summary>
		/// <param name="doc">The document in which to search.</param>
		/// <param name="startIndex">The index to start the search from.</param>
		/// <returns>
		///   Index of end position of a word if found, or length of the document if no word was
		///   found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="startIndex"/> is less than 0 or greater than length of the document.
		/// </exception>
		/// <seealso cref="IWordProc.PrevWordEnd">IWordProc.PrevWordEnd method</seealso>
		public virtual int PrevWordEnd( Document doc, int startIndex )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( startIndex < 0 || doc.Length < startIndex )
				throw new ArgumentOutOfRangeException( "startIndex" );

			int index;
			ClassifyCharProc isSameClass;
			
			// check start index
			if( startIndex <= 0 )
			{
				return 0;
			}
			else if( doc.Length <= startIndex )
			{
				return doc.Length;
			}

			// set seek starting index
			index = startIndex;
			while( doc.IsNotDividableIndex(index) )
			{
				index--;
			}

			// proceed until the char category changes
			isSameClass = ClassifyChar( doc, index );
			do
			{
				index = doc.PrevGraphemeClusterIndex( index );
				if( index < 0 )
					return 0;
			}
			while( isSameClass(doc, index) );

			return doc.NextGraphemeClusterIndex( index );
		}

		/// <summary>
		/// Determines where a screen line should be wrapped at.
		/// </summary>
		/// <param name="doc">The document currently rendering.</param>
		/// <param name="index">
		///   The index of character which is to be drawn over the right edge of text area.
		/// </param>
		/// <returns>The index of the character which starts the next screen line.</returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="index"/> is less than 0 or greater than length of the document.
		/// </exception>
		/// <seealso cref="IWordProc.HandleWordWrapping"/>
		public virtual int HandleWordWrapping( Document doc, int index )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( index < 0 || doc.Length < index )
				throw new ArgumentOutOfRangeException( "index" );

			int i;
			int startIndex;

			if( doc.Length <= index )
				return index;

			// execute EOL hanging
			if( EnableEolHanging
				&& IsEolCode(doc, index) )
			{
				if( doc[index] == '\r'
					&& index+1 < doc.Length && doc[index+1] == '\n' )
				{
					index += 2;
				}
				else
				{
					index += 1;
				}
				return index;
			}

			// execute 'hanging'
			startIndex = index;
			for( i=0; i<KinsokuDepth; i++ )
			{
				// execute character hanging
				if( EnableCharacterHanging
					&& index < doc.Length
					&& 0 <= Array.BinarySearch(CharsToBeHanged, doc[index]) )
				{
					index = doc.NextGraphemeClusterIndex( index );
					continue;
				}

				break;
			}
			if( KinsokuDepth <= i || doc.Length <= index )
			{
				index = startIndex; // reached maximum depth of kinsoku-shori
			}

			// restrict characters starting a line
			startIndex = index;
			for( i=0; i<KinsokuDepth; i++ )
			{
				// execute line-head 'oidashi'
				if( EnableLineHeadRestriction
					&& 1 <= index
					&& 0 <= Array.BinarySearch(CharsForbiddenToStartLine, doc[index]) )
				{
					index = doc.PrevGraphemeClusterIndex( index );
					continue;
				}

				break;
			}
			if( KinsokuDepth <= i || index <= 0 )
			{
				index = startIndex; // reached maximum depth of kinsoku-shori
			}

			// wrap words
			if( EnableWordWrap
				&& index < doc.Length
				&& (IsAlphabet(doc, index) || IsDigit(doc, index)) )
			{
				int wordBegin = doc.WordProc.PrevWordStart( doc, index );
				int wordEnd = doc.WordProc.NextWordEnd( doc, index );
				if( wordBegin <= index && index < wordEnd )
				{
					index = wordBegin;
				}
			}

			// restrict characters to end line
			startIndex = index;
			for( i=0; i<KinsokuDepth; i++ )
			{
				// execute line-end 'oidashi'
				if( EnableLineEndRestriction
					&& 1 <= index
					&& 0 <= Array.BinarySearch(CharsForbiddenToEndLine, doc[index-1]) )
				{
					index = doc.PrevGraphemeClusterIndex( index );
					continue;
				}

				break;
			}
			if( KinsokuDepth <= i || index <= 0 )
			{
				index = startIndex; // reached maximum depth of kinsoku-shori
			}

			// correct index if its forbidden position
			if( doc.IsNotDividableIndex(index) && 0 < index )
			{
				DebugUtl.Assert( index+1 < doc.Length );
				DebugUtl.Fail( String.Format(
					"kinsoku-shori resulted in forbidden index; between U+{0:X4} and U+{1:X4}", doc[index], doc[index+1]
				));
				index = doc.PrevGraphemeClusterIndex( index );
			}
			return index;
		}
		#endregion

		#region Other properties
		/// <summary>
		/// Gets or sets how many times a prohibition rule is applied to each screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// DefaultWordProc executes kinsoku shori on character by character basis. This property
		/// determines how many times the prohibition rules are applied for each characters which
		/// ends a screen line.
		/// </para>
		/// <para>
		/// If prohibition rules on character by character basis were applied only once, there are
		/// many cases that cannot be handled with. For example, if an open parenthesis was
		/// included in <see cref="CharsForbiddenToEndLine"/> and if a screen line ended with two
		/// open parentheses, applying kinsoku shori (line end restriction) once results pushing
		/// one open parenthesis to next screen line; and thus the screen line ends with an open
		/// parenthesis. Obviously this is not acceptable result of applying line end restriction.
		/// To solve this problem, DefaultWordProc applies rules multiple times.
		/// </para>
		/// <para>
		/// The default value of this property is 8.
		/// </para>
		/// </remarks>
		public virtual int KinsokuDepth
		{
			get{ return _KinsokuDepth; }
			set
			{
				if( value <= 0 )
					throw new InvalidOperationException( "KinsokuDepth must be positive value ("+value+" was specified)" );
				_KinsokuDepth = value;
			}
		}
		#endregion

		#region Character classification
		/// <summary>
		/// Distinguishs character class and get classification delegate object for the class.
		/// </summary>
		static ClassifyCharProc ClassifyChar( Document doc, int index )
		{
			if( IsDigit(doc, index) )		return IsDigit;
			if( IsAlphabet(doc, index) )	return IsAlphabet;
			if( IsWhiteSpace(doc, index) )	return IsWhiteSpace;
			if( IsPunct(doc, index) )		return IsPunct;
			if( IsEolCode(doc, index) )		return IsEolCode;
			if( IsHiragana(doc, index) )	return IsHiragana;
			if( IsKatakana(doc, index) )	return IsKatakana;

			return IsUnknown;
		}

		static ClassifyCharProc IsAlphabet = delegate( Document doc, int index )
		{
			if( doc.Length <= index )
				return false;

			char ch = doc[index];

			// take care of some letters that are used for integer literals
			if( ch == 'x'
				&& 0 <= index-1
				&& doc[index-1] == '0' )
			{
				return false;
			}
			if( ch == 'f' || ch == 'l' || ch == 'i' || ch == 'j' )
			{
				if( 0 <= index-1
					&& ('0' <= doc[index-1] && doc[index-1] <= '9') )
				{
					if( index+1 < doc.Length && IsAlphabet(doc, index+1) )
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}

			// is alphabet?
			if( ((int)ch) < CodeTable.Length && CodeTable[(int)ch] == 1 )
				return true;
			if( 0xff21 <= ch && ch <= 0xff3a ) // full-width alphabets (1)
				return true;
			if( 0xff41 <= ch && ch <= 0xff5a ) // full-width alphabets (2)
				return true;

			return false;
		};

		// 1=Alphabet, 0=Other
		readonly static byte[] CodeTable = new byte[] {
			/*      0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F  */
			/*000*/ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/*001*/ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/*002*/ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/*003*/ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/*004*/ 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			/*005*/ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1,
			/*006*/ 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			/*007*/ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0,
			/*008*/ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/*009*/ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/*00A*/ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/*00B*/ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			/*00C*/ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			/*00D*/ 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1,
			/*00E*/ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			/*00F*/ 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1
		};

		static ClassifyCharProc IsDigit = delegate( Document doc, int index )
		{
			if( doc.Length <= index )
				return false;

			char ch = doc[index];
			
			// is digit?
			if( 0x30 <= ch && ch <= 0x39 ) // half-width digits
				return true;
			if( 0xff10 <= ch && ch <= 0xff19 ) // full-width digits
				return true;

			// dot for decimals
			if( ch == 0x2e )
			{
				if( 0 <= index-1 && '0' <= doc[index-1] && doc[index-1] <= '9' )
					return true;
				if( index+1 < doc.Length && '0' <= doc[index+1] && doc[index+1] <= '9' )
					return true;
			}
			
			// include some postfix alphabets
			// (i, j is for complex numbers in Python)
			if( (ch == 'f' || ch == 'l' || ch == 'i' || ch == 'j')
				&& 1 <= index )
			{
				char ch2 = doc[ index-1 ];
				if( '0' <= ch2 && ch2 <= '9' )
				{
					if( index+1 < doc.Length && IsAlphabet(doc, index+1) )
					{
						return false;
					}
					else
					{
						return true;
					}
				}
			}

			// include '#' of '#fff'
			if( ch == '#'
				&& index+1 < doc.Length
				&& ('0' <= doc[index+1] && doc[index+1] <= '9'
					|| 'A' <= doc[index+1] && doc[index+1] <= 'Z'
					|| 'a' <= doc[index+1] && doc[index+1] <= 'a') )
			{
				return true;
			}

			// include 'x' of '0x'
			if( ch == 'x'
				&& 1 <= index && doc[index-1] == '0' )
			{
				return true;
			}

			// include hexadecimal literals
			if( ('a' <= ch && ch <= 'f' || 'A' <= ch && ch <= 'F')
				&& 2 <= index )
			{
				// find "0x" backward
				int i = index;
				int limit = Math.Max( 1, index-MaxHexIntLiteralLen );
				do
				{
					i--;
					ch = doc[i];
					if( ch == 'x' && doc[i-1] == '0' )
					{
						return true;
					}
					else if( ch == '#' )
					{
						return true;
					}
				}
				while( limit < i
					&& ('a' <= ch && ch <= 'f' || 'A' <= ch && ch <= 'F' || '0' <= ch && ch <= '9') );
			}

			return false;
		};

		static ClassifyCharProc IsPunct = delegate( Document doc, int index )
		{
			if( doc.Length <= index )
				return false;

			char ch = doc[index];
			
			if( ch == 0x5f ) // exclude '_'; is treated as an alphabet
				return false;

			if( 0x21 <= ch && ch <= 0x2f )
				return true;
			if( 0x3a <= ch && ch <= 0x40 )
				return true;
			if( 0x5b <= ch && ch <= 0x60 )
				return true;
			if( 0x7b <= ch && ch <= 0x7f )
				return true;
			if( 0x3001 <= ch && ch <= 0x303f && ch != 0x3005 )
				return true; // CJK punctuation marks except Ideographic iteration mark
			if( ch == 0x30fb )
				return true; // Katakana middle dot
			if( 0xff01 <= ch && ch <= 0xff0f )
				return true; // "Full width" forms (1)
			if( 0xff1a <= ch && ch <= 0xff20 )
				return true; // "Full width" forms (2)
			if( 0xff3b <= ch && ch <= 0xff40 )
				return true; // "Full width" forms (3)
			if( 0xff5b <= ch && ch <= 0xff65 )
				return true; // "Full width" forms (4)
			if( 0xffe0 <= ch && ch <= 0xffee )
				return true; // "Full width" forms (5)
			
			return false;
		};

		static ClassifyCharProc IsWhiteSpace = delegate( Document doc, int index )
		{
			if( doc.Length <= index )
				return false;

			char ch = doc[index];

			if( ch == 0x0a || ch == 0x0d ) // exclude EOL chars
				return false;
			if( 0x00 <= ch && ch <= 0x20 )
				return true;
			if( ch == 0x3000 ) // full-width space
				return true;

			return false;
		};

		static ClassifyCharProc IsEolCode = delegate( Document doc, int index )
		{
			if( doc.Length <= index )
				return false;

			char ch = doc[index];

			if( ch == 0x0a || ch == 0x0d )
				return true;
			
			return false;
		};

		static ClassifyCharProc IsHiragana = delegate( Document doc, int index )
		{
			if( doc.Length <= index )
				return false;

			char ch = doc[index];

			/*if( ch == 0x30fc ) // cho-inn
				return true;*/
			if( 0x3041 <= ch && ch <= 0x309f )
				return true;
			
			return false;
		};

		static ClassifyCharProc IsKatakana = delegate( Document doc, int index )
		{
			if( doc.Length <= index )
				return false;

			char ch = doc[index];

			if( ch == 0x30fb )
				return false; // Katakana middle dot is punctuation mark
			if( 0x30a0 <= ch && ch <= 0x30ff )
				return true;
			
			return false;
		};

		static ClassifyCharProc IsUnknown = delegate( Document doc, int index )
		{
			if( doc.Length <= index )
				return false;

			if( IsDigit(doc, index) )
				return false;
			if( IsAlphabet(doc, index) )
				return false;
			if( IsWhiteSpace(doc, index) )
				return false;
			if( IsPunct(doc, index) )
				return false;
			if( IsEolCode(doc, index) )
				return false;
			if( IsHiragana(doc, index) )
				return false;
			if( IsKatakana(doc, index) )
				return false;
			
			return true;
		};
		#endregion
	}
}
