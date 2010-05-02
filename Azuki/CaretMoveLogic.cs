// file: CaretMoveLogic.cs
// brief: Implementation of caret movement.
// author: YAMAMOTO Suguru
// update: 2010-05-02
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	static class CaretMoveLogic
	{
		#region Public interface
		public delegate int CalcMethod( IView view );

		/// <summary>
		/// Moves caret to the index where the specified method calculates.
		/// </summary>
		public static void MoveCaret( CalcMethod calculator, IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;

			int nextIndex = calculator( view );
			if( nextIndex == doc.CaretIndex )
			{
				// notify that the caret not moved
				Plat.Inst.MessageBeep();
			}
			else
			{
				// set new selection and scroll to caret
				doc.SetSelection( nextIndex, nextIndex );
				ui.SelectionMode = TextDataType.Normal;
				view.ScrollToCaret();
			}
		}

		/// <summary>
		/// Expand selection to the index where the specified method calculates
		/// (selection anchor will not be changed).
		/// </summary>
		public static void SelectTo( CalcMethod calculator, IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;
			int nextIndex;

			// calculate where to expand selection
			nextIndex = calculator( view );
			if( nextIndex == doc.CaretIndex )
			{
				// notify that the caret not moved
				Plat.Inst.MessageBeep();
			}

			// set new selection
			doc.SetSelection( doc.AnchorIndex, nextIndex, view );
			view.ScrollToCaret();
		}
		#endregion

		#region Index Calculation
		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "right" key.
		/// </summary>
		public static int Calc_Right( IView view )
		{
			Document doc = view.Document;
			if( doc.Length < doc.CaretIndex+1 )
			{
				return doc.Length;
			}
			
			int offset = 1;
			int caret = doc.CaretIndex;

			// avoid placing caret at middle of a CR-LF or a surrogate pair
			if( caret+2 <= doc.Length )
			{
				string nextTwoChars = "" + doc[caret] + doc[caret+1];
				if( nextTwoChars == "\r\n"
					|| Document.IsHighSurrogate(nextTwoChars[0]) )
				{
					offset = 2;
				}
			}

			return doc.CaretIndex + offset;
		}

		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "left" key.
		/// </summary>
		public static int Calc_Left( IView view )
		{
			Document doc = view.Document;
			if( doc.CaretIndex-1 < 0 )
			{
				return 0;
			}

			int offset = 1;
			int caret = doc.CaretIndex;

			// avoid placing caret at middle of a CR-LF or a surrogate pair
			if( 0 <= caret-2 )
			{
				string prevTwoChars = "" + doc[caret-2] + doc[caret-1];
				if( prevTwoChars == "\r\n"
					|| Document.IsLowSurrogate(prevTwoChars[1]) )
				{
					offset = 2;
				}
			}

			return doc.CaretIndex - offset;
		}

		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "down" key.
		/// </summary>
		public static int Calc_Down( IView view )
		{
			Point pt;
			int newIndex;
			Document doc = view.Document;

			// get screen location of the caret
			pt = view.GetVirPosFromIndex( doc.CaretIndex );

			// calculate next location
			pt.X = view.GetDesiredColumn();
			pt.Y += view.LineSpacing;
			/* NOT NEEDED because View.GetIndexFromVirPos handles this case.
			if( view.Height - view.LineSpacing < pt.Y )
			{
				return doc.CaretIndex; // no lines below. don't move.
			}*/
			newIndex = view.GetIndexFromVirPos( pt );

			// In line selection mode,
			// moving caret across the line which contains the anchor position
			// should select the line and a line below.
			// To select a line below, calculate index of the char at one more line below.
			if( doc.SelectionMode == TextDataType.Line
				&& Document.Utl.IsLineHead(doc, view, newIndex) )
			{
				Point pt2 = new Point( pt.X, pt.Y+view.LineSpacing );
				int skippedNewIndex = view.GetIndexFromVirPos( pt2 );
				if( skippedNewIndex == doc.AnchorIndex )
				{
					newIndex = skippedNewIndex;
				}
			}

			return newIndex;
		}

		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "up" key.
		/// </summary>
		public static int Calc_Up( IView view )
		{
			Point pt;
			int newIndex;
			Document doc = view.Document;

			// get screen location of the caret
			pt = view.GetVirPosFromIndex( doc.CaretIndex );

			// calculate next location
			pt.X = view.GetDesiredColumn();
			pt.Y -= view.LineSpacing;
			newIndex = view.GetIndexFromVirPos( pt );
			if( newIndex < 0 )
			{
				return doc.CaretIndex; // don't move
			}

			// In line selection mode,
			// moving caret across the line which contains the anchor position
			// should select the line and a line above.
			// To select a line above, calculate index of the char at one more line above.
			if( doc.SelectionMode == TextDataType.Line
				&& newIndex == doc.AnchorIndex
				&& Document.Utl.IsLineHead(doc, view, newIndex) )
			{
				pt.Y -= view.LineSpacing;
				if( 0 <= pt.Y )
				{
					newIndex = view.GetIndexFromVirPos( pt );
				}
			}

			return newIndex;
		}

		/// <summary>
		/// Calculate index of the next word.
		/// </summary>
		public static int Calc_NextWord( IView view )
		{
			Document doc = view.Document;
			if( doc.Length < doc.CaretIndex+1 )
			{
				return doc.Length;
			}

			return WordLogic.NextWordStartForMove( doc, doc.CaretIndex );
		}

		/// <summary>
		/// Calculate index of the previous word.
		/// </summary>
		public static int Calc_PrevWord( IView view )
		{
			Document doc = view.Document;
			if( doc.CaretIndex <= 1 )
			{
				return 0;
			}

			return WordLogic.PrevWordStartForMove( doc, doc.CaretIndex );
		}

		/// <summary>
		/// Calculate index of the first char of the line where caret is at.
		/// </summary>
		public static int Calc_LineHead( IView view )
		{
			return view.GetLineHeadIndexFromCharIndex(
					view.Document.CaretIndex
				);
		}

		/// <summary>
		/// Calculate index of the first non-whitespace char of the line where caret is at.
		/// </summary>
		public static int Calc_LineHeadSmart( IView view )
		{
			int lineHeadIndex, firstNonSpaceIndex;
			Document doc = view.Document;

			lineHeadIndex = view.GetLineHeadIndexFromCharIndex( doc.CaretIndex );

			firstNonSpaceIndex = lineHeadIndex;
			while( firstNonSpaceIndex < doc.Length
				&& Utl.IsWhiteSpace(doc[firstNonSpaceIndex]) )
			{
				firstNonSpaceIndex++;
			}

			return (firstNonSpaceIndex == doc.CaretIndex) ? lineHeadIndex : firstNonSpaceIndex;
		}

		/// <summary>
		/// Calculate index of the end location of the line where caret is at.
		/// </summary>
		public static int Calc_LineEnd( IView view )
		{
			Document doc = view.Document;
			int line, column;
			int offset = -1;

			view.GetLineColumnIndexFromCharIndex( doc.CaretIndex, out line, out column );
			if( view.LineCount <= line+1 )
			{
				return doc.Length;
			}

			int nextIndex = view.GetCharIndexFromLineColumnIndex( line+1, 0 );
			if( 0 <= nextIndex-1 && doc.GetCharAt(nextIndex-1) == '\n'
				&& 0 <= nextIndex-2 && doc.GetCharAt(nextIndex-2) == '\r' )
			{
				offset = -2;
			}

			return nextIndex + offset;
		}

		/// <summary>
		/// Calculate first index of the file.
		/// </summary>
		public static int Calc_FileHead( IView view )
		{
			return 0;
		}

		/// <summary>
		/// Calculate end index of the file.
		/// </summary>
		public static int Calc_FileEnd( IView view )
		{
			return view.Document.Length;
		}
		#endregion

		#region Utilities
		static class Utl
		{
			public static bool IsWhiteSpace( char ch )
			{
				if( ch == ' '
					|| ch == '\t'
					|| ch == '\x3000' )
					return true;

				return false;
			}
		}
		#endregion
	}
}
