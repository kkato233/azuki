// file: ColorScheme.cs
// brief: color set
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-11-03
//=========================================================
using System.Collections.Generic;
using System.Drawing;

namespace Sgry.Azuki
{
	/// <summary>
	/// Pair of foreground/background colors.
	/// </summary>
	public class ColorPair
	{
		#region Fields
		/// <summary>Foreground color.</summary>
		public Color Fore;

		/// <summary>Background color.</summary>
		public Color Back;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ColorPair()
			: this( Color.Black, Color.White )
		{}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ColorPair( Color fore, Color back )
		{
			Fore = fore;
			Back = back;
		}
		#endregion
	}

	/// <summary>
	/// Color set used for drawing.
	/// </summary>
	public class ColorScheme
	{
		Dictionary< byte, ColorPair > _Colors = new Dictionary< byte, ColorPair >();

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
		/// Gets or sets color pair associated with given char-class.
		/// </summary>
		public ColorPair this[ CharClass klass ]
		{
			get{ return GetColor(klass); }
			set{ SetColor(klass, value); }
		}

		/// <summary>
		/// Gets color pair for a char-class.
		/// </summary>
		public ColorPair GetColor( CharClass klass )
		{
			return _Colors[klass.Id];
		}

		/// <summary>
		/// Sets color pair for a char-class.
		/// </summary>
		public void SetColor( CharClass klass, ColorPair colorPair )
		{
			_Colors[klass.Id] = colorPair;
			if( klass == CharClass.Normal )
			{
				ForeColor = colorPair.Fore;
				BackColor = colorPair.Back;
			}
		}
		/// <summary>
		/// Gets default color scheme.
		/// </summary>
		public static ColorScheme Default
		{
			get
			{
				ColorScheme scheme = new ColorScheme();
				scheme.SetDefault();
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

			this[ CharClass.Normal ] = new ColorPair( Color.Black, bgcolor );
			this[ CharClass.Number ] = new ColorPair( Color.Black, bgcolor );
			this[ CharClass.String ] = new ColorPair( Color.Teal, bgcolor );
			this[ CharClass.Keyword ] = new ColorPair( Color.Blue, bgcolor );
			this[ CharClass.Keyword2 ] = new ColorPair( Color.Maroon, bgcolor );
			this[ CharClass.Keyword3 ] = new ColorPair( Color.Navy, bgcolor );
			this[ CharClass.PreProcessor ] = new ColorPair( Color.Purple, bgcolor );
			this[ CharClass.Comment ] = new ColorPair( Color.Green, bgcolor );
			this[ CharClass.DocComment ] = new ColorPair( Color.Gray, bgcolor );

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
		public Color ForeColor;

		/// <summary>
		/// Background color of normal text.
		/// </summary>
		public Color BackColor;

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
