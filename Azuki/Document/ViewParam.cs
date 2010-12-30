// file: ViewParam.cs
// brief: Parameters associated with each document used internally by View and UiImpl.
// author: YAMAMOTO Suguru
// update: 2010-12-26
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Parameters associated with each document used internally by View and UiImpl.
	/// </summary>
	/// <remarks>
	/// This class is a set of parameters that are dependent on each document
	/// but are not parameters about document content
	/// (mainly used for drawing text or user interaction.)
	/// </remarks>
	internal class ViewParam
	{
		#region Fields
		// common
		const int MinLineNumber = 1000;
		int _FirstVisibleLine = 0;
		int _ScrollPosX = 0;
		int _DesiredColumnX = 0;
		int _MaxLineNumber = 9999;

		// for PropView
		int _PrevCaretLine, _PrevAnchorLine;
		int _PrevHRulerVirX;

		// for PropWrapView
		SplitArray<int> _PLHI = new SplitArray<int>( 128, 128 ); // this was 'physical line head indexes' in past so its name starts with 'P'
		int _LastTextAreaWidth = 0;
		int _LastFontHashCode = 0;
		DateTime _LastModifiedTime = DateTime.MinValue;

		// for UiImpl
		/// <summary>Index of the bracket at caret position used to redraw (erase) previously highlighted bracket.</summary>
		public int MatchedBracketIndex1 = 0;
		/// <summary>Index of the lastly found matched bracket used to redraw previously highlighted bracket.</summary>
		public int MatchedBracketIndex2 = -1;
		/// <summary>Whether to mark URIs in the document with built-in URI marker or not.</summary>
		public bool MarksUri = true;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ViewParam()
		{
			_PLHI.Add( 0 );
		}
		#endregion

		#region View common properties
		/// <summary>
		/// Gets or sets current X-coordinate of the "desired column."
		/// </summary>
		public int DesiredColumnX
		{
			get{ return _DesiredColumnX; }
			set{ _DesiredColumnX = value; }
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
		/// Gets or sets maximum line number.
		/// </summary>
		public int MaxLineNumber
		{
			get{ return _MaxLineNumber; }
			set{ _MaxLineNumber = value; }
		}
		#endregion

		#region PropView specific parameters
		public int PrevAnchorLine
		{
			get{ return _PrevAnchorLine; }
			set{ _PrevAnchorLine = value; }
		}

		public int PrevCaretLine
		{
			get{ return _PrevCaretLine; }
			set{ _PrevCaretLine = value; }
		}

		/// <summary>
		/// Gets or sets lastly drawn horizontal ruler bar position.
		/// </summary>
		public int PrevHRulerVirX
		{
			get{ return _PrevHRulerVirX; }
			set{ _PrevHRulerVirX = value; }
		}
		#endregion

		#region PropWrapView specific parameters
		public SplitArray<int> PLHI
		{
			get{ return _PLHI; }
		}

		public int LastTextAreaWidth
		{
			get{ return _LastTextAreaWidth; }
			set{ _LastTextAreaWidth = value; }
		}

		public int LastFontHashCode
		{
			get{ return _LastFontHashCode; }
			set{ _LastFontHashCode = value; }
		}

		public DateTime LastModifiedTime
		{
			get{ return _LastModifiedTime; }
			set{ _LastModifiedTime = value; }
		}
		#endregion
	}
}
