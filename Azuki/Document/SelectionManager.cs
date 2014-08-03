// file: SelectionManager.cs
// brief: Internal class to manage text selection range.
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
			get{ return ViewParam.CaretIndex; }
			set
			{
				Debug.Assert( 0 <= value && value <= _Document.Length,
							  "invalid value ("+value+") was set to SelectionManager.CaretIndex"
							  + " (Document.Length:"+_Document.Length+")" );
				ViewParam.CaretIndex = value;
			}
		}

		/// <summary>
		/// Gets or sets current position of selection anchor.
		/// </summary>
		public int AnchorIndex
		{
			get{ return ViewParam.AnchorIndex; }
			set
			{
				Debug.Assert( 0 <= value && value <= _Document.Length,
							  "invalid value ("+value+") was set to SelectionManager.AnchorIndex"
							  + " (Document.Length:"+_Document.Length+")" );
				ViewParam.OriginalAnchorIndex = -1;
				ViewParam.AnchorIndex = value;
			}
		}

		/// <summary>
		/// Gets originally set position of selection anchor.
		/// </summary>
		public int OriginalAnchorIndex
		{
			get
			{
				if( 0 <= ViewParam.OriginalAnchorIndex )
					return ViewParam.OriginalAnchorIndex;
				else
					return ViewParam.AnchorIndex;
			}
		}

		public TextDataType SelectionMode
		{
			get{ return ViewParam.SelectionMode; }
			set
			{
				bool changed = (ViewParam.SelectionMode != value);
				ViewParam.SelectionMode = value;
				if( changed )
				{
					_Document.InvokeSelectionModeChanged();
				}
			}
		}

		public int[] RectSelectRanges
		{
			get{ return ViewParam.RectSelectRanges; }
			set{ ViewParam.RectSelectRanges = value; }
		}

		public void GetSelection( out int begin, out int end )
		{
			if( ViewParam.AnchorIndex < ViewParam.CaretIndex )
			{
				begin = ViewParam.AnchorIndex;
				end = ViewParam.CaretIndex;
			}
			else
			{
				begin = ViewParam.CaretIndex;
				end = ViewParam.AnchorIndex;
			}
		}

		public void SetSelection( int anchor, int caret, IViewInternal view )
		{
			Debug.Assert( 0 <= anchor && anchor <= _Document.Length, "parameter 'anchor' out of"
						  + " range (anchor:" + anchor + ", Document.Length:"
						  + _Document.Length + ")" );
			Debug.Assert( 0 <= caret && caret <= _Document.Length, "parameter 'caret' out of range"
						  + " (anchor:" + anchor + ", Document.Length:" + _Document.Length + ")" );
			Debug.Assert( ViewParam.SelectionMode == TextDataType.Normal || view != null );

			// ensure that document can be divided at given index
			if( anchor < caret )
			{
				while( _Document.IsNotDividableIndex(anchor) )	anchor--;
				while( _Document.IsNotDividableIndex(caret) )	caret++;
			}
			else if( caret < anchor )
			{
				while( _Document.IsNotDividableIndex(caret) )	caret--;
				while( _Document.IsNotDividableIndex(anchor) )	anchor++;
			}
			else// if( anchor == caret )
			{
				while( _Document.IsNotDividableIndex(caret) )	caret--;
				anchor = caret;
			}

			// set selection
			if( SelectionMode == TextDataType.Rectangle )
			{
				ClearLineSelectionData();
				ViewParam.OriginalAnchorIndex = -1;
				SetSelection_Rect( anchor, caret, view );
			}
			else if( SelectionMode == TextDataType.Line )
			{
				ClearRectSelectionData();
				ViewParam.OriginalAnchorIndex = -1;
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
				ViewParam.OriginalAnchorIndex = -1;
				SetSelection_Normal( anchor, caret );
			}
		}

		/// <summary>
		/// Distinguishes whether specified index is in selection or not.
		/// </summary>
		public bool IsInSelection( int index )
		{
			int begin, end;

			if( _Document.RectSelectRanges != null )
			{
				// is in rectangular selection mode.
				for( int i=0; i<_Document.RectSelectRanges.Length; i+=2 )
				{
					begin = _Document.RectSelectRanges[i];
					end = _Document.RectSelectRanges[i+1];
					if( begin <= index && index < end )
					{
						return true;
					}
				}

				return false;
			}
			else
			{
				// is not in rectangular selection mode.
				_Document.GetSelection( out begin, out end );
				return (begin <= index && index < end);
			}
		}
		#endregion

		#region Internal Logic
		void SetSelection_Rect( int anchor, int caret, IViewInternal view )
		{
			// calculate graphical position of both anchor and new caret
			Point anchorPos = view.GetVirPosFromIndex( anchor );
			Point caretPos = view.GetVirPosFromIndex( caret );

			// calculate ranges selected by the rectangle made with the two points
			ViewParam.RectSelectRanges = view.GetRectSelectRanges( MakeRect(anchorPos, caretPos) );

			// set selection
			SetSelection_Normal( anchor, caret );
		}

		void SetSelection_Line( int anchor, int caret, IViewInternal view )
		{
			int toLineIndex;

			// get line index of the lines where selection starts and ends
			toLineIndex = view.GetLineIndexFromCharIndex( caret );
			if( ViewParam.LineSelectionAnchor1 < 0
				|| (anchor != ViewParam.LineSelectionAnchor1 && anchor != ViewParam.LineSelectionAnchor2) )
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
				ViewParam.LineSelectionAnchor1 = anchor;
				ViewParam.LineSelectionAnchor2 = anchor;
			}
			else if( ViewParam.LineSelectionAnchor1 < caret )
			{
				//-- selecting to the line (or after) where selection started --
				// select between head of the starting line and the end of the destination line
				anchor = view.GetLineHeadIndexFromCharIndex( ViewParam.LineSelectionAnchor1 );
				if( view.IsLineHeadIndex(caret) == false )
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
				anchorLineIndex = view.GetLineIndexFromCharIndex( ViewParam.LineSelectionAnchor1 );
				if( anchorLineIndex+1 < view.LineCount )
				{
					anchor = view.GetLineHeadIndex( anchorLineIndex + 1 );
				}
				else
				{
					anchor = _Document.Length;
				}
				//DO_NOT//ViewParam.LineSelectionAnchor1 = anchor;
				ViewParam.LineSelectionAnchor2 = anchor;
			}

			// apply new selection
			SetSelection_Normal( anchor, caret );
		}

		void SetSelection_Words( int anchor, int caret )
		{
			int waBegin, waEnd; // wa = Word at Anchor
			int wcBegin, wcEnd; // wc = Word at Caret

			// remember original position of anchor 
			ViewParam.OriginalAnchorIndex = anchor;

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
			if( ViewParam.AnchorIndex == anchor && ViewParam.CaretIndex == caret )
			{
				// but on executing rectangle selection with mouse,
				// slight movement that does not change the selection in the line under the mouse cursor
				// might change selection in other lines which is not under the mouse cursor.
				// so invoke event only if it is rectangle selection mode.
				if( ViewParam.RectSelectRanges != null )
				{
					_Document.InvokeSelectionChanged( AnchorIndex, CaretIndex,
													  ViewParam.RectSelectRanges, false );
				}
				return;
			}

			// remember old selection state
			oldAnchor = ViewParam.AnchorIndex;
			oldCaret = ViewParam.CaretIndex;
			oldRectSelectRanges = ViewParam.RectSelectRanges;

			// apply new selection
			ViewParam.AnchorIndex = anchor;
			ViewParam.CaretIndex = caret;

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
			ViewParam.RectSelectRanges = null;
		}

		void ClearLineSelectionData()
		{
			ViewParam.LineSelectionAnchor1 = -1;
			ViewParam.LineSelectionAnchor2 = -1;
		}
		#endregion

		#region Utilities
		ViewParam ViewParam
		{
			get{ return _Document.ViewParam; }
		}

		static Rectangle MakeRect( Point pt1, Point pt2 )
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
		#endregion
	}
}
