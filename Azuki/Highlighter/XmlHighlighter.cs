// file: XmlHighlighter.cs
// brief: Highlighter for XML.
//=========================================================
using System;
using System.Collections.Generic;
using System.Text;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// A highlighter to highlight XML.
	/// </summary>
	class XmlHighlighter : IHighlighter
	{
		#region Fields
		static readonly string DefaultWordCharSet = null;
		List<Enclosure> _Enclosures = new List<Enclosure>();
		SplitArray<int> _ReparsePoints = new SplitArray<int>( 64 );
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets whether a highlighter hook procedure can be installed or not.
		/// </summary>
		public bool CanUseHook
		{
			get{ return false; }
		}

		/// <summary>
		/// Gets or sets highlighter hook procedure.
		/// </summary>
		/// <exception cref="System.NotSupportedException">This highlighter does not support hook procedure.</exception>
		public HighlightHook HookProc
		{
			get{ throw new NotSupportedException(); }
			set{ throw new NotSupportedException(); }
		}
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public XmlHighlighter()
		{
			Enclosure doubleQuote = new Enclosure();
			doubleQuote.opener = doubleQuote.closer = "\"";
			doubleQuote.klass = CharClass.String;
			doubleQuote.multiLine = true;
			_Enclosures.Add( doubleQuote );

			Enclosure singleQuote = new Enclosure();
			singleQuote.opener = singleQuote.closer = "'";
			singleQuote.klass = CharClass.String;
			singleQuote.multiLine = true;
			_Enclosures.Add( singleQuote );

			Enclosure cdata = new Enclosure();
			cdata.opener = "<![CDATA[";
			cdata.closer = "]]>";
			cdata.klass = CharClass.CDataSection;
			cdata.multiLine = true;
			_Enclosures.Add( cdata );

			Enclosure comment = new Enclosure();
			comment.opener = "<!--";
			comment.closer = "-->";
			comment.klass = CharClass.Comment;
			comment.multiLine = true;
			_Enclosures.Add( comment );
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

			char nextCh;
			int index, nextIndex;

			// determine where to start highlighting
			index = Utl.FindLeastMaximum( _ReparsePoints, dirtyBegin );
			if( 0 <= index )
			{
				dirtyBegin = _ReparsePoints[index];
			}
			else
			{
				dirtyBegin = 0;
			}

			// determine where to end highlighting
			int x = Utl.ReparsePointMinimumDistance;
			dirtyEnd += x - (dirtyEnd % x); // next multiple of x
			if( doc.Length < dirtyEnd )
			{
				dirtyEnd = doc.Length;
			}

			// seek each tags
			index = 0;
			while( 0 <= index && index < dirtyEnd )
			{
				if( Utl.TryHighlight(doc, _Enclosures, index, dirtyEnd, out nextIndex) )
				{
					Utl.EntryReparsePoint( _ReparsePoints, index );
					index = nextIndex;
				}
				else if( doc[index] == '<' )
				{
					Utl.EntryReparsePoint( _ReparsePoints, index );

					// set class for '<'
					doc.SetCharClass( index, CharClass.Delimiter );
					index++;
					if( doc.Length <= index )
					{
						return; // reached to the end
					}

					// if next char is '?' or '/', highlight it too
					nextCh = doc[ index ];
					if( nextCh == '?' || nextCh == '/' || nextCh == '!' )
					{
						doc.SetCharClass( index, CharClass.Delimiter );
						index++;
						if( doc.Length <= index )
							return; // reached to the end
					}

					// skip whitespaces
					while( Char.IsWhiteSpace(doc[index]) )
					{
						index++;
						if( doc.Length <= index )
							return; // reached to the end
					}

					// highlight element name
					nextIndex = Utl.FindNextToken( doc, index, DefaultWordCharSet );
					for( int i=index; i<nextIndex; i++ )
					{
						doc.SetCharClass( i, CharClass.ElementName );
					}
					index = nextIndex;

					// highlight attributes
					while( index < doc.Length && doc[index] != '>' )
					{
						// highlight enclosing part if this token begins a part
						if( Utl.TryHighlight(doc, _Enclosures, index, dirtyEnd, out nextIndex) )
						{
							// successfully highlighted. skip to next.
							index = nextIndex;
							continue;
						}

						// this token is normal class; reset classes and seek to next token
						nextIndex = Utl.FindNextToken( doc, index, DefaultWordCharSet );
						for( int i=index; i<nextIndex; i++ )
						{
							doc.SetCharClass( i, CharClass.Attribute );
						}
						index = nextIndex;
					}

					// highlight '>'
					if( index < doc.Length )
					{
						doc.SetCharClass( index, CharClass.Delimiter );
						if( 1 <= index && doc[index-1] == '/' )
							doc.SetCharClass( index-1, CharClass.Delimiter );
						index++;
					}
				}
				else if( doc[index] == '&' )
				{
					int seekEndIndex;
					bool wasEntity;
					CharClass klass;

					// find end position of this token
					FindEntityEnd( doc, index, out seekEndIndex, out wasEntity );
					DebugUtl.Assert( 0 <= seekEndIndex && seekEndIndex <= doc.Length );

					// highlight this token
					klass = wasEntity ? CharClass.Entity : CharClass.Normal;
					for( int i=index; i<seekEndIndex; i++ )
					{
						doc.SetCharClass( i, klass );
					}
					index = seekEndIndex;
				}
				else
				{
					// normal character.
					doc.SetCharClass( index, CharClass.Normal );
					index++;
				}
			}
		}

		static void FindEntityEnd( Document doc, int startIndex, out int endIndex, out bool wasEntity )
		{
			DebugUtl.Assert( startIndex < doc.Length );
			DebugUtl.Assert( doc[startIndex] == '&' );

			endIndex = startIndex + 1;
			while( endIndex < doc.Length )
			{
				char ch = doc[endIndex];

				if( (ch < 'A' || 'Z' < ch)
					&& (ch < 'a' || 'z' < ch)
					&& (ch < '0' || '9' < ch)
					&& (ch != '#') )
				{
					if( ch == ';' )
					{
						endIndex++;
						wasEntity = true;
						return;
					}
					else
					{
						wasEntity = false;
						return;
					}
				}

				endIndex++;
			}

			wasEntity = false;
			return;
		}
		#endregion
	}
}
