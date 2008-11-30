// file: Platform.cs
// brief: Platform API caller.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-06-08
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
		/// Present week notification to user.
		/// (may be auditory or graphically.
		/// the method depends on system setting)
		/// </summary>
		void MessageBeep();
		#endregion

		#region Clipboard
		/// <summary>
		/// Gets content in clipboard.
		/// </summary>
		/// <param name="isLineObj">
		/// whether the content should be treated as
		/// not a chars compositing a line
		/// but a line or not.
		/// </param>
		string GetClipboardText( out bool isLineObj );

		/// <summary>
		/// Sets content of the system clipboard.
		/// </summary>
		/// <param name="text">text to store</param>
		/// <param name="isLineObj">
		/// whether the content should be treated as
		/// not a chars compositing a line
		/// but a line or not.
		/// </param>
		void SetClipboardText( string text, bool isLineObj );
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
