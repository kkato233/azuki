// file: IView.cs
// brief: Interface for view implementations.
// author: YAMAMOTO Suguru
// update: 2010-03-20
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	/// <summary>
	/// Interface for view implementations.
	/// </summary>
	public interface IView : IDisposable
	{
		#region Properties
		/// <summary>
		/// Gets or sets the document displayed in this view.
		/// </summary>
		Document Document
		{
			get;
		}

		/// <summary>
		/// Gets number of the physical lines.
		/// </summary>
		/// <remarks>
		/// Through this property,
		/// number of the physical lines in this document can be retrieved.
		/// "Physical line" here means a text line drawn as a graphc
		/// and differs from "logical line" (strings simply separated by EOL codes).
		/// To retrieve count of the logical lines,
		/// use <see cref="Sgry.Azuki.Document.LineCount">Document.LineCount</see>
		/// instead.
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.LineCount">Document.LineCount</seealso>
		int LineCount
		{
			get;
		}
		#endregion

		/// <summary>
		/// Gets length of the pysical line.
		/// </summary>
		/// <param name="lineIndex">Index of the line of which to get the length.</param>
		/// <returns>Length of the specified line in character count.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		int GetLineLength( int lineIndex );

		#region Drawing Options
		/// <summary>
		/// Gets or sets top margin of the view in pixel.
		/// </summary>
		int TopMargin
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets left margin of the view in pixel.
		/// </summary>
		int LeftMargin
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets type of the indicator on the horizontal ruler.
		/// </summary>
		HRulerIndicatorType HRulerIndicatorType
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets view drawing options flags.
		/// </summary>
		DrawingOption DrawingOption
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether the current line would be drawn with underline or not.
		/// </summary>
		bool HighlightsCurrentLine
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to show line number or not.
		/// </summary>
		bool ShowLineNumber
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to show horizontal ruler or not.
		/// </summary>
		bool ShowsHRuler
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to show 'dirt bar' or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether to show 'dirt bar' or not.
		/// The dirt bar is graphically a thin bar at right end of the line number area
		/// that indicates the dirty state of each text line.
		/// The state of line is one of the following states.
		/// </para>
		/// <list>
		///		<item>LineDirtyState.Clean: the line is not modified yet.</item>
		///		<item>LineDirtyState.Dirty: the line is modified and not saved.</item>
		///		<item>LineDirtyState.Cleaned: the line is modified but saved.</item>
		/// </list>
		/// <para>
		/// Color of each line dirty state can be customized by setting
		/// ColorScheme.DirtyLineBar, ColorScheme.CleanedLineBar.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.LineDirtyState">LineDirtyState enum</seealso>
		/// <seealso cref="Sgry.Azuki.Document.GetLineDirtyState">Document.GetLineDirtyState method</seealso>
		bool ShowsDirtBar
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw half-width space with special graphic or not.
		/// </summary>
		bool DrawsSpace
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw full-width space with special graphic or not.
		/// </summary>
		bool DrawsFullWidthSpace
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw tab character with special graphic or not.
		/// </summary>
		bool DrawsTab
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw EOL code with special graphic or not.
		/// </summary>
		bool DrawsEolCode
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw EOF mark or not.
		/// </summary>
		bool DrawsEofMark
		{
			get; set;
		}

		/// <summary>
		/// Color set used for displaying text.
		/// </summary>
		ColorScheme ColorScheme
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets tab width in count of space chars.
		/// </summary>
		int TabWidth
		{
			get; set;
		}

		/// <summary>
		/// Gets width of tab character (U+0009) in pixel.
		/// </summary>
		int TabWidthInPx
		{
			get;
		}

		/// <summary>
		/// Gets width of space character (U+0020) in pixel.
		/// </summary>
		int SpaceWidthInPx
		{
			get;
		}
		#endregion

		#region Desired Column Management
		/// <summary>
		/// Sets column index of the current caret position to "desired column" value.
		/// </summary>
		/// <remarks>
		/// When the caret moves up or down,
		/// Azuki tries to set next caret's column index to this value.
		/// </remarks>
		void SetDesiredColumn();

		/// <summary>
		/// Gets current "desired column" value.
		/// </summary>
		/// <remarks>
		/// When the caret moves up or down,
		/// Azuki tries to set next caret's column index to this value.
		/// </remarks>
		int GetDesiredColumn();
		#endregion

		#region Position / Index Conversion
		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		Point GetVirPosFromIndex( int index );

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		Point GetVirPosFromIndex( int lineIndex, int columnIndex );

		/// <summary>
		/// Gets char-index of the char at the point specified by location in the virtual space.
		/// </summary>
		/// <returns>The index of the character at specified location.</returns>
		int GetIndexFromVirPos( Point pt );

		/// <summary>
		/// Converts a coordinate in virtual space to a coordinate in client area.
		/// </summary>
		void VirtualToScreen( ref Point pt );

		/// <summary>
		/// Converts a coordinate in client area to a coordinate in virtual space.
		/// </summary>
		void ScreenToVirtual( ref Point pt );

		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		int GetLineHeadIndex( int lineIndex );

		/// <summary>
		/// Gets the index of the first char in the physical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		int GetLineHeadIndexFromCharIndex( int charIndex );

		/// <summary>
		/// Calculates physical line index from char-index.
		/// </summary>
		/// <param name="charIndex">The index of the line which contains the char at this parameter will be calculated.</param>
		/// <returns>The index of the line which contains the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		int GetLineIndexFromCharIndex( int charIndex );

		/// <summary>
		/// Calculates physical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex );

		/// <summary>
		/// Calculates char-index from physical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex );

		/// <summary>
		/// Calculates and returns text ranges that will be selected by specified rectangle.
		/// </summary>
		/// <param name="selRect">Rectangle to be used to specify selection target.</param>
		/// <returns>Array of indexes (1st begin, 1st end, 2nd begin, 2nd end, ...)</returns>
		/// <remarks>
		/// <para>
		/// (This method is basically for internal use.
		/// I do not recommend to use this from outside of Azuki.)
		/// </para>
		/// <para>
		/// This method calculates text ranges which will be selected by given rectangle.
		/// Because mapping of character indexes and graphical position (layout) are
		/// executed by view implementations, the result of this method will be changed
		/// according to the interface implementation.
		/// </para>
		/// <para>
		/// Return value of this method is an array of text indexes
		/// that is consisted with beginning index of first text range (row),
		/// ending index of first text range,
		/// beginning index of second text range,
		/// ending index of second text range and so on.
		/// </para>
		/// </remarks>
		int[] GetRectSelectRanges( Rectangle selRect );

		/// <summary>
		/// Calculates location of character at specified index in horizontal ruler index.
		/// </summary>
		/// <param name="charIndex">The index of the character to calculate its location.</param>
		/// <returns>Horizontal ruler index of the character.</returns>
		/// <remarks>
		/// <para>
		/// This method calculates location of character at specified index
		/// in horizontal ruler index.
		/// </para>
		/// <para>
		/// 'Horizontal ruler index' here means how many small lines drawn on the horizontal ruler
		/// exist between left-end of the text area
		/// and the character at index specified by <paramref name="charIndex"/>.
		/// This value is zero-based index.
		/// </para>
		/// </remarks>
		int GetHRulerIndex( int charIndex );

		/// <summary>
		/// Calculates location of character at specified index in horizontal ruler index.
		/// </summary>
		/// <param name="lineIndex">The line index of the character to calculate its location.</param>
		/// <param name="columnIndex">The column index of the character to calculate its location.</param>
		/// <returns>Horizontal ruler index of the character.</returns>
		/// <remarks>
		/// <para>
		/// This method calculates location of character at specified index
		/// in horizontal ruler index.
		/// </para>
		/// <para>
		/// 'Horizontal ruler index' here means how many small lines drawn on the horizontal ruler
		/// exist between left-end of the text area
		/// and the character at index specified by <paramref name="charIndex"/>.
		/// This value is zero-based index.
		/// </para>
		/// </remarks>
		int GetHRulerIndex( int lineIndex, int columnIndex );
		#endregion

		#region Operations
		/// <summary>
		/// Scroll to where the caret is.
		/// </summary>
		void ScrollToCaret();

		/// <summary>
		/// Scroll vertically.
		/// </summary>
		void Scroll( int lineDelta );

		/// <summary>
		/// Scroll horizontally.
		/// </summary>
		void HScroll( int columnDelta );

		/// <summary>
		/// Requests to invalidate whole area.
		/// </summary>
		void Invalidate();

		/// <summary>
		/// Requests to invalidate specified area.
		/// </summary>
		/// <param name="rect">rectangle area to be invalidate (in client area coordinate)</param>
		void Invalidate( Rectangle rect );

		/// <summary>
		/// Requests to invalidate area covered by given text range.
		/// </summary>
		/// <param name="beginIndex">Begin text index of the area to be invalidated.</param>
		/// <param name="endIndex">End text index of the area to be invalidated.</param>
		void Invalidate( int beginIndex, int endIndex );

		/// <summary>
		/// Sets font size to larger one.
		/// </summary>
		void ZoomIn();

		/// <summary>
		/// Sets font size to smaller one.
		/// </summary>
		void ZoomOut();
		#endregion

		#region States
		/// <summary>
		/// Gets or sets index of the line which is displayed at top of this view.
		/// </summary>
		int FirstVisibleLine
		{
			get; set;
		}
		#endregion

		#region Appearance
		/// <summary>
		/// Gets or sets the font used for drawing text.
		/// </summary>
		FontInfo FontInfo
		{
			get; set;
		}

		/// <summary>
		/// Gets height of each lines in pixel.
		/// </summary>
		int LineHeight
		{
			get;
		}

		/// <summary>
		/// Gets or sets size of padding between lines in pixel.
		/// </summary>
		int LinePadding
		{
			get; set;
		}

		/// <summary>
		/// Gets distance between lines in pixel.
		/// </summary>
		int LineSpacing
		{
			get;
		}

		/// <summary>
		/// Gets width of the line number area in pixel.
		/// </summary>
		int LineNumAreaWidth
		{
			get;
		}

		/// <summary>
		/// Gets height of the horizontal ruler.
		/// </summary>
		int HRulerHeight
		{
			get;
		}

		/// <summary>
		/// Gets distance between lines on the horizontal ruler.
		/// </summary>
		int HRulerUnitWidth
		{
			get;
		}

		/// <summary>
		/// Gets or sets width of the virtual text area.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
		/// <remarks>
		///	<para>
		/// This property accesses the width of the *virtual* text area.
		/// Text area indicates the logical space where Azuki draws text content
		/// and is not the area which is graphically visible;
		/// visible text area is a portion of the text area.
		/// </para>
		/// <para>
		/// Since Azuki only draws text in the text area,
		/// width of it affectes how text lines were drawn.
		/// If <see cref="Sgry.Azuki.IUserInterface.ViewType">
		/// IUserInterface.ViewType</see> was set to
		/// <see cref="Sgry.Azuki.ViewType.Proportional">
		/// ViewType.Proportional</see>,
		/// the width will be expanded as needed
		/// to continue drawing a long logical line.
		/// If <see cref="Sgry.Azuki.IUserInterface.ViewType">
		/// IUserInterface.ViewType</see> was set to
		/// <see cref="Sgry.Azuki.ViewType.WrappedProportional">
		/// ViewType.WrappedProportional</see>,
		/// each logical text lines will be wrapped at right end of the text area.
		/// </para>
		/// <para>
		/// Note that text area does not contain line-number area.
		/// </para>
		/// </remarks>
		int TextAreaWidth
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets size of the currently visible area.
		/// This value includes the size of both line-number area and visible text area.
		/// </summary>
		Size VisibleSize
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets size of the currently visible size of the text area (line number area is not included).
		/// </summary>
		Size VisibleTextAreaSize
		{
			get;
		}
		#endregion

		#region Coordinates of Graphical Parts
		/// <summary>
		/// Gets X coordinate in client area of line number area.
		/// </summary>
		int XofLineNumberArea
		{
			get;
		}

		/// <summary>
		/// Gets X coordinate in client area of dirt bar area.
		/// </summary>
		int XofDirtBar
		{
			get;
		}

		/// <summary>
		/// Gets X coordinate in client area of left margin.
		/// </summary>
		int XofLeftMargin
		{
			get;
		}

		/// <summary>
		/// Gets X coordinate in client area of text area.
		/// </summary>
		int XofTextArea
		{
			get;
		}

		/// <summary>
		/// Gets Y coordinate in client area of horizontal ruler.
		/// </summary>
		int YofHRuler
		{
			get;
		}

		/// <summary>
		/// Gets Y coordinate in client area of top margin.
		/// </summary>
		int YofTopMargin
		{
			get;
		}

		/// <summary>
		/// Gets Y coordinate in client area of text area.
		/// </summary>
		int YofTextArea
		{
			get;
		}
		#endregion
	}
}
