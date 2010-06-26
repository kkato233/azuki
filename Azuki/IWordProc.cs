// file: IWordProc.cs
// brief: Interface of word processor objects.
// author: YAMAMOTO Suguru
// update: 2010-06-26
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Interface of word processor objects that handles 'word' in Azuki.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Word processor is used to detemrine boundary of words
	/// or to determine how words at right edge of text area should be wrapped.
	/// Typical usage of word processor is moving caret between words,
	/// selecting or deleting words, and line wrapping.
	/// </para>
	/// <para>
	/// Azuki provides only one built-in word processor -
	/// <see cref="Sgry.Azuki.DefaultWordProc">DefaultWordProc</see>.
	/// DefaultWordProc is used by default and is capable to handle
	/// Japanese kinsoku-shori and general word wrapping.
	/// If you need to change how Azuki recognizes words,
	/// implement an original word processor and then set it to
	/// <see cref="Sgry.Azuki.Document.WordProc">Document.WordProc</see>
	/// property.
	/// To implement a word processor,
	/// <see cref="Sgry.Azuki.DefaultWordProc">DefaultWordProc</see>
	/// can be used as a base class.
	/// </para>
	/// </remarks>
	/// <seealso cref="Sgry.Azuki.DefaultWordProc">DefaultWordProc class</seealso>
	/// <seealso cref="Sgry.Azuki.Document.WordProc">Document.WordProc property</seealso>
	public interface IWordProc
	{
		#region IWordProc - Properties
		/// <summary>
		/// Gets or sets whether to avoid wrapping screen lines
		/// in the middle of an alphabet word or not.
		/// </summary>
		bool EnableWordWrap
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to restrict characters which can end a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If this property was set true,
		/// Azuki tries to avoid placing characters specified by
		/// <see cref="Sgry.Azuki.IWordProc.CharsForbiddenToEndLine">
		/// CharsForbiddenToEndLine property</see>
		/// at ends of a screen line.
		/// This is one of the restriction rules in kinsoku shori.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IWordProc.CharsForbiddenToEndLine">IWordProc.CharsForbiddenToEndLine property</seealso>
		bool EnableLineEndRestriction
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets characters which are forbidden to end a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property is a set of characters.
		/// All characters included in the value
		/// will be avoided to be placed at the end of a screen line.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IWordProc.EnableLineEndRestriction">IWordProc.EnableLineEndRestriction property</seealso>
		char[] CharsForbiddenToEndLine
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to restrict characters which can start a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If this property was set true,
		/// Azuki tries to avoid placing characters specified by
		/// <see cref="Sgry.Azuki.IWordProc.CharsForbiddenToStartLine">
		/// CharsForbiddenToStartLine property</see>
		/// at start of a screen line.
		/// This is one of the restriction rules in kinsoku shori.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IWordProc.CharsForbiddenToStartLine">IWordProc.CharsForbiddenToStartLine property</seealso>
		bool EnableLineHeadRestriction
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets characters which are forbidden to start a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property is a set of characters.
		/// All characters included in the value
		/// will be avoided to be placed at start of a screen line.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IWordProc.EnableLineHeadRestriction">IWordProc.EnableLineHeadRestriction property</seealso>
		char[] CharsForbiddenToStartLine
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to 'hang' specified characters on the end of screen lines.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether to 'hang'
		/// specified characters on the end of screen lines.
		/// </para>
		/// <para>
		/// The term 'hang' here means placing a character beyond the end of screen line.
		/// Hanged character will be drawn out of text area
		/// and not be sent to next screen line.
		/// </para>
		/// <para>
		/// Which characters are hanged is determined by
		/// <see cref="Sgry.Azuki.IWordProc.CharsToBeHanged">CharsToBeHanged property</see>.
		/// To hang EOL code graphics, use 
		/// <see cref="Sgry.Azuki.IWordProc.EnableEolHanging">EnableEolHanging</see>
		/// instead.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IWordProc.CharsToBeHanged">IWordProc.CharsToBeHanged property</seealso>
		/// <seealso cref="Sgry.Azuki.IWordProc.EnableEolHanging">IWordProc.EnableEolHanging property</seealso>
		bool EnableCharacterHanging
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets characters which will be 'hanged' on a screen line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property is a set of characters.
		/// All characters included in the value
		/// will be avoided to be placed at start of a screen line,
		/// and will be 'hanged.'
		/// The term 'hang' here means placing a character beyond the end of screen line.
		/// Hanged character will be drawn out of text area
		/// and will not be the starting character of the next screen line.
		/// </para>
		/// <para>
		/// Note that CR (U+000d) and LF (U+000a)
		/// must not be included in the value of this property.
		/// To hang graphics of CR, LF, or CR+LF, use
		/// <see cref="Sgry.Azuki.IWordProc.EnableEolHanging">EnableEolHanging</see>.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IWordProc.EnableCharacterHanging">IWordProc.EnableCharacterHanging property</seealso>
		/// <seealso cref="Sgry.Azuki.IWordProc.EnableEolHanging">IWordProc.EnableEolHanging property</seealso>
		char[] CharsToBeHanged
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to 'hang' EOL characters on the end of screen lines.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether to 'hang'
		/// EOL graphics on the end of screen lines.
		/// </para>
		/// <para>
		/// The term 'hang' here means placing a character beyond the end of screen line.
		/// Hanged character will be drawn out of text area
		/// and will not be the starting character of the next screen line.
		/// </para>
		/// <para>
		/// To hang characters which is not EOL code, use 
		/// <see cref="Sgry.Azuki.IWordProc.EnableCharacterHanging">EnableCharacterHanging</see>
		/// instead.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IWordProc.EnableCharacterHanging">IWordProc.EnableCharacterHanging property</seealso>
		bool EnableEolHanging
		{
			get; set;
		}
		#endregion

		/// <summary>
		/// Searches document for start position of a word.
		/// </summary>
		/// <param name="doc">The document in which to search.</param>
		/// <param name="startIndex">The index to start the search from.</param>
		/// <returns>Index of start position of a word if found, or length of the document if no word was found.</returns>
		/// <remarks>
		/// <para>
		/// This method searches <paramref name="doc"/> for a word
		/// between <paramref name="startIndex"/> and the end of document.
		/// If a word was found, this method returns
		/// the start position of the word, 
		/// or returns the length of the document if no word was found.
		/// </para>
		/// <para>
		/// All word processor implementations must meet the requirements next.
		/// </para>
		/// <list type="bullet">
		///		<item>
		///		returns <paramref name="startIndex"/>
		///		if a word starts at <paramref name="startIndex"/>.
		///		</item>
		///		<item>
		///		returns the length of document if no word was found.
		///		</item>
		/// </list>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		<paramref name="startIndex"/> is less than 0 or greater than length of the document.
		/// </exception>
		int NextWordStart( Document doc, int startIndex );

		/// <summary>
		/// Searches document for end position of a word.
		/// </summary>
		/// <param name="doc">The document in which to search.</param>
		/// <param name="startIndex">The index to start the search from.</param>
		/// <returns>Index of end position of a word if found, or length of the document if no word was found.</returns>
		/// <remarks>
		/// <para>
		/// This method searches <paramref name="doc"/> for a word
		/// between <paramref name="startIndex"/> and the end of document.
		/// If a word was found, this method returns
		/// the end position of the word, 
		/// or returns the length of the document if no word was found.
		/// </para>
		/// <para>
		/// All word processor implementations must meet the requirements next.
		/// </para>
		/// <list type="bullet">
		///		<item>
		///		returns <paramref name="startIndex"/>
		///		if a word ends at <paramref name="startIndex"/>.
		///		</item>
		///		<item>
		///		returns the length of document if no word was found.
		///		</item>
		/// </list>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		<paramref name="startIndex"/> is less than 0 or greater than length of the document.
		/// </exception>
		int NextWordEnd( Document doc, int startIndex );

		/// <summary>
		/// Searches document backward for start position of a word.
		/// </summary>
		/// <param name="doc">The document in which to search.</param>
		/// <param name="startIndex">The index to start the search from.</param>
		/// <returns>Index of start position of a word if found, or 0 if no word was found.</returns>
		/// <remarks>
		/// <para>
		/// This method searches <paramref name="doc"/> for a word
		/// between <paramref name="startIndex"/> and the end of document.
		/// If a word was found, this method returns
		/// the start position of the word,
		/// or returns 0 if no word was found.
		/// </para>
		/// <para>
		/// All word processor implementations must meet the requirements next.
		/// </para>
		/// <list type="bullet">
		///		<item>
		///		returns <paramref name="startIndex"/>
		///		if a word starts at <paramref name="startIndex"/>.
		///		</item>
		///		<item>
		///		returns 0 if no word was found.
		///		</item>
		/// </list>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		<paramref name="startIndex"/> is less than 0 or greater than length of the document.
		/// </exception>
		int PrevWordStart( Document doc, int startIndex );

		/// <summary>
		/// Searches document backward for end position of a word.
		/// </summary>
		/// <param name="doc">The document in which to search.</param>
		/// <param name="startIndex">The index to start the search from.</param>
		/// <returns>Index of end position of a word if found, or length of the document if no word was found.</returns>
		/// <remarks>
		/// <para>
		/// This method searches <paramref name="doc"/> for a word
		/// between <paramref name="startIndex"/> and the end of document.
		/// If a word was found, this method returns
		/// the end position of the word, 
		/// or returns 0 if no word was found.
		/// </para>
		/// <para>
		/// All word processor implementations must meet the requirements next.
		/// </para>
		/// <list type="bullet">
		///		<item>
		///		returns <paramref name="startIndex"/>
		///		if a word ends at <paramref name="startIndex"/>.
		///		</item>
		///		<item>
		///		returns 0 if no word was found.
		///		</item>
		/// </list>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		<paramref name="startIndex"/> is less than 0 or greater than length of the document.
		/// </exception>
		int PrevWordEnd( Document doc, int startIndex );

		/// <summary>
		/// Determines where a screen line should be wrapped at.
		/// </summary>
		/// <param name="doc">The document currently rendering.</param>
		/// <param name="index">The index of character which is to be drawn over the right edge of text area.</param>
		/// <returns>The index of the character which starts the next screen line.</returns>
		/// <remarks>
		/// <para>
		/// This method is used to determine where Azuki should wrap a screen line at.
		/// </para>
		/// <para>
		/// Azuki calls this method
		/// everytime the graphic of a screen line reaches to the right edge of the text area.
		/// If a valid index is returned from this method,
		/// the text line will be 'wrapped'
		/// - characters at the index and after will be drawn as a new screen line.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">
		///		<paramref name="doc"/> is null.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		<paramref name="index"/> is less than 0 or greater than length of the document.
		/// </exception>
		int HandleWordWrapping( Document doc, int index );
	}
}
