// file: SelectionManager.cs
// brief: Internal class to manage text selection range.
// author: YAMAMOTO Suguru
// update: 2010-08-09
//=========================================================
using System;
using System.Diagnostics;
using System.Drawing;

namespace Sgry.Azuki
{
	/// <summary>
	/// Internal class to manage text selection range.
	/// </summary>
	class SelectionManager
	{
		#region Fields
		Document _Document;
		int _CaretIndex = 0;
		int _AnchorIndex = 0;
		int _OriginalAnchorIndex = -1;
		int _LineSelectionAnchor1 = -1;
		int _LineSelectionAnchor2 = -1; // temporary variable holding selection anchor on expanding line selection backward
		int[] _RectSelectRanges = null;
		TextDataType _SelectionMode = TextDataType.Normal;
		#endregion

		#region Init / Dispose
		public SelectionManager( Document doc )
		{
			Debug.Assert( doc != null );
			_Document = doc;
		}
		#endregion

		#region Selection State
		/// <summary>
		/// Gets or sets current position of the caret.
		/// </summary>
		public int CaretIndex
		{
			get{ return _CaretIndex; }
			set
			{
				Debug.Assert( 0 <= value && value <= _Document.Length, "invalid value ("+value+") was set to SelectionManager.CaretIndex (Document.Length:"+_Document.Length+")" );
				_CaretIndex = value;
			}
		}

		/// <summary>
		/// Gets or sets current position of selection anchor.
		/// </summary>
		public int AnchorIndex
		{
			get{ return _AnchorIndex; }
			set
			{
				Debug.Assert( 0 <= value && value <= _Document.Length, "invalid value ("+value+") was set to SelectionManager.AnchorIndex (Document.Length:"+_Document.Length+")" );
				_OriginalAnchorIndex = -1;
				_AnchorIndex = value;
			}
		}

		/// <summary>
		/// Gets originally set position of selection anchor.
		/// </summary>
		public int OriginalAnchorIndex
		{
			get
			{
				if( 0 <= _OriginalAnchorIndex )
					return _OriginalAnchorIndex;
				else
					return _AnchorIndex;
			}
		}

		public TextDataType SelectionMode
		{
			get{ return _SelectionMode; }
			set
			{
				bool changed = (_SelectionMode != value);
				_SelectionMode = value;
				if( changed )
				{
					_Document.InvokeSelectionModeChanged();
				}
			}
		}

		public int[] RectSelectRanges
		{
			get{ return _RectSelectRanges; }
			set{ _RectSelectRanges = value; }
		}

		public void GetSelection( out int begin, out int end )
		{
			if( _AnchorIndex < _CaretIndex )
			{
				begin = _AnchorIndex;
				end = _CaretIndex;
			}
			else
			{
				begin = _CaretIndex;
				end = _AnchorIndex;
			}
		}

		public void SetSelection( int anchor, int caret, IView view )
		{
			Debug.Assert( 0 <= anchor && anchor <= _Document.Length, "parameter 'anchor' out of range (anchor:"+anchor+", Document.Length:"+_Document.Length+")" );
			Debug.Assert( 0 <= caret && caret <= _Document.Length, "parameter 'caret' out of range (anchor:"+anchor+", Document.Length:"+_Document.Length+")" );
			Debug.Assert( _SelectionMode == TextDataType.Normal || view != null );

			// ensure that document can be divided at given index
			Document.Utl.ConstrainIndex( _Document, ref anchor, ref caret );

			// set selection
			if( SelectionMode == TextDataType.Rectangle )
			{
				ClearLineSelectionData();
				_OriginalAnchorIndex = -1;
				SetSelection_Rect( anchor, caret, view );
			}
			else if( SelectionMode == TextDataType.Line )
			{
				ClearRectSelectionData();
				_OriginalAnchorIndex = -1;
				SetSelection_Line( anchor, caret, view );
			}
			else if( SelectionMode == TextDataType.Words )
			{
				ClearLineSelectionData();
				ClearRectSelectionData();
				SetSelection_Words( anchor, caret );
			}
			else
			{
				ClearLineSelectionData();
				ClearRectSelectionData();
				_OriginalAnchorIndex = -1;
				SetSelection_Normal( anchor, caret );
			}
		}
		#endregion

		#region Internal Logic
		void SetSelection_Rect( int anchor, int caret, IView view )
		{
			// calculate graphical position of both anchor and new caret
			Point anchorPos = view.GetVirPosFromIndex( anchor );
			Point caretPos = view.GetVirPosFromIndex( caret );

			// calculate ranges selected by the rectangle made with the two points
			_RectSelectRanges = view.GetRectSelectRanges(
					Utl.MakeRectFromTwoPoints(anchorPos, caretPos)
				);

			// set selection
			SetSelection_Normal( anchor, caret );
		}

