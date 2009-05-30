// file: ViewParam.cs
// brief: View parameters associated with each document.
// author: YAMAMOTO Suguru
// update: 2009-05-30
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// View parameters associated with each document.
	/// </summary>
	/// <remarks>
	/// This class is a set of parameters that are dependent on each document
	/// but are not parameters about document content
	/// (mainly used for drawing text or user interaction.)
	/// </remarks>
	public sealed class ViewParam
	{
		#region Fields
		// common
		const int MinLineNumber = 1000;
		int _FirstVisibleLine = 0;
		int _ScrollPosX = 0;
		int _DesiredColumn = 0;
		int _MaxLineNumber = 9999;

		// for PropView
		int _PrevCaretLine, _PrevAnchorLine;

		// for PropWrapView
		SplitArray<int> _PLHI = new SplitArray<int>( 128, 128 );
		int _LastTextAreaWidth = 0;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		internal ViewParam()
		{
			_PLHI.Add( 0 );
		}
		#endregion

		#region View common properties
		/// <summary>
		/// Gets or sets current "desired column."
		/// </summary>
		public int DesiredColumn
		{
			get{ return _DesiredColumn; }
			set{ _DesiredColumn = value; }
		}

		/// <summary>
		/// Gets or sets index of the line which is displayed at top of the view.
		/// </summary>
		public int FirstVisibleLine
		{
			get{ return _FirstVisibleLine; }
			set
			{
				if( value < 0 )
					throw new ArgumentException( "FirstVisibleLine must be greater than zero (set value: "+value+")" );
				_FirstVisibleLine = value;
			}
		}

		/// <summary>
		/// Gets or sets x-coordinate of the view's origin currently displayed.
		/// </summary>
		public int ScrollPosX
		{
			get{ return _ScrollPosX; }
			set
			{
				if( value < 0 )
					throw new ArgumentException( "ScrollPosX must be greater than zero (set value: "+value+")" );
				_ScrollPosX = value;
			}
		}

		/// <summary>
		/// Gets or sets maximum line number (internal use only.)
		/// </summary>
		internal int MaxLineNumber
		{
			get{ return _MaxLineNumber; }
			set{ _MaxLineNumber = value; }
		}
		#endregion

		#region PropView specific parameters
		internal int PrevAnchorLine
		{
			get{ return _PrevAnchorLine; }
			set{ _PrevAnchorLine = value; }
		}

		internal int PrevCaretLine
		{
			get{ return _PrevCaretLine; }
			set{ _PrevCaretLine = value; }
		}
		#endregion

		#region PropWrapView specific parameters
		internal SplitArray<int> PLHI
		{
			get{ return _PLHI; }
		}

		internal int LastTextAreaWidth
		{
			get{ return _LastTextAreaWidth; }
			set{ _LastTextAreaWidth = value; }
		}
		#endregion
	}
}
