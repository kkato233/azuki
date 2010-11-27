// file: UriMarker.cs
// brief: a singleton class which marks URIs up in document.
// author: YAMAMOTO Suguru
// update: 2010-11-16
//=========================================================
using System;
using System.Text;
using System.Collections.Generic;

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
		static string[] _SchemeTriggers = new string[] { "ftp://", "http://", "https://", "mailto:" };
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

			// update marking in this line
			lineIndex = doc.GetLineIndexFromCharIndex( e.Index );
			shouldBeRedrawn = MarkOneLine( doc, lineIndex );
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
			Document doc = ((IUserInterface)sender).Document;
			e.ShouldBeRedrawn = MarkOneLine( doc, e.LineIndex );
		}
		#endregion

		#region Marking logic
		/// <summary>
		/// Mark one or more URIs in a logical line.
		/// </summary>
		/// <returns>Whether specified line should be redrawn or not.</returns>
		bool MarkOneLine( Document doc, int lineIndex )
		{
			DebugUtl.Assert( doc != null );
			DebugUtl.Assert( 0 <= lineIndex );
			DebugUtl.Assert( lineIndex < doc.LineCount );

			int lineBegin, lineEnd;
			int lastMarkedIndex;
			int seekIndex;
			bool shouldUpdate = false;

			// first of all, do nothing if document is empty.
			if( doc.Length == 0 )
			{
				return shouldUpdate;
			}

			// prepare scanning
			lineBegin = doc.GetLineHeadIndex( lineIndex );
			lineEnd = lineBegin + doc.GetLineLength( lineIndex );
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
					int uriEnd = UriMarker.Inst.GetUriEnd( doc, seekIndex, out isMailAddress );
					if( 0 < uriEnd )
					{
						// clear marking before this URI part
						if( lastMarkedIndex < seekIndex )
						{
							doc.Unmark( lastMarkedIndex, seekIndex, Marking.Uri );
						}

						// mark the URI part
						shouldUpdate = doc.Mark( seekIndex, uriEnd, Marking.Uri );

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
				shouldUpdate = doc.Unmark( lastMarkedIndex, lineEnd, Marking.Uri );
			}

			return shouldUpdate;
		}

		public int GetUriEnd( Document doc, int startIndex, out bool isMailAddress )
		{
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
			// parse scheme part
			while( index < lineEnd )
			{
				ch = doc[index++];
				if( GetUriEnd_ValidChar(ch) )
				{
					scheme.Append( ch );
					continue;
				}
				if( ch == '/' || ch == '?' || ch == '#' )
				{
					return -1;
				}
				if( ch == ':' )
				{
					break;
				}
			}

			// if scheme is mailto, switch to mail address specific logic
			if( index+1 < lineEnd
				&& scheme.ToString() == "mailto" )
			{
				isMailAddress = true;
				return GetMailToEnd( doc, index+1 );
			}

		//colon:
			// parse colon part
			if( index < lineEnd )
			{
				ch = doc[index++];
				if( GetUriEnd_ValidChar(ch) == false
					&& ch != '/' )
				{
					return -1;
				}
			}

		//slash:
			// parse slash part
			if( NextChar(doc, index++, out ch) == false )
			{
				return -1;
			}
			if( ch != '/' )
			{
				return -1;
			}

		//authority:
			// parse first character of authority part
			if( NextChar(doc, index++, out ch) == false )
			{
				return -1;
			}

			// parse remainings of authority part
			for( ; index<lineEnd; index++ )
			{
				if( NextChar(doc, index, out ch) == false )
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
			}

		//path:
			// parse path part
			for( ; index<lineEnd; index++ )
			{
				if( NextChar(doc, index, out ch) == false )
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
			}

		query:
			// parse query part
			for( ; index<lineEnd; )
			{
				if( NextChar(doc, index++, out ch) == false )
				{
					return index;
				}
				if( ch == '#' )
				{
					break; //goto fragment;
				}
			}

		fragment:
			// parse fragment part
			for( ; index<lineEnd; index++ )
			{
				if( NextChar(doc, index, out ch) == false )
				{
					return index;
				}
			}

			return index;
		}

		static bool GetUriEnd_ValidChar( char ch )
		{
			return ( 0x7f < ch
					|| 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z' // alpha
					|| '0' <= ch && ch <= '9' // digit
					|| ch == '-' || ch == '.' || ch == '_' || ch == '~' // 'unreserved' remainings
					|| ch == '%' // pct-encoded
					);//|| ch == ':' || ch == '/' || ch == '?' || ch == '#' );
		}

		/// <summary>
		/// Gets next character.
		/// </summary>
		/// <returns>Whether next character was valid as part of URI and successfully retrieved.</returns>
		bool NextChar( Document doc, int index, out char ch )
		{
			if( doc.Length <= index )
			{
				ch = '\0';
				return false;
			}

			ch = doc[index];
			if( 0x7f < ch
				|| 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z' // alpha
				|| '0' <= ch && ch <= '9' // digit
				|| ch == '-' || ch == '.' || ch == '_' || ch == '~' // 'unreserved' remainings
				|| ch == '%' // pct-encoded
				|| ch == ':' || ch == '/' || ch == '?' || ch == '#' )
			{
				return true;
			}

			return false;
		}

		int GetMailToEnd( Document doc, int startIndex )
		{
			int index = startIndex;
			int lineEnd;
			char ch;

			// prepare parsing
			int lineHeadIndex = doc.GetLineHeadIndexFromCharIndex( startIndex );
			lineEnd = lineHeadIndex + doc.GetLineLengthFromCharIndex( lineHeadIndex );
			DebugUtl.Assert( lineEnd <= doc.Length );

		//local-part:
			while( index < lineEnd )
			{
				ch = doc[index++];
				if( GetMailToEnd_IsLocalPartChar(ch) )
				{
					continue;
				}
				if( ch == '@' )
				{
					break;
				}
				return -1;
			}

			// do not mark if all characters consumed before starting domain part
			if( lineEnd <= index )
			{
				return -1;
			}

		//domain:
			// parse first character of domain part
			ch = doc[index];
			if( GetMailToEnd_IsDomainChar(ch) == false )
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

				// go to next char
				index++;
			}

			return index;
		}

		static bool GetMailToEnd_IsLocalPartChar( char ch )
		{
			return ('0' <= ch && ch <= '9')
					|| ('A' <= ch && ch <= 'Z')
					|| 'a' <= ch && ch <= 'z'
					|| 0 <= "!#$%&'*+-/=?^_`{|}~".IndexOf(ch);
		}

		static bool GetMailToEnd_IsDomainChar( char ch )
		{
			return ('\x21' <= ch && ch <= '\x5a')
					|| ('\x5e' <= ch && ch <= '\x7e');
		}
		#endregion

		#region Utilities
		bool SchemeStartsFromHere( Document doc, int index )
		{
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
