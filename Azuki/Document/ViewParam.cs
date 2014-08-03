// file: ViewParam.cs
// brief: Parameters associated with each document used internally by View and UiImpl.
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Parameters associated with each document used internally by View and UiImpl.
	/// </summary>
	/// <remarks>
	/// This class is a set of parameters that are dependent on each document but are not
	/// related to the document content (mainly used for drawing text or user interaction.)
	/// </remarks>
	class ViewParam
	{
		#region Fields
		/// <summary>Wherther the document should be re-highlighted or not.</summary>
		public bool H_IsInvalid = false;

		/// <summary>Beginning position of the range to be highlighted.</summary>
		public int H_InvalidRangeBegin = Int32.MaxValue;

		/// <summary>Ending position of the range to be highlighted.</summary>
		public int H_InvalidRangeEnd = 0;

		/// <summary>Beginning position of the range which was already highlighted.</summary>
		public int H_ValidRangeBegin = 0;

		/// <summary>Ending position of the range which was already highlighted.</summary>
		public int H_ValidRangeEnd = 0;

		/// <summary>Index of a matched brackets; Index of a bracket after caret, counterpart of
		/// it, a bracket before caret, and counterpart of it.</summary>
		public readonly int[] MatchedBracketIndexes = {-1, -1, -1, -1};
		#endregion

		#region Init / Dispose
		public ViewParam()
		{
			PLHI = new SplitArray<int>( 128, 128 ); // this was 'physical line head indexes' in past so its name starts with 'P'
			PLHI.Add( 0 );
			MaxLineNumber = 9999;
			LastModifiedTime = DateTime.MinValue;
		}
		#endregion

		#region View common properties
		public int DesiredColumnX { get; set; }
		public int FirstVisibleLine { get; set; }
		public int ScrollPosX { get; set; }
		public int MaxLineNumber { get; set; }
		#endregion

		#region PropView specific parameters
		public int PrevAnchorLine { get; set; }
		public int PrevCaretLine { get; set; }
		public int PrevHRulerVirX { get; set; }
		#endregion

		#region PropWrapView specific parameters
		public SplitArray<int> PLHI { get; private set; }
		public int LastTextAreaWidth { get; set; }
		public int LastFontHashCode { get; set; }
		public DateTime LastModifiedTime { get; set; }
		#endregion

		#region Selection
		public int CaretIndex = 0;
		public int AnchorIndex = 0;
		public int OriginalAnchorIndex = -1;
		public int LineSelectionAnchor1 = -1;
		public int LineSelectionAnchor2 = -1; // temporary variable holding selection anchor on expanding line selection backward
		public int[] RectSelectRanges = null;
		public TextDataType SelectionMode = TextDataType.Normal;
		#endregion
	}
}
