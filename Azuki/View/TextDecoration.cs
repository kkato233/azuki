// file: TextDecoration.cs
// brief: Text decoration classes.
// author: YAMAMOTO Suguru
// update: 2010-11-q1
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	public class TextDecoration
	{
		static TextDecoration _None = null;

		protected TextDecoration()
		{}

		public static TextDecoration None
		{
			get
			{
				if( _None == null )
				{
					_None = new TextDecoration();
				}
				return _None;
			}
		}
	}

	public class UnderlineTextDecoration : TextDecoration
	{
		LineStyle _LineStyle;
		Color _LineColor;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public UnderlineTextDecoration( LineStyle lineStyle, Color lineColor )
		{
			_LineStyle = lineStyle;
			_LineColor = lineColor;
		}

		/// <summary>
		/// Gets or sets style of underline.
		/// </summary>
		public LineStyle LineStyle
		{
			get{ return _LineStyle; }
			set{ _LineStyle = value; }
		}

		/// <summary>
		/// Gets or sets color of underline.
		/// </summary>
		public Color LineColor
		{
			get{ return _LineColor; }
			set{ _LineColor = value; }
		}
	}

	/// <summary>
	/// - EXPERIMENTAL - Decorates text with an transparent rectangle.
	/// </summary>
	public class OutlineTextDecoration : TextDecoration
	{
		Color _LineColor;

		public OutlineTextDecoration( Color outlineColor )
		{
			LineColor = outlineColor;
		}

		public Color LineColor
		{
			get{ return _LineColor; }
			set{ _LineColor = value; }
		}
	}

	/// <summary>
	/// Indicates style of line for text decoration.
	/// </summary>
	[Flags]
	public enum LineStyle
	{
		/// <summary>Does not draw line.</summary>
		None,

		/// <summary>Solid line.</summary>
		Solid,

		/// <summary>Doubled line.</summary>
		Double,

		/// <summary>Dashed line.</summary>
		Dashed,

		/// <summary>Line written with many dots.</summary>
		Dotted,

		/// <summary>Waved line.</summary>
		Waved
	}

}
