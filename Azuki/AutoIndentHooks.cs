// file: AutoIndentLogic.cs
// brief: Logic around auto-indentation.
//=========================================================
using System;
using System.Text;
using Point = System.Drawing.Point;

namespace Sgry.Azuki
{
	/// <summary>
	/// Hook delegate called every time a character was inserted.
	/// </summary>
	/// <param name="ui">User interface object such as AzukiControl.</param>
	/// <param name="ch">The character to be inserted.</param>
	/// <returns>
	/// Whether the hook handles input successfully or not.
	/// </returns>
	/// <remarks>
	///   <para>
	///   AutoIndentHook is the type of delegate which is used to override (hook and change)
	///   Azuki's built-in input handling logic. If a hook of this type was installed, it is called
	///   every time a character is inserted and if it returned true, Azuki suppresses built-in
	///   input handling logic.
	///   </para>
	/// </remarks>
	/// <seealso cref="IUserInterface.AutoIndentHook"/>
	/// <seealso cref="AutoIndentHooks"/>
	public delegate bool AutoIndentHook( IUserInterface ui, char ch );

	/// <summary>
	/// Static class containing built-in hook delegates for auto-indentation.
	/// </summary>
	/// <seealso cref="IUserInterface.AutoIndentHook"/>
	/// <seealso cref="WinForms.AzukiControl.AutoIndentHook"/>
	public static class AutoIndentHooks
	{
		/// <summary>
		/// Basic auto-indent hook.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This hook executes most basic auto-indentation; it just copies previous indentation
		///   characters every time the user creates a new line.
		///   </para>
		/// </remarks>
		public static readonly AutoIndentHook GenericHook = delegate( IUserInterface ui, char ch )
		{
			Document doc = ui.Document;
			StringBuilder str = new StringBuilder();
			int lineHead;
			int newCaretIndex;

			// do nothing if Azuki is in single line mode
			if( ui.IsSingleLineMode )
			{
				return false;
			}

			// if EOL code was detected, perform indentation
			if( LineLogic.IsEolChar(ch) )
			{
				str.Append( doc.EolCode );

				// get indent chars
				lineHead = doc.GetLineHeadIndexFromCharIndex( doc.CaretIndex );
				for( int i=lineHead; i<doc.CaretIndex; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' || doc[i] == '\x3000' )
						str.Append( doc[i] );
					else
						break;
				}

				// replace selection
				newCaretIndex = Math.Min( doc.AnchorIndex, doc.CaretIndex ) + str.Length;
				doc.Replace( str.ToString() );
				doc.SetSelection( newCaretIndex, newCaretIndex );

				return true;
			}

			return false;
		};

