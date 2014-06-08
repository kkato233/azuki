namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Interface of highlighter object for Azuki.
	/// </summary>
	/// <remarks>
	///   <para>
	///   This interface is commonly used by highlighter objects which are used to highlight syntax
	///   of documents.
	///   </para>
	///   <para>
	///   If a highlighter object is set for a document,
	///   IHighlighter.<see cref="IHighlighter.Highlight"/> method will be called on every time
	///   slightly after the user stopped editing. Since the method is called with parameters
	///   indicating where to begin highlighting and where to end highlighting, highlighting will
	///   not process entire document.
	///   </para>
	/// </remarks>
	public interface IHighlighter
	{
		/// <summary>
		/// Highlights a part of a document.
		/// </summary>
		/// <param name="doc">Document to highlight.</param>
		/// <param name="dirtyBegin">
		///   Index to start highlighting. On return, start index of the range to be invalidated.
		/// </param>
		/// <param name="dirtyEnd">
		///   Index to end highlighting. On return, end index of the range to be invalidated.
		/// </param>
		void Highlight( Document doc, ref int dirtyBegin, ref int dirtyEnd );

		/// <summary>
		/// Gets or sets whether the hook mechanism is supported or not.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets or sets whether this highlighter object supports hook mechanism or
		///   not. Please refer to the document of <see cref="IHighlighter.HookProc"/> property
		///   about hook mechanism.
		///   </para>
		/// </remarks>
		/// <seealso cref="IHighlighter.HookProc">IHighlighter.HookProc property</seealso>
		bool CanUseHook
		{
			get;
		}

		/// <summary>
		/// Gets or sets highlighter hook procedure.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets or sets a hook procedure to override highlight logic built into
		///   the highlighter object. A delegate object set to this property will be called when a
		///   token is highlighted and if the delegate returns true, the highlighter will skip
		///   highlighting the token; so the delegate can highlight tokens differently.
		///   </para>
		///   <para>
		///   It is not needed to implement highlight hook for all highlighters so accessing this
		///   property may throw a NotSupportedException depending on implementations. If an
		///   implementation of IHighlighter does not provide hook mechanism, its CanHook property 
		///   SHOULD returns false and accessing this property SHOULD throw a
		///   NotSupportedException.
		///   </para>
		///   <para>
		///   One of the typical usage is changing character class for specific keywords for
		///   application specific reason. Another typical usage is expanding logic of a keyword
		///   based highlighter to consider language syntax a little more (example of this usage is
		///   built-in C/C++ highlighter which uses a hook procedure to expand logic for
		///   highlighting preprocessor macros whose '#' and keyword parts are separated with
		///   spaces.) Note that since this functionality is a hook, a very little change can be
		///   applied to original behavior. If needed highlighting result cannot be easily achieved
		///   with a hook, consider implementing a new IHighlighter from a scratch.
		///   </para>
		/// </remarks>
		/// <exception cref="System.NotSupportedException">
		/// This highlighter does not support hook mechanism.
		/// </exception>
		/// <seealso cref="IHighlighter.CanUseHook">IHighlighter.CanUseHook property</seealso>
		/// <seealso cref="HighlightHook">HighlightHook delegate</seealso>
		HighlightHook HookProc
		{
			get; set;
		}
	}

	/// <summary>
	/// The type of the hook to override
	/// default procedure to highlight a token.
	/// </summary>
	/// <param name="doc">The document to be highlighted.</param>
	/// <param name="token">The substring to be highlighted.</param>
	/// <param name="index">The index of where the token is at.</param>
	/// <param name="klass">
	///   The character class which the token is to be classified as, by the highlighter.
	/// </param>
	/// <returns>
	///   Return true if default behavior of the highlighter should be suppressed,
	///   otherwise return false.
	/// </returns>
	/// <remarks>
	///   <para>
	///   Please refer to the document of IHighlighter.<see cref="IHighlighter.HookProc" />
	///   property about hook mechanism.
	///   </para>
	/// </remarks>
	/// <seealso cref="IHighlighter.HookProc">IHighlighter.HookProc property</seealso>
	public delegate bool HighlightHook( Document doc, string token, int index, CharClass klass );
}
