// file: ColorScheme.cs
// brief: color set
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
	/// First part is a set of pairs of foreground color and background color
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
	/// Note that if background color for a CharClass except CharClass.Normal
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
		TextDecoration[] _MarkingDecorations = new TextDecoration[ Marking.MaxID+1 ];

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ColorScheme()
		{
			SetDefault();
		}

		/// <summary>
		/// Creates a copy of another instance.
		/// </summary>
		public ColorScheme( ColorScheme another )
		{
			Array.Copy( another._ForeColors, _ForeColors, _ForeColors.Length );
			Array.Copy( another._BackColors, _BackColors, _BackColors.Length );
			Array.Copy( another._MarkingDecorations, _MarkingDecorations, _MarkingDecorations.Length );
			SelectionFore = another.SelectionFore;
			SelectionBack = another.SelectionBack;
			WhiteSpaceColor = another.WhiteSpaceColor;
			EolColor = another.EolColor;
			EofColor = another.EofColor;
			HighlightColor = another.HighlightColor;
			LineNumberFore = another.LineNumberFore;
			LineNumberBack = another.LineNumberBack;
			DirtyLineBar = another.DirtyLineBar;
			CleanedLineBar = another.CleanedLineBar;
			RightEdgeColor = another.RightEdgeColor;
			MatchedBracketFore = another.MatchedBracketFore;
			MatchedBracketBack = another.MatchedBracketBack;
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
		/// Note that, although Azuki does not use actually set background color value
		/// if it was Color.Transparent,
		/// this method returns the actually set value (Color.Transparent) in the case.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.ColorScheme.SetColor">ColorScheme.SetColor method</seealso>
		public void GetColor( CharClass klass, out Color fore, out Color back )
		{
			fore = _ForeColors[ (byte)klass ];
			back = _BackColors[ (byte)klass ];
		}

		/// <summary>
		/// Sets color pair for a character-class.
		/// </summary>
		/// <param name="klass">The color-pair associated with this character-class will be got.</param>
		/// <param name="fore">Foreground color used to draw characters marked as the character-class.</param>
		/// <param name="back">Background color used to draw characters marked as the character-class.</param>
		/// <exception cref="System.ArgumentException">
		///		Color.Transparent was set to CharClass.Normal.
		///	</exception>
		/// <remarks>
		/// <para>
		/// This method sets a pair of colors which is associated with
		/// CharClass specified by parameter '<paramref name="klass"/>.'
		/// </para>
		/// <para>
		/// Note that if Color.Transparent was set for background color
		/// of a CharClass except CharClass.Normal,
		/// Azuki uses the color of
		/// <see cref="Sgry.Azuki.ColorScheme.BackColor">BackColor property</see>
		/// for drawing tokens of the CharClass.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetColor">ColorScheme.GetColor method</seealso>
		public void SetColor( CharClass klass, Color fore, Color back )
		{
			if( klass == CharClass.Normal
				&& (fore == Color.Transparent || back == Color.Transparent) )
				throw new ArgumentException( "foreground color or background color for CharClass.Normal must not be Color.Transparent." );

			_ForeColors[ (byte)klass ] = fore;
			_BackColors[ (byte)klass ] = back;
		}

		/// <summary>
		/// Gets how text marked with specified ID should be decorated.
		/// </summary>
		/// <param name="markingID">The ID of the marking.</param>
		/// <returns>TextDecoration object associated with specified ID, or null.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		Parameter <paramref name="markingID"/> is out of valid range.
		///	</exception>
		/// <exception cref="System.ArgumentException">
		///		Parameter <paramref name="markingID"/> is not registered to Marking class.
		///	</exception>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(int[])">ColorScheme.GetMarkingDecorations(int[]) method</seealso>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(uint)">ColorScheme.GetMarkingDecorations(uint) method</seealso>
		/// <seealso cref="Sgry.Azuki.ColorScheme.SetMarkingDecoration">ColorScheme.SetMarkingDecoration method</seealso>
		public TextDecoration GetMarkingDecoration( int markingID )
		{
			if( Marking.GetMarkingInfo(markingID) == null )
				throw new ArgumentException( "Specified markingID is not registered. (markingID:"+markingID+")", "markingID" );

			return _MarkingDecorations[markingID];
		}

		/// <summary>
		/// Gets multiple TextDecoration at once
		/// associated with specified marking IDs.
		/// </summary>
		/// <param name="markingIDs">The array of marking ID.</param>
		/// <returns>An array of TextDecoration objects.</returns>
		/// <exception cref="System.ArgumentNullException">
		///		Parameter <paramref name="markingIDs"/> is null.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		Parameter <paramref name="markingIDs"/> contains a value
		///		which is out of valid range.
		///	</exception>
		/// <exception cref="System.ArgumentException">
		///		Parameter <paramref name="markingIDs"/> contains a value
		///		which is not registered to Marking class.
		///	</exception>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetMarkingDecoration">ColorScheme.GetMarkingDecoration method</seealso>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(uint)">ColorScheme.GetMarkingDecorations(uint) method</seealso>
		/// <seealso cref="Sgry.Azuki.ColorScheme.SetMarkingDecoration">ColorScheme.SetMarkingDecoration method</seealso>
		public TextDecoration[] GetMarkingDecorations( int[] markingIDs )
		{
			if( markingIDs == null )
				throw new ArgumentNullException( "markingIDs" );

			List<TextDecoration> styles = new List<TextDecoration>( Marking.MaxID+1 );

			foreach( int id in markingIDs )
			{
				styles.Add( GetMarkingDecoration(id) );
			}

			return styles.ToArray();
		}

		/// <summary>
		/// Gets decorations associated with marking IDs from bit mask (internal representation).
		/// </summary>
		/// <param name="markingBitMask">
		///		When this method returns,
		///		an array of decorations associated with markings
		///		represented by this bit mask will be retrieved.
		/// </param>
		/// <returns>An array of decoration information, or an empty array.</returns>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetMarkingDecoration">ColorScheme.GetMarkingDecoration method</seealso>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(int[])">ColorScheme.GetMarkingDecorations(int[]) method</seealso>
		/// <seealso cref="Sgry.Azuki.ColorScheme.SetMarkingDecoration">ColorScheme.SetMarkingDecoration method</seealso>
		public TextDecoration[] GetMarkingDecorations( uint markingBitMask )
		{
			List<TextDecoration> styles = new List<TextDecoration>( Marking.MaxID+1 );

			for( int i=0; i<=Marking.MaxID; i++ )
			{
				if( (markingBitMask & 0x01) != 0 )
				{
					styles.Add( _MarkingDecorations[i] );
				}
				markingBitMask >>= 1;
			}

			return styles.ToArray();
		}

		/// <summary>
		/// Sets how text parts marked with specified ID should be decorated.
		/// </summary>
		/// <param name="markingID">The marking ID.</param>
		/// <param name="decoration">
		///		TextDecoration object to be associated with <paramref name="markingID"/>.
		///		If null was specified, TextDecoration.None will be used internally.
		/// </param>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetMarkingDecoration">ColorScheme.GetMarkingDecoration method</seealso>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(uint)">ColorScheme.GetMarkingDecorations(uint) method</seealso>
		/// <seealso cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(int[])">ColorScheme.GetMarkingDecorations(int[]) method</seealso>
		public void SetMarkingDecoration( int markingID, TextDecoration decoration )
		{
			if( Marking.GetMarkingInfo(markingID) == null )
				throw new ArgumentException( "Specified marking ID is not registered. (markingID:"+markingID+")", "markingID" );

			if( decoration == null )
			{
				decoration = TextDecoration.None;
			}
			_MarkingDecorations[markingID] = decoration;
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
			Color oreillyPerl = Color.FromArgb( 0x00, 0x97, 0xc2 );

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
			SetColor( CharClass.Regex, oreillyPerl, Color.Transparent );
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
			SetColor( CharClass.CDataSection, Color.Gray, Color.Transparent );
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
			this.MatchedBracketFore = Color.Transparent;
			this.MatchedBracketBack = Color.FromArgb( 0x93, 0xff, 0xff );

			for( int i=0; i<=Marking.MaxID; i++ )
			{
				_MarkingDecorations[i] = TextDecoration.None;
			}
			SetMarkingDecoration( Marking.Uri,
								  new UnderlineTextDecoration(LineStyle.Solid, Color.Transparent) );
		}
		#endregion

		#region Properties
		/// <summary>
		/// Foreground color of normal text.
		/// </summary>
		/// <exception cref="System.ArgumentException">Color.Transparent was set.</exception>
		public Color ForeColor
		{
			get{ return _ForeColors[0]; }
			set
			{
				if( value == Color.Transparent )
					throw new ArgumentException( "foreground color for CharClass.Normal must not be Color.Transparent." );
				_ForeColors[0] = value;
			}
		}

		/// <summary>
		/// Background color of normal text.
		/// </summary>
		/// <exception cref="System.ArgumentException">Color.Transparent was set.</exception>
		public Color BackColor
		{
			get{ return _BackColors[0]; }
			set
			{
				if( value == Color.Transparent )
					throw new ArgumentException( "background color for CharClass.Normal must not be Color.Transparent." );
				_BackColors[0] = value;
			}
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

		/// <summary>
		/// Color of the bracket which is matching to the bracket at caret.
		/// </summary>
		public Color MatchedBracketFore;

		/// <summary>
		/// Background color of the bracket which is matching to the bracket at caret.
		/// </summary>
		public Color MatchedBracketBack;
		#endregion
	}
}