		/// <summary>
		/// Auto-indent hook for C styled source code.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This hook delegate provides a special indentation logic for C styled source code.
		///   Here 'C style' means that curly brackets are used to enclose each logical block, such
		///   as C++, Java, C#, and so on.
		///   </para>
		///   <para>
		///   The differences between this and generic auto-indentation are below:
		///   </para>
		///   <list type="bullet">
		///     <item>
		///     Pressing Enter key increases indentation level if the line was terminated with a
		///     closing curly bracket (<c> } </c>)
		///     </item>
		///     <item>
		///     Inserting an opening curly bracket (<c> { </c>) decreases indentation level if the
		///     line was consisted only with whitespace characters.
		///     </item>
		///   </list>
		///   <para>
		///	  Note that the characters to be used to create indentation will be chosen according to
		///	  the value of <see cref="IUserInterface.UsesTabForIndent"/> property.
		///   </para>
		/// </remarks>
		public static readonly AutoIndentHook CHook = delegate( IUserInterface ui, char ch )
		{
			Document doc = ui.Document;
			StringBuilder indentChars = new StringBuilder( 64 );
			int lineHead, lineEnd;
			int newCaretIndex;
			int selBegin, selEnd;
			int selBeginL;
			
			doc.GetSelection( out selBegin, out selEnd );
			selBeginL = doc.GetLineIndexFromCharIndex( selBegin );

			// calculate line head and line end
			lineHead = doc.GetLineHeadIndex( selBeginL );
			if( selBeginL+1 < doc.LineCount )
			{
				lineEnd = doc.GetLineHeadIndex( selBeginL+1 );
			}
			else
			{
				lineEnd = doc.Length;
			}

			// user hit Enter key?
			if( LineLogic.IsEolChar(ch) )
			{
				int i;
				bool extraPaddingNeeded = false;

				// do nothing if it's in single line mode
				if( ui.IsSingleLineMode )
				{
					return false;
				}

				indentChars.Append( doc.EolCode );

				// if the line is empty, do nothing
				if( lineHead == lineEnd )
				{
					return false;
				}

				// get indent chars
				for( i=lineHead; i<selBegin; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' )
						indentChars.Append( doc[i] );
					else
						break;
				}

				// if there are following white spaces, remove them
				for( i=selEnd; i<lineEnd; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' || doc[i] == '\x3000' )
						selEnd++;
					else
						break;
				}

				// determine whether extra padding is needed or not
				// (because replacement changes line end index
				// determination after replacement will be much harder)
				if( Utl.FindPairedBracket_Backward(doc, selBegin, lineHead, '}', '{') != -1
					&& Utl.IndexOf(doc, '}', selBegin, lineEnd) == -1 )
				{
					extraPaddingNeeded = true;
				}

				// replace selection
				newCaretIndex = Math.Min( doc.AnchorIndex, selBegin ) + indentChars.Length;
				doc.Replace( indentChars.ToString(), selBegin, selEnd );

				// if there is a '{' without pair before caret
				// and is no '}' after caret, add indentation
				if( extraPaddingNeeded )
				{
					// make indentation characters
					string extraPadding;
					Point pos = ui.View.GetVirPosFromIndex( newCaretIndex );
					pos.X += ui.View.TabWidthInPx;
					extraPadding = UiImpl.GetNeededPaddingChars( ui, pos, true );
					doc.Replace( extraPadding, newCaretIndex, newCaretIndex );
					newCaretIndex += extraPadding.Length;
				}

				doc.SetSelection( newCaretIndex, newCaretIndex );

				return true;
			}
			// user hit '}'?
			else if( ch == '}' )
			{
				int pairIndex, pairLineHead, pairLineEnd;
				int pairLineIndex;

				// ensure this line contains only white spaces
				for( int i=lineHead; i<lineEnd; i++ )
				{
					if( LineLogic.IsEolChar(doc[i]) )
					{
						break;
					}
					else if( doc[i] != ' ' && doc[i] != '\t' )
					{
						return false; // this line contains a non white space char
					}
				}

				// find the paired open bracket
				pairIndex = Utl.FindPairedBracket_Backward( doc, selBegin, 0, '}', '{' );
				if( pairIndex == -1 )
				{
					return false; // no pair exists. nothing to do
				}
				
				// get indent char of the line where the pair exists
				pairLineIndex = ui.GetLineIndexFromCharIndex( pairIndex );
				pairLineHead = ui.GetLineHeadIndex( pairLineIndex );
				pairLineEnd = pairLineHead + ui.GetLineLength( pairLineIndex );
				for( int i=pairLineHead; i<pairLineEnd; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' )
						indentChars.Append( doc[i] );
					else
						break;
				}
				
				// replace indent chars of current line
				indentChars.Append( '}' );
				doc.Replace( indentChars.ToString(), lineHead, selBegin );
				
				return true;
			}

			return false;
		};

