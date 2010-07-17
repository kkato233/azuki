// file: ColorScheme.cs
// brief: color set
// author: YAMAMOTO Suguru
// update: 2010-07-17
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
	/// ColorScheme defines color set used for drawing document by Azuki.
	/// </para>
	/// <para>
	/// ColorScheme is consisted with two major parts.
	/// First part is a set of pairs of fore-ground color and back-ground color
	/// associated with each <see cref="Sgry.Azuki.CharClass">CharClass</see>.
	/// The view objects will reference this
	/// to determine which color should be used for each token by its character-class.
	/// This set can be accessed through
	/// <see cref="Sgry.Azuki.ColorScheme.GetColor">GetColor method</see>
	/// or <see cref="Sgry.Azuki.ColorScheme.SetColor">SetColor method</see>.
	/// Second part is color values used to draw graphic
	/// such as selected text, line numbers, control characters and so on.
	/// Values of this part are defined as
	/// public properties of this class.
	/// </para>
	/// <para>
	/// Note that if back-ground color for a CharClass except CharClass.Normal
	/// was set to Color.Transparent,
	/// Azuki uses the color of
	/// <see cref="Sgry.Azuki.ColorScheme.BackColor">BackColor property</see>
	/// for drawing tokens of the character-class.
	/// </para>
	/// </remarks>
	/// <seealso cref="Sgry.Azuki.CharClass">CharClass enum</seealso>
	/// <seealso cref="Sgry.Azuki.ColorScheme.GetColor">GetColor method</seealso>
	/// <seealso cref="Sgry.Azuki.ColorScheme.SetColor">SetColor method</seealso>
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
		/// Gets color pair for a character-class.
		/// </summary>
		/// <param name="klass">The color-pair associated with this character-class will be got.</param>
		/// <param name="fore">Foreground color used to draw characters marked as the character-class.</param>
		/// <param name="back">Background color used to draw characters marked as the character-class.</param>
		/// <remarks>
		/// <para>
		/// This method gets a pair of colors which is associated with
		/// CharClass specified by parameter '<paramref name="klass"/>.'
		/// </para>
		/// <para>
		/// Note that, although Azuki does not use actually set back-ground color value
		/// if it was Color.Transparent,
		/// this method returns the actually set value (Color.Transparent) in the case.
		/// </para>
		/// </remarks>
		public void GetColor( CharClass klass, out Color fore, out Color back )
		{
			fore = _ForeColors[ (byte)klass ];
			back = _BackColors[ (byte)klass ];
		}

		/// <summary>
		/// Sets color pair for a character-class.
		/// </summary>
		/// <param name="klass">The color-pair associated with this character-class will be got.</param>
		/// <param name="fore">Fore-ground color used to draw characters marked as the character-class.</param>
		/// <param name="back">Back-ground color used to draw characters marked as the character-class.</param>
		/// <remarks>
		/// <para>
		/// This method sets a pair of colors which is associated with
		/// CharClass specified by parameter '<paramref name="klass"/>.'
		/// </para>
		/// <para>
		/// Note that if Color.Transparent was set for back-ground color
		/// of a CharClass except CharClass.Normal,
		/// Azuki uses the color of
		/// <see cref="Sgry.Azuki.ColorScheme.BackColor">BackColor property</see>
		/// for drawing tokens of the CharClass.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		///		Parameter '<paramref name="fore"/>' is Color.Transparent.
		///		- or -
		///		Parameter '<paramref name="back"/>' is Color.Transparent but parameter '<paramref name="klass"/>' is CharClass.Normal.
		/// </exception>
		public void SetColor( CharClass klass, Color fore, Color back )
		{
			if( fore == Color.Transparent )
				throw new ArgumentException( "fore-ground color must not be Color.Transparent.", "fore" );
			if( klass == CharClass.Normal && back == Color.Transparent )
				throw new ArgumentException( "back-ground color for CharClass.Normal must not be Color.Transparent.", "back" );

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
			Color azuki = Color.FromArgb( 0x92, 0x62, 0x57 ); // azuki iro (japanese)
			Color shin_bashi = Color.FromArgb( 0x74, 0xa9, 0xd6 ); // shin-bashi iro (japanese)
			Color hana_asagi = Color.FromArgb( 0x1b, 0x77, 0x92 ); // hana-asagi iro (japanese)
			Color waka_midori = Color.FromArgb( 0xa8, 0xef, 0xaf ); // waka-midori iro (japanese)
			Color himawari = Color.FromArgb( 0xff, 0xf1, 0x0f ); // himawari iro (japanese)
			Color sax_blue = Color.FromArgb( 0x46, 0x48, 0xb8 );
			
			SetColor( CharClass.Normal, Color.Black, bgcolor );
			SetColor( CharClass.Number, Color.Black, Color.Transparent );
			SetColor( CharClass.String, Color.Teal, Color.Transparent );
			SetColor( CharClass.Comment, Color.Green, Color.Transparent );
			SetColor( CharClass.DocComment, Color.Gray, Color.Transparent );
			SetColor( CharClass.Keyword, Color.Blue, Color.Transparent );
			SetColor( CharClass.Keyword2, Color.Maroon, Color.Transparent );
			SetColor( CharClass.Keyword3, Color.Navy, Color.Transparent );
			SetColor( CharClass.Macro, Color.Purple, Color.Transparent );
			SetColor( CharClass.Character, Color.Purple, Color.Transparent );
			SetColor( CharClass.Type, Color.BlueViolet, Color.Transparent );
			SetColor( CharClass.Regex, Color.Teal, Color.Transparent );
			SetColor( CharClass.Annotation, Color.Gray, Color.Transparent );
			SetColor( CharClass.Selecter, Color.Navy, Color.Transparent );
			SetColor( CharClass.Property, Color.Blue, Color.Transparent );
			SetColor( CharClass.Value, Color.Red, Color.Transparent );
			SetColor( CharClass.ElementName, Color.Maroon, Color.Transparent );
			SetColor( CharClass.Entity, Color.Gray, Color.Transparent );
			SetColor( CharClass.Attribute, Color.Navy, Color.Transparent );
			SetColor( CharClass.AttributeValue, Color.Navy, Color.Transparent );
			SetColor( CharClass.EmbededScript, Color.Gray, Color.Transparent );
			SetColor( CharClass.Delimiter, Color.Blue, Color.Transparent );
			SetColor( CharClass.CDataSection, Color.Silver, Color.Transparent );
			SetColor( CharClass.LatexBracket, Color.Teal, Color.Transparent );
			SetColor( CharClass.LatexCommand, sax_blue, Color.Transparent );
			SetColor( CharClass.LatexCurlyBracket, Color.Maroon, Color.Transparent );
			SetColor( CharClass.LatexEquation, Color.Maroon, Color.Transparent );
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
			this.EofColor = shin_bashi;
			this.HighlightColor = azuki;
			this.LineNumberFore = hana_asagi;
			this.LineNumberBack = Color.FromArgb( 0xef, 0xef, 0xff );
			this.DirtyLineBar = himawari;
			this.CleanedLineBar = waka_midori;
			this.RightEdgeColor = Color.FromArgb( 0xDD, 0xDE, 0xD3 ); // ivory
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
		/// Color of EOF chars.
		/// </summary>
		public Color EofColor;

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

		/// <summary>
		/// Color of the dirt bar at left of a modified line.
		/// </summary>
		public Color DirtyLineBar;

		/// <summary>
		/// Color of the dirt bar at left of a modified but saved (cleaned) line.
		/// </summary>
		public Color CleanedLineBar;

		/// <summary>
		/// Color of the right edge of text area (line wrapping edge).
		/// </summary>
		public Color RightEdgeColor;
		#endregion
	}
}
