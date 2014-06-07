// file: Platform.cs
// brief: Platform API caller.
// author: YAMAMOTO Suguru
// update: 2011-02-20
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	/// <summary>
	/// The interface for invoking Platform API.
	/// </summary>
	public interface IPlatform
	{
		#region UI Notification
		/// <summary>
		/// Notify user by platform-dependent method
		/// (may be auditory or graphically.)
		/// </summary>
		void MessageBeep();
		#endregion

		#region Clipboard
		/// <summary>
		/// Gets content of the system clipboard.
		/// </summary>
		/// <param name="dataType">The type of the text data in the clipboard</param>
		/// <returns>Text content retrieved from the clipboard if available. Otherwise null.</returns>
		/// <remarks>
		/// This method gets text from the system clipboard.
		/// If stored text data is a special format (line or rectangle,)
		/// its data type will be set to <paramref name="dataType"/> parameter.
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.TextDataType">TextDataType enum</seealso>
		string GetClipboardText( out TextDataType dataType );

		/// <summary>
		/// Sets content of the system clipboard.
		/// </summary>
		/// <param name="text">Text data to set.</param>
		/// <param name="dataType">Type of the data to set.</param>
		/// <remarks>
		/// This method set content of the system clipboard.
		/// If <paramref name="dataType"/> is TextDataType.Normal,
		/// the text data will be just a character sequence.
		/// If <paramref name="dataType"/> is TextDataType.Line or TextDataType.Rectangle,
		/// stored text data would be special format that is compatible with Microsoft Visual Studio.
		/// </remarks>
		void SetClipboardText( string text, TextDataType dataType );
		#endregion

		#region UI parameters
		/// <summary>
		/// It will be regarded as a drag operation by the system
		/// if mouse cursor moved beyond this rectangle.
		/// </summary>
		Size DragSize
		{
			get;
		}
		#endregion

		#region Graphic Interface
		/// <summary>
		/// Gets a graphic device context from a window.
		/// </summary>
		IGraphics GetGraphics( object window );
		#endregion
	}

	/// <summary>
	/// Graphic drawing interface.
	/// </summary>
	public interface IGraphics : IDisposable
	{
		//void Dispose();

		#region Off-screen Rendering
		/// <summary>
		/// Begin using off-screen buffer and cache drawing which will be done after.
		/// </summary>
		/// <param name="paintRect">painting area (used for creating off-screen buffer).</param>
		void BeginPaint( Rectangle paintRect );

		/// <summary>
		/// End using off-screen buffer and flush all drawing results.
		/// </summary>
		void EndPaint();
		#endregion

		#region Graphic Setting
		/// <summary>
		/// Font used for drawing/measuring text.
		/// </summary>
		Font Font
		{
			set;
		}

		/// <summary>
		/// Font used for drawing/measuring text.
		/// </summary>
		FontInfo FontInfo
		{
			set;
		}

		/// <summary>
		/// Foreground color used by drawing APIs.
		/// </summary>
		Color ForeColor
		{
			set;
		}

		/// <summary>
		/// Background color used by drawing APIs.
		/// </summary>
		Color BackColor
		{
			set;
		}

		/// <summary>
		/// Select specified rectangle as a clipping region.
		/// </summary>
		void SetClipRect( Rectangle clipRect );

		/// <summary>
		/// Remove currently selected clipping region.
		/// </summary>
		void RemoveClipRect();
		#endregion

		#region Text Drawing
		/// <summary>
		/// Draws a text.
		/// </summary>
		void DrawText( string text, ref Point position, Color color );

		/// <summary>
		/// Measures graphical size of the specified text.
		/// </summary>
		/// <param name="text">text to measure</param>
		/// <returns>size of the text in the graphic device context</returns>
		Size MeasureText( string text );

		/// <summary>
		/// Measures graphical size of the a text within the specified clipping width.
		/// </summary>
		/// <param name="text">text to measure</param>
		/// <param name="clipWidth">width of the clipping area for rendering text (in pixel unit if the context is screen)</param>
		/// <param name="drawableLength">count of characters which could be drawn within the clipping area width</param>
		/// <returns>size of the text in the graphic device context</returns>
		Size MeasureText( string text, int clipWidth, out int drawableLength );
		#endregion

		#region Graphic Drawing
		/// <summary>
		/// Draws a line with foreground color.
		/// Note that the point where the line extends to will also be painted.
		/// </summary>
		void DrawLine( int fromX, int fromY, int toX, int toY );
		
		/// <summary>
		/// Draws a rectangle with foreground color.
		/// Note that right and bottom edge will also be painted.
		/// </summary>
		void DrawRectangle( int x, int y, int width, int height );

		/// <summary>
		/// Fills a rectangle with background color.
		/// Note that right and bottom edge will also be painted.
		/// </summary>
		void FillRectangle( int x, int y, int width, int height );
		#endregion
	}

	/// <summary>
	/// Information about font.
	/// </summary>
	public class FontInfo
	{
		string _Name;
		int _Size;
		FontStyle _Style;

		#region Properties
		/// <summary>
		/// Font face name of this font.
		/// </summary>
		public string Name
		{
			get{ return _Name; }
			set{ _Name = value; }
		}

		/// <summary>
		/// Size of this font in pt (point).
		/// </summary>
		public int Size
		{
			get{ return _Size; }
			set{ _Size = value; }
		}

		/// <summary>
		/// Style of this font.
		/// </summary>
		public FontStyle Style
		{
			get{ return _Style; }
			set{ _Style = value; }
		}
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public FontInfo()
		{
			_Name = SystemFonts.DefaultFont.Name;
			_Size = (int)SystemFonts.DefaultFont.Size;
			_Style = SystemFonts.DefaultFont.Style;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public FontInfo( string name, int size, FontStyle style )
		{
			_Name = name;
			_Size = size;
			_Style = style;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public FontInfo( FontInfo fontInfo )
		{
			if( fontInfo == null )
				throw new ArgumentNullException( "fontInfo" );

			_Name = fontInfo.Name;
			_Size = fontInfo.Size;
			_Style = fontInfo.Style;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public FontInfo( Font font )
		{
			if( font == null )
				throw new ArgumentNullException( "font" );

			_Name = font.Name;
			_Size = (int)font.Size;
			_Style = font.Style;
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets user readable text of this font information.
		/// </summary>
		public override string ToString()
		{
			return String.Format( "\"{0}\", {1}, {2}", _Name, _Size, _Style );
		}

		/// <summary>
		/// Creates new instance of System.Drawing.Font with same information.
		/// </summary>
		/// <exception cref="System.ArgumentException">Failed to create System.Font object.</exception>
		public Font ToFont()
		{
			try
			{
				return new Font( Name, Size, Style );
			}
			catch( ArgumentException ex )
			{
				// ArgumentException will be thrown
				// if the font specified the name does not support
				// specified font style.
				// try to find available font style for the font.
				FontStyle[] styles = new FontStyle[5];
				styles[0] = FontStyle.Regular;
				styles[1] = FontStyle.Bold;
				styles[2] = FontStyle.Italic;
				styles[3] = FontStyle.Underline;
				styles[4] = FontStyle.Strikeout;
				foreach( FontStyle s in styles )
				{
					try
					{
						return new Font( Name, Size, s );
					}
					catch
					{}
				}

				// there is nothing Azuki can do...
				throw ex;
			}
		}

		/// <summary>
		/// Creates new instance of System.Drawing.Font with same information.
		/// </summary>
		public static implicit operator Font( FontInfo other )
		{
			return other.ToFont();
		}
		#endregion
	}

	/// <summary>
	/// The singleton class of platform API caller.
	/// </summary>
	public static class Plat
	{
		static IPlatform _Plat = null;

		#region Interface
		/// <summary>
		/// The instance of platform API caller object.
		/// </summary>
		public static IPlatform Inst
		{
			get
			{
				if( _Plat == null )
				{
					if( IsWindows() )
						_Plat = new Sgry.Azuki.WinForms.PlatWin();
					else
						throw new NotSupportedException( "Not supported!" );
				}

				return _Plat;
			}
		}

		internal static bool IsWindows()
		{
			PlatformID platform = Environment.OSVersion.Platform;
			
			if( platform == PlatformID.Win32Windows
				|| platform == PlatformID.Win32NT
				|| platform == PlatformID.WinCE )
			{
				return true;
			}
			
			return false;
		}

		/*internal static bool IsMono()
		{
			Type type = Type.GetType( "Mono.Runtime" );
	        return (type != null);
		}*/
		#endregion
	}

	/// <summary>
	/// Describes information about mouse event.
	/// </summary>
	public interface IMouseEventArgs
	{
		/// <summary>
		/// Gets the index of the mouse button which invoked this event.
		/// </summary>
		int ButtonIndex { get; }

		/// <summary>
		/// Gets the index of the character at where the mouse cursor points when this event occurred.
		/// </summary>
		int Index { get; }

		/// <summary>
		/// Gets the location of the mouse cursor when this event occurred.
		/// </summary>
		Point Location { get; }

		/// <summary>
		/// Gets x-coordinate of the mouse cursor when this event occurred.
		/// </summary>
		int X { get; }

		/// <summary>
		/// Gets y-coordinate of the mouse cursor when this event occurred.
		/// </summary>
		int Y { get; }

		/// <summary>
		/// Gets whether Shift key was pressed down when this event occurred.
		/// </summary>
		bool Shift { get; }

		/// <summary>
		/// Gets whether Control key was pressed down when this event occurred.
		/// </summary>
		bool Control { get; }

		/// <summary>
		/// Gets whether Alt key was pressed down when this event occurred.
		/// </summary>
		bool Alt { get; }

		/// <summary>
		/// Gets whether Special key (Windows key) was pressed down when this event occurred.
		/// </summary>
		bool Special { get; }

		/// <summary>
		/// If set true by an event handler, Azuki does not execute built-in default action.
		/// </summary>
		bool Handled { get; set; }
	}

	/// <summary>
	/// Methods of Anti-Alias to be used for text rendering.
	/// </summary>
	/// <seealso cref="Sgry.Azuki.UserPref.Antialias">UserPref.Antialias property</seealso>
	public enum Antialias
	{
		/// <summary>Uses system default setting.</summary>
		Default,

		/// <summary>Applies no anti-alias process.</summary>
		None,

		/// <summary>Uses single color anti-alias.</summary>
		Gray,

		/// <summary>Uses sub-pixel rendering.</summary>
		Subpixel
	}
}
