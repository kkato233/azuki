// file: ColorScheme.cs
// brief: color set
// author: YAMAMOTO Suguru
// update: 2009-07-05
//=========================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// Color set used for drawing text.
	/// </summary>
	/// <remarks>
	/// <para>
	/// ColorScheme is a set of pairs of foreground color and background color
	/// and is used to draw tokens of document.
	/// </para>
	/// </remarks>
	/// <seealso cref="Sgry.Azuki.CharClass">CharClass enum</seealso>
	public class ColorScheme
	{
		Color[] _ForeColors = new Color[ Byte.MaxValue ];
		Color[] _BackColors = new Color[ Byte.MaxValue ];

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ColorScheme()
		{
			SetDefault();
		}
		#endregion

		#region Operations
		/// <summary>
		/// Gets color pair for a char-class.
		/// </summary>
		/// <param name="klass">The color-pair associated with this char-class will be got.</param>
		/// <param name="fore">Foreground color used to draw characters marked as the char-class.</param>
		/// <param name="back">Background color used to draw characters marked as the char-class.</param>
		public void GetColor( CharClass klass, out Color fore, out Color back )
		{
			fore = _ForeColors[ (byte)klass ];
			back = _BackColors[ (byte)klass ];
		}

		/// <summary>
		/// Sets color pair for a char-class.
		/// </summary>
		/// <param name="klass">The color-pair associated with this char-class will be got.</param>
		/// <param name="fore">Foreground color used to draw characters marked as the char-class.</param>
		/// <param name="back">Background color used to draw characters marked as the char-class.</param>
		public void SetColor( CharClass klass, Color fore, Color back )
		{
			_ForeColors[ (byte)klass ] = fore;
			_BackColors[ (byte)klass ] = back;
		}

		/// <summary>
		/// Gets default color scheme.
		/// </summary>
		public static ColorScheme Default
		{
			get
			{
				ColorScheme scheme = new ColorScheme();
				//NO_NEED//scheme.SetDefault();
				return scheme;
			}
		}

		/*/// <summary>
		/// Gets high contrast color scheme.
		/// </summary>
		public static ColorScheme HighContrast
		{
			get
			{
			}
		}*/

		void SetDefault()
		{
			Color bgcolor = Color.FromArgb( 0xff, 0xfa, 0xf0 );
			Color azuki = Color.FromArgb( 0x92, 0x62, 0x57 ); // azuki iro
			Color shin_bashi = Color.FromArgb( 0x74, 0xa9, 0xd6 ); // shin-bashi iro (japanese)
			Color hana_asagi = Color.FromArgb( 0x1b, 0x77, 0x92 ); // hana-asagi iro (japanese)
			Color sax_blue = Color.FromArgb( 0x46, 0x48, 0xb8 );
			
			SetColor( CharClass.Normal, Color.Black, bgcolor );
			SetColor( CharClass.Number, Color.Black, bgcolor );
			SetColor( CharClass.String, Color.Teal, bgcolor );
			SetColor( CharClass.Comment, Color.Green, bgcolor );
			SetColor( CharClass.DocComment, Color.Gray, bgcolor );
			SetColor( CharClass.Keyword, Color.Blue, bgcolor );
			SetColor( CharClass.Keyword2, Color.Maroon, bgcolor );
			SetColor( CharClass.Keyword3, Color.Navy, bgcolor );
			SetColor( CharClass.Macro, Color.Purple, bgcolor );
			SetColor( CharClass.Character, Color.Purple, bgcolor );
			SetColor( CharClass.Type, Color.BlueViolet, bgcolor );
			SetColor( CharClass.Regex, Color.Teal, bgcolor );
			SetColor( CharClass.Annotation, Color.Gray, bgcolor );
			SetColor( CharClass.Selecter, Color.Navy, bgcolor );
			SetColor( CharClass.Property, Color.Blue, bgcolor );
			SetColor( CharClass.Value, Color.Red, bgcolor );
			SetColor( CharClass.ElementName, Color.Maroon, bgcolor );
			SetColor( CharClass.Entity, Color.Gray, bgcolor );
			SetColor( CharClass.Attribute, Color.Navy, bgcolor );
			SetColor( CharClass.AttributeValue, Color.Navy, bgcolor );
			SetColor( CharClass.EmbededScript, Color.Gray, bgcolor );
			SetColor( CharClass.Delimiter, Color.Blue, bgcolor );
			SetColor( CharClass.CDataSection, Color.Silver, bgcolor );
			SetColor( CharClass.LatexBracket, Color.Teal, bgcolor );
			SetColor( CharClass.LatexCommand, sax_blue, bgcolor );
			SetColor( CharClass.LatexCurlyBracket, Color.Maroon, bgcolor );
			SetColor( CharClass.LatexEquation, Color.Maroon, bgcolor );
			SetColor( CharClass.Heading1, Color.Black, Color.FromArgb(0xff, 0xff, 0x00) ); // -LOG( 1/1.0 )
			SetColor( CharClass.Heading2, Color.Black, Color.FromArgb(0xff, 0xff, 0x65) ); // -LOG( 1/2.5 )
			SetColor( CharClass.Heading3, Color.Black, Color.FromArgb(0xff, 0xff, 0x99) ); // -LOG( 1/4.0 )
			SetColor( CharClass.Heading4, Color.Black, Color.FromArgb(0xff, 0xff, 0xbc) ); // -LOG( 1/5.5 )
			SetColor( CharClass.Heading5, Color.Black, Color.FromArgb(0xff, 0xff, 0xb7) ); // -LOG( 1/7.0 )
			SetColor( CharClass.Heading6, Color.Black, Color.FromArgb(0xff, 0xff, 0xed) ); // -LOG( 1/8.5 )

			this.SelectionFore = Color.White;
			this.SelectionBack = azuki;
			this.WhiteSpaceColor = Color.Silver;
			this.EolColor = shin_bashi;
			this.HighlightColor = azuki;
			this.LineNumberFore = hana_asagi;
			this.LineNumberBack = Color.FromArgb( 0xef, 0xef, 0xff );
		}
		#endregion

		#region Properties
		/// <summary>
		/// Foreground color of normal text.
		/// </summary>
		public Color ForeColor
		{
			get{ return _ForeColors[0]; }
			set{ _ForeColors[0] = value; }
		}

		/// <summary>
		/// Background color of normal text.
		/// </summary>
		public Color BackColor
		{
			get{ return _BackColors[0]; }
			set{ _BackColors[0] = value; }
		}

		/// <summary>
		/// Color of selected text.
		/// </summary>
		public Color SelectionFore;

		/// <summary>
		/// Background color of selected text.
		/// </summary>
		public Color SelectionBack;

		/// <summary>
		/// Color of white-space chars.
		/// </summary>
		public Color WhiteSpaceColor;

		/// <summary>
		/// Color of EOL chars.
		/// </summary>
		public Color EolColor;

		/// <summary>
		/// Underline color of the line which the caret is on.
		/// </summary>
		public Color HighlightColor;

		/// <summary>
		/// Color of the line number text.
		/// </summary>
		public Color LineNumberFore;

		/// <summary>
		/// Background color of the line number text.
		/// </summary>
		public Color LineNumberBack;
		#endregion
	}
}
