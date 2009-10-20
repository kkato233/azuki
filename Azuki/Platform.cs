// file: Platform.cs
// brief: Platform API caller.
// author: YAMAMOTO Suguru
// update: 2009-10-18
//=========================================================
using System;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using Debug = System.Diagnostics.Debug;

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
		/// <returns>Text content in the clipboard.</returns>
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

		/// <summary>
		/// Gets a graphic device context from a window.
		/// </summary>
		IGraphics GetGraphics( IntPtr window );
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
		#region Properties
		/// <summary>
		/// Font face name of this font.
		/// </summary>
		public string Name;

		/// <summary>
		/// Size of this font in pt (point).
		/// </summary>
		public int Size;

		/// <summary>
		/// Style of this font.
		/// </summary>
		public FontStyle Style;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public FontInfo( string name, int size, FontStyle style )
		{
			Name = name;
			Size = size;
			Style = style;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public FontInfo( Font font )
		{
#			if !PocketPC
			if( font.OriginalFontName != null )
				Name = font.OriginalFontName;
			else
				Name = font.Name;
#			else
			Name = font.Name;
#			endif

			Size = (int)font.Size;
			Style = font.Style;
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Creates new instance of System.Drawing.Font with same information.
		/// </summary>
		public Font ToFont()
		{
			return new Font( Name, Size, Style );
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
						_Plat = new Sgry.Azuki.Windows.PlatWin();
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
}