		void SetSelection_Line( int anchor, int caret, IView view )
		{
			int toLineIndex;

			// get line index of the lines where selection starts and ends
			toLineIndex = view.GetLineIndexFromCharIndex( caret );
			if( _LineSelectionAnchor1 < 0
				|| (anchor != _LineSelectionAnchor1 && anchor != _LineSelectionAnchor2) )
			{
				//-- line selection anchor changed or did not exists --
				// select between head of the line and end of the line
				int fromLineIndex = view.GetLineIndexFromCharIndex( anchor );
				anchor = view.GetLineHeadIndex( fromLineIndex );
				if( fromLineIndex+1 < view.LineCount )
				{
					caret = view.GetLineHeadIndex( fromLineIndex + 1 );
				}
				else
				{
					caret = _Document.Length;
				}
				_LineSelectionAnchor1 = anchor;
				_LineSelectionAnchor2 = anchor;
			}
			else if( _LineSelectionAnchor1 < caret )
			{
				//-- selecting to the line (or after) where selection started --
				// select between head of the starting line and the end of the destination line
				anchor = view.GetLineHeadIndexFromCharIndex( _LineSelectionAnchor1 );
				if( Document.Utl.IsLineHead(_Document, view, caret) == false )
				{
					toLineIndex = view.GetLineIndexFromCharIndex( caret );
					if( toLineIndex+1 < view.LineCount )
					{
						caret = view.GetLineHeadIndex( toLineIndex + 1 );
					}
					else
					{
						caret = _Document.Length;
					}
				}
			}
			else// if( caret < LineSelectionAnchor )
			{
				//-- selecting to foregoing lines where selection started --
				// select between head of the destination line and end of the starting line
				int anchorLineIndex;

				caret = view.GetLineHeadIndex( toLineIndex );
				anchorLineIndex = view.GetLineIndexFromCharIndex( _LineSelectionAnchor1 );
				if( anchorLineIndex+1 < view.LineCount )
				{
					anchor = view.GetLineHeadIndex( anchorLineIndex + 1 );
				}
				else
				{
					anchor = _Document.Length;
				}
				//DO_NOT//_LineSelectionAnchor1 = anchor;
				_LineSelectionAnchor2 = anchor;
			}

			// apply new selection
			SetSelection_Normal( anchor, caret );
		}

		void SetSelection_Words( int anchor, int caret )
		{
			int waBegin, waEnd; // wa = Word at Anchor
			int wcBegin, wcEnd; // wc = Word at Caret

			// remember original position of anchor 
			_OriginalAnchorIndex = anchor;

			// ensure both selection boundaries are on word boundary
			_Document.GetWordAt( anchor, out waBegin, out waEnd );
			_Document.GetWordAt( caret, out wcBegin, out wcEnd );
			if( anchor <= caret )
			{
				anchor = waBegin;
				caret = wcEnd;
			}
			else
			{
				caret = wcBegin;
				anchor = waEnd;
			}

			// select normally
			SetSelection_Normal( anchor, caret );

		}

		void SetSelection_Normal( int anchor, int caret )
		{
			int oldAnchor, oldCaret;
			int[] oldRectSelectRanges = null;

			// if given parameters change nothing, do nothing
			if( _AnchorIndex == anchor && _CaretIndex == caret )
			{
				// but on executing rectangle selection with mouse,
				// slight movement that does not change the selection in the line under the mouse cursor
				// might change selection in other lines which is not under the mouse cursor.
				// so invoke event only if it is rectangle selection mode.
				if( _RectSelectRanges != null )
				{
					_Document.InvokeSelectionChanged( AnchorIndex, CaretIndex, _RectSelectRanges, false );
				}
				return;
			}

			// remember old selection state
			oldAnchor = _AnchorIndex;
			oldCaret = _CaretIndex;
			oldRectSelectRanges = _RectSelectRanges;

			// apply new selection
			_AnchorIndex = anchor;
			_CaretIndex = caret;

			// invoke event
			if( oldRectSelectRanges != null )
			{
				_Document.InvokeSelectionChanged( oldAnchor, oldCaret, oldRectSelectRanges, false );
			}
			else
			{
				_Document.InvokeSelectionChanged( oldAnchor, oldCaret, oldRectSelectRanges, false );
			}
		}

		void ClearRectSelectionData()
		{
			_RectSelectRanges = null;
		}

		void ClearLineSelectionData()
		{
			_LineSelectionAnchor1 = -1;
			_LineSelectionAnchor2 = -1;
		}
		#endregion

		#region Utilities
		static class Utl
		{
			public static Rectangle MakeRectFromTwoPoints( Point pt1, Point pt2 )
			{
				Rectangle rect = new Rectangle();

				// set left and width
				if( pt1.X < pt2.X )
				{
					rect.X = pt1.X;
					rect.Width = pt2.X - pt1.X;
				}
				else
				{
					rect.X = pt2.X;
					rect.Width = pt1.X - pt2.X;
				}

				// set top and height
				if( pt1.Y < pt2.Y )
				{
					rect.Y = pt1.Y;
					rect.Height = pt2.Y - pt1.Y;
				}
				else
				{
					rect.Y = pt2.Y;
					rect.Height = pt1.Y - pt2.Y;
				}

				return rect;
			}
		}
		#endregion
	}
}
