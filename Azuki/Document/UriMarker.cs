// file: UriMarker.cs
// brief: a singleton class which marks URIs up in document.
// author: YAMAMOTO Suguru
// update: 2011-06-26
//=========================================================
using System;
using System.Text;
using System.Collections.Generic;
using UnicodeCategory = System.Globalization.UnicodeCategory;

namespace Sgry.Azuki
{
	/// <summary>
	/// Parser to mark URIs up in Azuki document.
	/// </summary>
	class UriMarker
	{
		#region Fields
		static UriMarker _Inst = null;
		static DefaultWordProc _WordProc = new DefaultWordProc();
		static string[] _SchemeTriggers = new string[] { "file://", "ftp://", "http://", "https://", "mailto:" };
		#endregion

		#region Static members
		/// <summary>
		/// Gets or sets list of URI scheme to trigger URI parsing;
		/// "http://" for instance.
		/// </summary>
		public static string[] Schemes
		{
			get{ return _SchemeTriggers; }
			set{ _SchemeTriggers = value; }
		}

		/// <summary>
		/// Initializes static members.
		/// </summary>
		static UriMarker()
		{
			_WordProc.EnableCharacterHanging = false;
			_WordProc.EnableEolHanging = false;
			_WordProc.EnableLineEndRestriction = false;
			_WordProc.EnableLineHeadRestriction = false;
			_WordProc.EnableWordWrap = false;
		}

		/// <summary>
		/// Gets the singleton instance of UriMarker.
		/// </summary>
		public static UriMarker Inst
		{
			get
			{
				if( _Inst == null )
				{
					_Inst = new UriMarker();
				}
				return _Inst;
			}
		}
		#endregion

		private UriMarker()
		{}

		#region Event handlers
		public void HandleContentChanged( object sender, ContentChangedEventArgs e )
		{
			UiImpl ui = (UiImpl)sender;
			Document doc = ui.Document;
			int lineIndex;
			int lineHead, lineEnd;
			bool shouldBeRedrawn;

			if( doc.MarksUri == false )
				return;

			// update marking in this line
			lineIndex = doc.GetLineIndexFromCharIndex( e.Index );
			shouldBeRedrawn = MarkOrUnmarkOneLine( doc, lineIndex, true );
			if( shouldBeRedrawn )
			{
				// update entire graphic of the logical line
				// if marking bits associated with any character was changed
				lineHead = doc.GetLineHeadIndex( lineIndex );
				lineEnd = lineHead + doc.GetLineLength( lineIndex );
				ui.View.Invalidate( lineHead, lineEnd );
			}
		}

		public void UI_LineDrawing( object sender, LineDrawEventArgs e )
		{
			IUserInterface ui = (IUserInterface)sender;

			// Even if the URI marking is disabled, scanning procedure must be done because
			// characters marked as URI already must be unmarked after disabling URI marking.
			/*DO_NOT -->
			if( doc.MarksUri == false )
				return;
			<-- DO_NOT*/

			// mark up all URIs in the logical line
			int scrernLineHeadIndex = ui.View.GetLineHeadIndex( e.LineIndex );
			int logicalLineIndex = ui.Document.GetLineIndexFromCharIndex( scrernLineHeadIndex );
			e.ShouldBeRedrawn = MarkOrUnmarkOneLine( ui.Document, logicalLineIndex, ui.MarksUri );
		}
		#endregion