		/// <summary>
		/// Auto-indent hook for Python script.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This hook delegate provides a special indentation logic for Python programming
		///   language.
		///   </para>
		///   <list type="bullet">
		///     <item>
		///     Pressing Enter key increases indentation level if the line was terminated with a
		///     colon (<c> : </c>).
		///     </item>
		///     <item>
		///     When the caret is in a middle of a paired parentheses, additional spaces will be
		///     inserted so that the caret's column position will be the same as the previous
		///     opening parenthesis. For example, if there is a code like next:
		///     <pre><code lang="Python">
		///     fruits = ('apple', 'orange')
		///     </code></pre>
		///     and pressing Enter when the caret is at one character ahead of a comma will result:
		///     <pre><code lang="Python">
		///     fruits = ('apple',
		///               'orange')
		///     </code></pre>
		///     </item>
		///   </list>
		///   <para>
		///	  Note that the characters to be used to create indentation will be chosen according to
		///	  the value of <see cref="IUserInterface.UsesTabForIndent"/> property.
		///   </para>
		/// </remarks>
		public static readonly AutoIndentHook PythonHook = delegate( IUserInterface ui, char ch )
		{
			// Do nothing if Azuki is in single line mode
			if( ui.IsSingleLineMode )
			{
				return false;
			}

			if( LineLogic.IsEolChar(ch) )
			{
				Document doc = ui.Document;
				View view = (View)ui.View;
				StringBuilder indentChars = new StringBuilder( doc.EolCode, 128 );

				// First of all, remove selected text
				doc.Replace( "" );

				int lineIndex = doc.GetLineIndexFromCharIndex( doc.CaretIndex );
				int lineBegin = doc.GetLineHeadIndex( lineIndex );
				int lineEnd = (lineIndex+1 < doc.LineCount) ? doc.GetLineHeadIndex( lineIndex + 1 )
															: doc.Length;

				// Determine whether an extra padding is needed
				bool levelUp = false;
				{
					for( int i=doc.CaretIndex-1; lineBegin <= i; i-- )
					{
						if( doc[i] == ':' )
						{
							levelUp = true;
							break;
						}
						else if( " \t".IndexOf(doc[i]) < 0 )
						{
							break;
						}
					}
				}

				int lastOpenParenIndex = -1;
				if( !levelUp )
				{
					char[] openers = new char[]{'(', '[', '{'};
					char[] closers = new char[]{')', ']', '}'};
					int[] levels = new int[3]{0, 0, 0};
					for( int i=doc.CaretIndex-1; lineBegin <= i; i-- )
					{
						int type = Array.IndexOf( openers, doc[i] );
						if( 0 <= type )
						{
							if( levels[type] == 0 )
							{
								lastOpenParenIndex = i;
								break;
							}
							else
							{
								levels[type]--;
							}
						}
						type = Array.IndexOf( closers, doc[i] );
						if( 0 <= type )
						{
							levels[type]++;
						}
					}
				}

				// Get indent chars
				for( int i=lineBegin; i<doc.CaretIndex; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' || doc[i] == '\x3000' )
						indentChars.Append( doc[i] );
					else
						break;
				}

				// Remove whitespaces just after the caret
				int extraSpaceCount = 0;
				for( int i=doc.CaretIndex; i<lineEnd; i++ )
				{
					if( 0 <= " \t".IndexOf(doc[i]) )
						extraSpaceCount++;
					else
						break;
				}

				// Replace selection
				int newCaretIndex = Math.Min( doc.AnchorIndex, doc.CaretIndex ) + indentChars.Length;
				doc.Replace( indentChars.ToString(), doc.CaretIndex, doc.CaretIndex+extraSpaceCount );
				doc.SetSelection( newCaretIndex, newCaretIndex );

				// Add indent level
				if( levelUp )
				{
					Point pos = view.GetVirPosFromIndex( doc.CaretIndex );
					pos.X += view.TabWidthInPx;
					indentChars.Length = 0;
					indentChars.Append( UiImpl.GetNeededPaddingChars(ui, pos, true) );
					doc.Replace( indentChars.ToString() );
					newCaretIndex += indentChars.Length;
					doc.SetSelection( newCaretIndex, newCaretIndex );
				}
				else if( 0 <= lastOpenParenIndex )
				{
					int index = lastOpenParenIndex + 1;
					while( index < lineEnd && (doc[index] == ' ' || doc[index] == '\t' ) )
						index++;
					int caretX = view.GetVirPosFromIndex( doc.CaretIndex ).X;
					int destX = view.GetVirPosFromIndex( index ).X;
					int spaceCount = (destX - caretX) / view.SpaceWidthInPx;
					indentChars.Length = 0;
					for( int i=0; i<spaceCount; i++ )
						indentChars.Append( ' ' );
					doc.Replace( indentChars.ToString() );
					newCaretIndex += indentChars.Length;
					doc.SetSelection( newCaretIndex, newCaretIndex );
				}

				return true;
			}

			return false;
		};

		#region Utilities
		static class Utl
		{
			public static int IndexOf( Document doc, char value, int startIndex, int endIndex )
			{
				for( int i=startIndex; i<endIndex; i++ )
				{
					if( doc[i] == value )
					{
						return i;
					}
				}

				return -1;
			}

			public static int LastIndexOf( Document doc, char value, int startIndex, int endIndex )
			{
				for( int i=startIndex-1; endIndex<=i; --i )
				{
					if( doc[i] == value )
						return i;
				}

				return -1;
			}

			public static int FindPairedBracket_Backward( Document doc, int startIndex, int endIndex, char bracket, char pairBracket )
			{
				int depth = 1;

				// seek backward until paired bracket was found
				for( int i=startIndex-1; endIndex<=i; i-- )
				{
					if( doc[i] == bracket && doc.IsCDATA(i) == false )
					{
						// a bracket was found.
						// increase depth count
						depth++;
					}
					else if( doc[i] == pairBracket && doc.IsCDATA(i) == false )
					{
						// a paired bracket was found.
						// decrease count and if the count fell down to zero,
						// return the position.
						depth--;
						if( depth == 0 )
						{
							return i; // found the pair
						}
					}
				}

				// not found
				return -1;
			}
		}
		#endregion
	}
}
