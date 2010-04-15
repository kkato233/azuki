// file: Actions.Selection.cs
// brief: Actions for Azuki engine (actions to change selection).
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2009-09-21
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	public static partial class Actions
	{
		#region Caret Movement
		/// <summary>
		/// Moves caret to right.
		/// </summary>
		public static void MoveRight( IUserInterface ui )
		{
			IView view = ui.View;
			int selBegin, selEnd;

			// if there are something selected,
			// release selection and set caret at where the selection ends
			view.Document.GetSelection( out selBegin, out selEnd );
			if( selEnd != selBegin )
			{
				view.Document.SetSelection( selEnd, selEnd );
				view.ScrollToCaret();
			}
			// otherwise, move caret right
			else
			{
				CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_Right, ui );
			}

			// update desired column
			view.SetDesiredColumn();
		}

		/// <summary>
		/// Moves caret to left.
		/// </summary>
		public static void MoveLeft( IUserInterface ui )
		{
			IView view = ui.View;
			int selBegin, selEnd;

			// if there are something selected,
			// release selection and set caret at where the selection starts
			view.Document.GetSelection( out selBegin, out selEnd );
			if( selEnd != selBegin )
			{
				view.Document.SetSelection( selBegin, selBegin );
				view.ScrollToCaret();
			}
			// otherwise, move caret left
			else
			{
				CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_Left, ui );
			}

			// update desired column
			view.SetDesiredColumn();
		}

		/// <summary>
		/// Moves caret down.
		/// </summary>
		public static void MoveDown( IUserInterface ui )
		{
			// move caret
			CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_Down, ui );
		}

		/// <summary>
		/// Moves caret up.
		/// </summary>
		public static void MoveUp( IUserInterface ui )
		{
			// move caret
			CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_Up, ui );
		}

		/// <summary>
		/// Moves caret to next word.
		/// </summary>
		public static void MoveToNextWord( IUserInterface ui )
		{
			Document doc = ui.View.Document;
			int selBegin, selEnd;

			// if there are something selected, release selection
			doc.GetSelection( out selBegin, out selEnd );
			if( selEnd != selBegin )
			{
				doc.SetSelection( doc.CaretIndex, doc.CaretIndex );
			}

			// then, move caret
			CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_NextWord, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Moves caret to previous word.
		/// </summary>
		public static void MoveToPrevWord( IUserInterface ui )
		{
			Document doc = ui.View.Document;
			int selBegin, selEnd;

			// if there are something selected, release selection
			doc.GetSelection( out selBegin, out selEnd );
			if( selEnd != selBegin )
			{
				doc.SetSelection( doc.CaretIndex, doc.CaretIndex );
			}

			// then, move caret
			CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_PrevWord, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Moves caret to line head.
		/// </summary>
		public static void MoveToLineHead( IUserInterface ui )
		{
			Document doc = ui.View.Document;
			int selBegin, selEnd;

			// if there are something selected, release selection
			doc.GetSelection( out selBegin, out selEnd );
			if( selEnd != selBegin )
			{
				doc.SetSelection( doc.CaretIndex, doc.CaretIndex );
			}

			// then, move caret
			CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_LineHead, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Moves caret to the first non-whitespace char at the line.
		/// </summary>
		public static void MoveToLineHeadSmart( IUserInterface ui )
		{
			Document doc = ui.View.Document;
			int selBegin, selEnd;

			// if there are something selected, release selection
			doc.GetSelection( out selBegin, out selEnd );
			if( selEnd != selBegin )
			{
				doc.SetSelection( doc.CaretIndex, doc.CaretIndex );
			}

			// then, move caret
			CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_LineHeadSmart, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Moves caret to line end.
		/// </summary>
		public static void MoveToLineEnd( IUserInterface ui )
		{
			Document doc = ui.Document;
			int selBegin, selEnd;

			// if there are something selected, release selection
			doc.GetSelection( out selBegin, out selEnd );
			if( selEnd != selBegin )
			{
				doc.SetSelection( doc.CaretIndex, doc.CaretIndex );
			}

			// then, move caret
			CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_LineEnd, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Moves caret to one page after.
		/// </summary>
		public static void MovePageDown( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;
			Point pt;
			int nextIndex;
			int diff = (view.VisibleSize.Height / view.LineSpacing);

			// get current virtual coordinate of the caret
			pt = view.GetVirPosFromIndex( doc.CaretIndex );
			
			// calc new virtual coordinate of the caret
			pt.Y += diff * view.LineSpacing;
			/*NOT_NEEDED
			if( view.VisibleSize.Height < pt.Y )
			{
				pt.Y = view.VisibleSize.Height;
			}*/

			// calc index from the coord
			nextIndex = view.GetIndexFromVirPos( pt );

			// move caret and scroll
			doc.SetSelection( nextIndex, nextIndex );
			view.Scroll( diff );
			view.ScrollToCaret();
		}

		/// <summary>
		/// Moves caret to one page before.
		/// </summary>
		public static void MovePageUp( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;
			Point pt;
			int nextIndex;
			int diff = (view.VisibleSize.Height / view.LineSpacing);

			// get current virtual coordinate of the caret
			pt = view.GetVirPosFromIndex( doc.CaretIndex );
			
			// calc new virtual coordinate of the caret
			pt.Y -= diff * view.LineSpacing;
			if( pt.Y < 0 )
			{
				pt.Y = 0;
			}

			// calc index from the coord
			nextIndex = view.GetIndexFromVirPos( pt );

			// move caret and scroll
			doc.SetSelection( nextIndex, nextIndex );
			view.Scroll( -diff );
			view.ScrollToCaret();
		}

		/// <summary>
		/// Moves caret to file head.
		/// </summary>
		public static void MoveToFileHead( IUserInterface ui )
		{
			Document doc = ui.Document;
			int selBegin, selEnd;

			// if there are something selected, release selection
			doc.GetSelection( out selBegin, out selEnd );
			if( selEnd != selBegin )
			{
				doc.SetSelection( doc.CaretIndex, doc.CaretIndex );
			}

			// then, move caret
			CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_FileHead, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Moves caret to file end.
		/// </summary>
		public static void MoveToFileEnd( IUserInterface ui )
		{
			Document doc = ui.Document;
			int selBegin, selEnd;

			// if there are something selected, release selection
			doc.GetSelection( out selBegin, out selEnd );
			if( selEnd != selBegin )
			{
				doc.SetSelection( doc.CaretIndex, doc.CaretIndex );
			}

			// then, move caret
			CaretMoveLogic.MoveCaret( CaretMoveLogic.Calc_FileEnd, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}
		#endregion

		#region Selection
		/// <summary>
		/// Expands selection to right.
		/// </summary>
		public static void SelectToRight( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_Right, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Expands selection to left.
		/// </summary>
		public static void SelectToLeft( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_Left, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Expands selection down.
		/// </summary>
		public static void SelectToDown( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_Down, ui );
		}

		/// <summary>
		/// Expands selection up.
		/// </summary>
		public static void SelectToUp( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_Up, ui );
		}

		/// <summary>
		/// Expands selection to next word begin.
		/// </summary>
		public static void SelectToNextWord( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_NextWord, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Expands selection to previous word begin.
		/// </summary>
		public static void SelectToPrevWord( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_PrevWord, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Expands selection to line head.
		/// </summary>
		public static void SelectToLineHead( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_LineHead, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Expands selection to the first non-whitespace char at the line.
		/// </summary>
		public static void SelectToLineHeadSmart( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_LineHeadSmart, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Expands selection to line end.
		/// </summary>
		public static void SelectToLineEnd( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_LineEnd, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Expands selection to one page after.
		/// </summary>
		public static void SelectToPageDown( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;
			Point pt;
			int nextIndex;
			int diff = (view.VisibleSize.Height / view.LineSpacing);

			// get current virtual coordinate of the caret
			pt = view.GetVirPosFromIndex( doc.CaretIndex );
			
			// calc new virtual coordinate of the caret
			pt.Y += diff * view.LineSpacing;
			/*NOT_NEEDED
			if( view.VisibleSize.Height < pt.Y )
			{
				pt.Y = view.VisibleSize.Height;
			}*/

			// calc index from the coord
			nextIndex = view.GetIndexFromVirPos( pt );

			// move caret and scroll
			doc.SetSelection( doc.AnchorIndex, nextIndex );
			view.Scroll( diff );
		}

		/// <summary>
		/// Expands selection to one page before.
		/// </summary>
		public static void SelectToPageUp( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;
			Point pt;
			int nextIndex;
			int diff = (view.VisibleSize.Height / view.LineSpacing);

			// get current virtual coordinate of the caret
			pt = view.GetVirPosFromIndex( doc.CaretIndex );
			
			// calc new virtual coordinate of the caret
			pt.Y -= diff * view.LineSpacing;
			if( pt.Y < 0 )
			{
				pt.Y = 0;
			}

			// calc index from the coord
			nextIndex = view.GetIndexFromVirPos( pt );

			// move caret and scroll
			doc.SetSelection( doc.AnchorIndex, nextIndex );
			view.Scroll( -diff );
		}

		/// <summary>
		/// Expands selection to file head.
		/// </summary>
		public static void SelectToFileHead( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_FileHead, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Expands selection to file end.
		/// </summary>
		public static void SelectToFileEnd( IUserInterface ui )
		{
			// change selection
			CaretMoveLogic.SelectTo( CaretMoveLogic.Calc_FileEnd, ui );

			// update desired column
			ui.View.SetDesiredColumn();
		}

		/// <summary>
		/// Expands rectangle selection to right.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This action expands rectangle selection to right.
		/// If Azuki is not in rectangle selection mode,
		/// it automatically switches to rectangle selection mode.
		/// </para>
		/// </remarks>
		public static void RectSelectToRight( IUserInterface ui )
		{
			// force to enable rectangle selection
			ui.IsRectSelectMode = true;

			// expand selection
			SelectToRight( ui );
		}

		/// <summary>
		/// Expands rectangle selection to left.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This action expands rectangle selection to left.
		/// If Azuki is not in rectangle selection mode,
		/// it automatically switches to rectangle selection mode.
		/// </para>
		/// </remarks>
		public static void RectSelectToLeft( IUserInterface ui )
		{
			// force to enable rectangle selection
			ui.IsRectSelectMode = true;

			// expand selection
			SelectToLeft( ui );
		}

		/// <summary>
		/// Expands rectangle selection down.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This action expands rectangle selection down.
		/// If Azuki is not in rectangle selection mode,
		/// it automatically switches to rectangle selection mode.
		/// </para>
		/// </remarks>
		public static void RectSelectToDown( IUserInterface ui )
		{
			// force to enable rectangle selection
			ui.IsRectSelectMode = true;

			// expand selection
			SelectToDown( ui );
		}

		/// <summary>
		/// Expands rectangle selection up.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This action expands rectangle selection up.
		/// If Azuki is not in rectangle selection mode,
		/// it automatically switches to rectangle selection mode.
		/// </para>
		/// </remarks>
		public static void RectSelectToUp( IUserInterface ui )
		{
			// force to enable rectangle selection
			ui.IsRectSelectMode = true;

			// expand selection
			SelectToUp( ui );
		}

		/// <summary>
		/// Selects all text.
		/// </summary>
		public static void SelectAll( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;
			
			// set parameters
			doc.SetSelection( 0, doc.Length );

			// update desired column
			view.SetDesiredColumn();
			view.ScrollToCaret();

			view.Invalidate(); // this is needed because Azuki's invalidation logic only supports selection change by caret movement
		}
		#endregion
	}
}