		#region Marking logic
		/// <summary>
		/// Marks URIs in a logical line.
		/// </summary>
		/// <returns>Whether specified line should be redrawn or not.</returns>
		bool MarkOrUnmarkOneLine( Document doc, int logicalLineIndex, bool marks )
		{
			DebugUtl.Assert( doc != null );
			DebugUtl.Assert( 0 <= logicalLineIndex );
			DebugUtl.Assert( logicalLineIndex < doc.LineCount );

			int lineBegin, lineEnd;
			int lastMarkedIndex;
			int seekIndex;
			int changeCount = 0;

			// first of all, do nothing if document is empty.
			if( doc.Length == 0 )
			{
				return false;
			}

			// prepare scanning
			lineBegin = doc.GetLineHeadIndex( logicalLineIndex );
			lineEnd = lineBegin + doc.GetLineLength( logicalLineIndex );
			if( lineBegin == lineEnd )
			{
				return false; // this is an empty line.
			}

			// scan and mark all URIs in the line
			lastMarkedIndex = lineBegin;
			seekIndex = lineBegin;
			while( 0 <= seekIndex && seekIndex < lineEnd )
			{
				// mark URI if one starts from here
				if( SchemeStartsFromHere(doc, seekIndex) )
				{
					bool isMailAddress;
					int uriEnd = GetUriEnd( doc, seekIndex, out isMailAddress );
					if( 0 < uriEnd )
					{
						// clear marking before this URI part
						if( lastMarkedIndex < seekIndex )
						{
							changeCount += doc.Unmark( lastMarkedIndex, seekIndex, Marking.Uri )
										   ? 1 : 0;
						}

						// mark the URI part
						if( marks )
						{
							changeCount += doc.Mark( seekIndex, uriEnd, Marking.Uri )
										   ? 1 : 0;
						}
						else
						{
							changeCount += doc.Unmark( seekIndex, uriEnd, Marking.Uri )
										   ? 1 : 0;
						}

						// update seek position
						lastMarkedIndex = uriEnd;
						seekIndex = uriEnd;
						if( doc.Length <= seekIndex )
						{
							DebugUtl.Assert( seekIndex == doc.Length );
							break;
						}
					}
				}

				// skip to next word
				seekIndex = _WordProc.NextWordStart( doc, seekIndex+1 );
			}

			// clear marking of remaining characters
			if( lastMarkedIndex < lineEnd )
			{
				changeCount += doc.Unmark( lastMarkedIndex, lineEnd, Marking.Uri )
							   ? 1 : 0;
			}

			return (0 < changeCount);
		}

		public int GetUriEnd( Document doc, int startIndex, out bool isMailAddress )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( startIndex < 0 || doc.Length < startIndex )
				throw new ArgumentOutOfRangeException( "startIndex" );

			int index = startIndex;
			int lineEnd;
			char ch;
			StringBuilder scheme = new StringBuilder( 8 );

			// prepare parsing
			isMailAddress = false;
			lineEnd = doc.GetLineHeadIndexFromCharIndex( startIndex );
			lineEnd += doc.GetLineLength( doc.GetLineIndexFromCharIndex(startIndex) );
			DebugUtl.Assert( lineEnd <= doc.Length );

		//scheme:
			// parse first character of scheme part
			if( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
				{
					return -1;
				}
				if( ch == '/' || ch == '?' || ch == '#' || ch == ':' )
				{
					return -1;
				}
				scheme.Append( ch );

				index++;
			}
			else
			{
				return -1;
			}

			// parse remainings of scheme part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
				{
					return -1;
				}
				if( ch == '/' || ch == '?' || ch == '#' )
				{
					return -1;
				}
				if( ch == ':' )
				{
					break;
				}
				scheme.Append( ch );

				index++;
			}
			if( lineEnd <= index )
			{
				return -1;
			}

		//colon:
			// parse colon part
			DebugUtl.Assert( doc[index] == ':' );
			index++;

			// if scheme is mailto, switch to mail address specific logic
			if( scheme.ToString() == "mailto" )
			{
				isMailAddress = true;
				return GetMailToEnd( doc, index );
			}

		//slash-1:
			// parse slash part
			if( index < lineEnd )
			{
				ch = doc[ index ];
				if( ch != '/' )
				{
					return -1;
				}

				index++;
			}
			else
			{
				return -1;
			}

		//slash-2:
			// parse slash part
			if( index < lineEnd )
			{
				ch = doc[ index ];
				if( ch != '/' )
				{
					return -1;
				}

				index++;
			}
			else
			{
				return -1;
			}

		//authority:
			// parse first character of authority part
			if( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
				{
					return -1;
				}

				index++;
			}
			else
			{
				return -1;
			}

			// parse remainings of authority part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
				{
					return index;
				}
				if( ch == '/' )
				{
					break; //goto path;
				}
				if( ch == '?' )
				{
					goto query;
				}
				if( ch == '#' )
				{
					goto fragment;
				}

				index++;
			}

		//path:
			// parse path part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
				{
					return index;
				}
				if( ch == '?' )
				{
					break; //goto query;
				}
				if( ch == '#' )
				{
					goto fragment;
				}

				index++;
			}

		query:
			// parse query part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
				{
					return index;
				}
				if( ch == '#' )
				{
					break; //goto fragment;
				}

				index++;
			}

		fragment:
			// parse fragment part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
				{
					return index;
				}
				 
				index++;
			}

			return index;
		}

		static bool GetUriEnd_ValidChar( char ch )
		{
			if( ch <= 0x7f )
			{
				if( 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z' // alpha
					|| '0' <= ch && ch <= '9' // digit
					|| 0 <= "./_-?&=#%~!$*+,:;@\\^|".IndexOf(ch) )
				{
					return true;
				}
				return false;
			}
			else
			{
				UnicodeCategory cat = Char.GetUnicodeCategory( ch );
				if( cat == UnicodeCategory.ClosePunctuation
					|| cat == UnicodeCategory.OpenPunctuation
					|| cat == UnicodeCategory.ParagraphSeparator
					|| cat == UnicodeCategory.SpaceSeparator
					|| cat == UnicodeCategory.Format
					|| 0 <= "\x3001\x3002".IndexOf(ch) )
				{
					return false;
				}
				return true;
			}
		}

		public int GetMailToEnd( Document doc, int startIndex )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( startIndex < 0 || doc.Length < startIndex )
				throw new ArgumentOutOfRangeException( "startIndex" );

			int index = startIndex;
			int lineEnd;
			char ch;

			if( doc.Length <= startIndex )
				return -1;

			// prepare parsing
			int lineHeadIndex = doc.GetLineHeadIndexFromCharIndex( startIndex );
			lineEnd = lineHeadIndex + doc.GetLineLengthFromCharIndex( lineHeadIndex );
			DebugUtl.Assert( lineEnd <= doc.Length );

		//local-part:
			if( index < lineEnd )
			{
				ch = doc[index];
				if( GetMailToEnd_IsLocalPartChar(ch) == false )
				{
					return -1;
				}

				index++;
			}
			while( index < lineEnd )
			{
				ch = doc[index];
				if( ch == '@' )
				{
					break;
				}
				if( GetMailToEnd_IsLocalPartChar(ch) == false )
				{
					return -1;
				}

				index++;
			}
			if( lineEnd <= index )
			{
				return -1;
			}

		//at-mark:
			DebugUtl.Assert( doc[index] == '@' );
			index++;

		//domain:
			// parse first character of domain part
			if( index < lineEnd )
			{
				ch = doc[index];
				if( GetMailToEnd_IsDomainChar(ch) == false )
				{
					return -1;
				}

				index++;
			}
			else
			{
				return -1;
			}

			// parse remainings of domain part
			while( index < lineEnd )
			{
				ch = doc[index];
				if( GetMailToEnd_IsDomainChar(ch) == false )
				{
					return index;
				}

				index++;
			}

			return index;
		}

		static bool GetMailToEnd_IsLocalPartChar( char ch )
		{
			return ('0' <= ch && ch <= '9')
					|| ('A' <= ch && ch <= 'Z')
					|| 'a' <= ch && ch <= 'z'
					|| 0 <= ".-_!#$%&'*+/=?^`{|}~".IndexOf(ch);
		}

		static bool GetMailToEnd_IsDomainChar( char ch )
		{
			return ('A' <= ch && ch <= 'Z')
				|| ('a' <= ch && ch <= 'z')
				|| ('0' <= ch && ch <= '9')
				|| (0 <= "-.:[]".IndexOf(ch));
		}
		#endregion

		#region Utilities
		bool SchemeStartsFromHere( Document doc, int index )
		{
			DebugUtl.Assert( doc != null );
			DebugUtl.Assert( 0 <= index );
			DebugUtl.Assert( index < doc.Length );

			foreach( string scheme in _SchemeTriggers )
			{
				if( StartsWith(doc, index, scheme) )
				{
					return true;
				}
			}

			return false;
		}

		bool StartsWith( Document doc, int index, string text )
		{
			DebugUtl.Assert( doc != null );
			DebugUtl.Assert( 0 <= index );
			DebugUtl.Assert( index < doc.Length );
			DebugUtl.Assert( text != null );

			for( int i=0; i<text.Length; i++ )
			{
				if( doc.Length <= (index + i)
					|| text[i] != doc[index+i] )
				{
					return false;
				}
			}

			return true;
		}
		#endregion
	}
}
