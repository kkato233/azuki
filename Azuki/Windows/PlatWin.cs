// file: PlatWin.cs
// brief: Platform API caller for Windows.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-06-08
//=========================================================
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Debug = Sgry.DebugUtl;

namespace Sgry.Azuki.Windows
{
	/// <summary>
	/// Platform API for Windows.
	/// </summary>
	class PlatWin : IPlatform
	{
		#region UI Notification
		public void MessageBeep()
		{
			Utl.MessageBeep( 0 );
		}
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
		public string GetClipboardText( out bool isLineObj )
		{
			return Utl.GetClipboardText( IntPtr.Zero, out isLineObj );
		}

		/// <summary>
		/// Sets content of the system clipboard.
		/// </summary>
		/// <param name="text">text to store</param>
		/// <param name="isLineObj">
		/// whether the content should be treated as
		/// not a chars compositing a line
		/// but a line or not.
		/// </param>
		public void SetClipboardText( string text, bool isLineObj )
		{
			Utl.SetClipboardText( IntPtr.Zero, text, isLineObj );
		}
		#endregion

		/// <summary>
		/// Gets a graphic device context from a window.
		/// </summary>
		public IGraphics GetGraphics( IntPtr window )
		{
			return new GraWin( window );
		}

		#region API Entry Points
		class Utl
		{
			const uint TA_LEFT = 0;
			const uint TA_RIGHT = 2;
			const uint TA_CENTER = 6;
			const uint TA_NOUPDATECP = 0;
			const uint TA_TOP = 0;
			const uint CF_TEXT = 1;
			const uint CF_UNICODETEXT = 13;
			//const uint CF_PRIVATEFIRST = 0x200;
			const uint CF_LINEOBJECT = 0x201;
			//const uint CF_PRIVATELAST = 0x2ff;
			const int PATINVERT = 0x005A0049;

			#region Structs
			[StructLayout(LayoutKind.Sequential)]
			struct SIZE
			{
				public Int32 width, height;
			}

			[StructLayout(LayoutKind.Sequential)]
			struct RECT
			{
				public RECT( Rectangle rect )
				{
					left = rect.Left;
					top = rect.Top;
					right = rect.Right;
					bottom = rect.Bottom;
				}
				public Int32 left, top, right, bottom;
			}
			#endregion

			#region Clipboard
#			if !PocketPC
			[DllImport("user32")]
#			else
			[DllImport("coredll")]
#			endif
			static extern Int32 IsClipboardFormatAvailable( UInt32 format );

#			if !PocketPC
			[DllImport("user32")]
#			else
			[DllImport("coredll")]
#			endif
			static extern Int32 OpenClipboard( IntPtr ownerWnd );

#			if !PocketPC
			[DllImport("user32")]
#			else
			[DllImport("coredll")]
#			endif
			static extern Int32 EmptyClipboard();

#			if !PocketPC
			[DllImport("user32")]
#			else
			[DllImport("coredll")]
#			endif
			static extern Int32 CloseClipboard();

#			if !PocketPC
			[DllImport("user32")]
#			else
			[DllImport("coredll")]
#			endif
			static extern IntPtr GetClipboardData( UInt32 format );
			public static string GetClipboardText( IntPtr ownerWnd, out bool isLineObj )
			{
				Int32 rc; // result code
				IntPtr dataHandle, dataPtr;
				uint type = UInt32.MaxValue;
				string data = null;

				isLineObj = false;

				// distinguish type of data in the clipboard
				if( IsClipboardFormatAvailable(CF_LINEOBJECT) != 0 )
				{
					type = CF_LINEOBJECT;
					isLineObj = true;
				}
				else if( IsClipboardFormatAvailable(CF_UNICODETEXT) != 0 )
				{
					type = CF_UNICODETEXT;
				}
				else if( IsClipboardFormatAvailable(CF_TEXT) != 0 )
				{
					type = CF_TEXT;
				}
				if( type == UInt32.MaxValue )
					return null; // no text data was in clipboard

				// open clipboard
				rc = OpenClipboard( ownerWnd );
				if( rc == 0 )
				{
					return null;
				}

				// get handle of the clipboard data
				dataHandle = GetClipboardData( type );

				// get data pointer by locking the handle
				dataPtr = MyGlobalLock( dataHandle );

				// retrieve data
				if( type == CF_UNICODETEXT || type == CF_LINEOBJECT )
					data = Marshal.PtrToStringUni( dataPtr );
				else if( type == CF_TEXT )
					data = MyPtrToStringAnsi( dataPtr );
				else
					Debug.Fail( "unknown error!" );

				// unlock handle
				MyGlobalUnlock( dataHandle );
				CloseClipboard();

				return data;
			}

#			if !PocketPC
			[DllImport("user32")]
#			else
			[DllImport("coredll")]
#			endif
			static extern IntPtr SetClipboardData( UInt32 format, IntPtr data );
			public static void SetClipboardText( IntPtr ownerWnd, string text, bool isLineObj )
			{
				Int32 rc; // result code
				char[] chars = text.ToCharArray();
				UInt32 format = CF_UNICODETEXT;

				if( isLineObj )
					format = CF_LINEOBJECT;

				// open clipboard
				rc = OpenClipboard( ownerWnd );
				if( rc == 0 )
				{
					return;
				}
				EmptyClipboard();

				// write text
				IntPtr dataHdl = MyStringToHGlobalUni( text );
				SetClipboardData( format, dataHdl );

				// close clipboard
				CloseClipboard();

				return;
			}
			#endregion

			#region Handle Allocation
			static IntPtr MyGlobalLock( IntPtr handle )
			{
#				if !PocketPC
				return GlobalLock( handle );
#				else
				return handle;
#				endif
			}

			static void MyGlobalUnlock( IntPtr handle )
			{
#				if !PocketPC
				GlobalUnlock( handle );
#				else
				// do nothing
#				endif
			}
			#endregion

			#region String Conversion
			static string MyPtrToStringAnsi( IntPtr dataPtr )
			{
				unsafe {
					byte* p = (byte*)dataPtr;
					int byteCount = 0;
					
					// count length
					for( int i=0; *(p + i) != 0; i++ )
					{
						byteCount++;
					}

					// copy data
					byte[] data = new byte[ byteCount ];
					for( int i=0; i<byteCount; i++ )
					{
						data[i] = *(p + i);
					}
					
					return new String( Encoding.Default.GetChars(data) );
				}
			}

			static IntPtr MyStringToHGlobalUni( string text )
			{
#				if !PocketPC
				return Marshal.StringToHGlobalUni( text );
#				else
				unsafe {
					IntPtr handle = Marshal.AllocHGlobal( sizeof(char)*(text.Length + 1) );
					for( int i=0; i<text.Length; i++ )
					{
						Marshal.WriteInt16( handle, i*sizeof(char), (short)text[i] ); // handle[i] = text[i];
					}
					Marshal.WriteInt16( handle, text.Length*sizeof(char), 0 ); // buf[text.Length] = '\0';
					
					return handle;
				}
#				endif
			}
			#endregion

#			if !PocketPC
			[DllImport("kernel32")]
			static extern IntPtr GlobalLock( IntPtr handle );
			[DllImport("kernel32")]
			static extern Int32 GlobalUnlock( IntPtr handle );
#			endif

#			if !PocketPC
			[DllImport("user32")]
#			else
			[DllImport("coredll")]
#			endif
			public static extern int MessageBeep( Int32 type );

			//static extern int SysBeep(30); // for Linux?
		}
		#endregion
	}

	class GraWin : IGraphics
	{
		#region Fields
		IntPtr _Window = IntPtr.Zero;
		IntPtr _DC = IntPtr.Zero;
		IntPtr _MemDC = IntPtr.Zero;
		Size _MemDcSize;
		Point _Offset = Point.Empty;
		IntPtr _MemBmp = IntPtr.Zero;
		IntPtr _OrgMemBmp;
		int _ForeColor;
		IntPtr _Pen = IntPtr.Zero;
		IntPtr _Brush = IntPtr.Zero;
		IntPtr _NullPen = IntPtr.Zero;
		IntPtr _Font = IntPtr.Zero;
		#endregion

		#region Init / Dispose
		public GraWin( IntPtr window )
		{
			_Window = window;
			_DC = WinApi.GetDC( _Window );
		}

		public void Dispose()
		{
			WinApi.SelectObject( _MemDC, _OrgMemBmp );

			// release DC
			WinApi.ReleaseDC( _Window, _DC );
			WinApi.DeleteDC( _MemDC );

			// free objects lastly used
			Utl.SafeDeleteObject( _Pen );
			Utl.SafeDeleteObject( _Brush );
			Utl.SafeDeleteObject( _NullPen );
			Utl.SafeDeleteObject( _Font );
		}
		#endregion

		#region Off-screen Rendering
		/// <summary>
		/// Begin using off-screen buffer and cache drawing which will be done after.
		/// </summary>
		/// <param name="paintRect">painting area (used for creating off-screen buffer).</param>
		public void BeginPaint( Rectangle paintRect )
		{
			Debug.Assert( _MemDC == IntPtr.Zero, "invalid state; _MemDC must be IntPtr.Zero." );
			Debug.Assert( _MemBmp == IntPtr.Zero, "invalid state; _MemBmp must be IntPtr.Zero." );

			// create offscreen from the window DC
			_MemDC = WinApi.CreateCompatibleDC( _DC );
			_MemBmp = WinApi.CreateCompatibleBitmap( _DC, paintRect.Width, paintRect.Height );
			_Offset = paintRect.Location;
			_MemDcSize = paintRect.Size;
			_OrgMemBmp = WinApi.SelectObject( _MemDC, _MemBmp );
		}

		/// <summary>
		/// End using off-screen buffer and flush all drawing results.
		/// </summary>
		public void EndPaint()
		{
			Debug.Assert( _MemDC != IntPtr.Zero, "invalid state; EndPaint was called before BeginPaint." );
			const uint SRCCOPY = 0x00CC0020;

			// flush cached graphic update
			WinApi.BitBlt( _DC, _Offset.X, _Offset.Y, _MemDcSize.Width, _MemDcSize.Height,
				_MemDC, 0, 0, SRCCOPY );
			RemoveClipRect();

			// dispose resources used in off-screen rendering
			WinApi.SelectObject( _MemDC, _OrgMemBmp );
			WinApi.DeleteDC( _MemDC );
			_MemDC = IntPtr.Zero;
			Utl.SafeDeleteObject( _MemBmp );
			_MemBmp = IntPtr.Zero;

			// reset graphic coordinate offset
			_Offset.X = _Offset.Y = 0;
		}
		#endregion

		#region Graphic Setting
		/// <summary>
		/// Font used for drawing/measuring text.
		/// </summary>
		public Font Font
		{
			set
			{
				Debug.Assert( value != null, "invalid operation; PlatWin.Font must not be set as null." );
				
				WinApi.LogFont lf;
				
				// delete old font
				Utl.SafeDeleteObject( _Font );

				// create font handle from Font instance of .NET
				lf = WinApi.CreateLogFont( IntPtr.Zero, value );
				lf.quality = 5; // CLEARTYPE_QUALITY
				_Font = WinApi.CreateFontIndirectW( lf );
			}
		}

		/// <summary>
		/// Foreground color used by drawing APIs.
		/// </summary>
		public Color ForeColor
		{
			set
			{
				Utl.SafeDeleteObject( _Pen );
				_ForeColor = (value.R) | (value.G << 8) | (value.B << 16);
				_Pen = WinApi.CreatePen( 0, 1, _ForeColor );
			}
		}

		/// <summary>
		/// Background color used by drawing APIs.
		/// </summary>
		public Color BackColor
		{
			set
			{
				Utl.SafeDeleteObject( _Brush );
				int colorRef = (value.R) | (value.G << 8) | (value.B << 16);
				_Brush = WinApi.CreateSolidBrush( colorRef );
			}
		}

		/// <summary>
		/// Select specified rectangle as a clipping region.
		/// </summary>
		public void SetClipRect( Rectangle clipRect )
		{
			unsafe
			{
				// make RECT structure
				WinApi.RECT r = new WinApi.RECT();
				r.left = clipRect.X - _Offset.X;
				r.top = clipRect.Y - _Offset.Y;
				r.right = r.left + clipRect.Width;
				r.bottom = r.top + clipRect.Height;

				// create rectangle region and select it as a clipping region
				IntPtr clipRegion = WinApi.CreateRectRgnIndirect( &r );
				WinApi.SelectClipRgn( DC, clipRegion );
				WinApi.DeleteObject( clipRegion ); // SelectClipRgn copies given region thus we can delete this
			}
		}

		/// <summary>
		/// Remove currently selected clipping region from the offscreen buffer.
		/// </summary>
		public void RemoveClipRect()
		{
			WinApi.SelectClipRgn( DC, IntPtr.Zero );
		}
		#endregion

		#region Text Rendering
		/// <summary>
		/// Draws a text.
		/// </summary>
		public void DrawText( string text, ref Point position, Color foreColor )
		{
			const int TRANSPARENT = 1;
			IntPtr oldFont, newFont;
			Int32 oldForeColor;
			int x, y;

			x = position.X - _Offset.X;
			y = position.Y - _Offset.Y;

			newFont = _Font;
			oldFont = WinApi.SelectObject( DC, newFont );
			oldForeColor = WinApi.SetTextColor( DC, foreColor );

			WinApi.SetTextAlign( DC, false );
			WinApi.SetBkMode( DC, TRANSPARENT );
			WinApi.ExtTextOut( DC, x, y, 0, text );
			
			WinApi.SetTextColor( DC, oldForeColor );
			WinApi.SelectObject( DC, oldFont );
		}

		/// <summary>
		/// Measures graphical size of the a text.
		/// </summary>
		/// <param name="text">text to measure</param>
		/// <returns>size of the text in the graphic device context</returns>
		public Size MeasureText( string text )
		{
			int dummy;
			return MeasureText( text, Int32.MaxValue, out dummy );
		}

		/// <summary>
		/// Measures graphical size of the a text within the specified clipping width.
		/// </summary>
		/// <param name="text">text to measure</param>
		/// <param name="clipWidth">width of the clipping area for rendering text (in pixel unit if the context is screen)</param>
		/// <param name="drawableLength">count of characters which could be drawn within the clipping area width</param>
		/// <returns>size of the text in the graphic device context</returns>
		public Size MeasureText( string text, int clipWidth, out int drawableLength )
		{
			IntPtr oldFont;
			Size size;
			int[] extents = new int[text.Length];
			
			oldFont = WinApi.SelectObject( DC, _Font ); // measuring do not need to be done in offscreen buffer.

			// calculate width of given text and graphical distance from left end to where the each char is at
			size = WinApi.GetTextExtent( DC, text, text.Length, clipWidth, out drawableLength, out extents );

			// calculate width of the drawable part
			if( drawableLength == 0 )
			{
				// no chars can be written in clipping area.
				// so width of the drawable part is zero.
				size.Width = 0;
			}
			else
			{
				// there are chars which can be written in clipping area.
				// so get distance of the char at right most; this is the width of the drawable part of the text
				// (note: array of extents will always be filled by GetTextExtentExPoint API in WinCE.)
				if( drawableLength < extents.Length )
				{
					size.Width = extents[ drawableLength - 1 ];
				}
			}

			WinApi.SelectObject( DC, oldFont );

			return size;
		}
		#endregion

		#region Graphic Drawing
		/// <summary>
		/// Draws a line with foreground color.
		/// Note that the point where the line extends to will also be painted.
		/// </summary>
		public void DrawLine( int fromX, int fromY, int toX, int toY )
		{
			IntPtr oldPen;

			fromX -= _Offset.X;
			fromY -= _Offset.Y;
			toX -= _Offset.X;
			toY -= _Offset.Y;

			oldPen = WinApi.SelectObject( DC, _Pen );
			
			WinApi.MoveToEx( DC, fromX, fromY, IntPtr.Zero );
			WinApi.LineTo( DC, toX, toY );
			WinApi.SetPixel( DC, toX, toY, _ForeColor );
			
			WinApi.SelectObject( DC, oldPen );
		}

		/// <summary>
		/// Draws a rectangle with foreground color.
		/// Note that right and bottom edge will also be painted.
		/// </summary>
		public void DrawRectangle( int x, int y, int width, int height )
		{
			IntPtr oldPen;

			x -= _Offset.X;
			y -= _Offset.Y;

			unsafe
			{
				WinApi.POINT[] points = new WinApi.POINT[5];
				points[0] = new WinApi.POINT( x, y );
				points[1] = new WinApi.POINT( x+width, y );
				points[2] = new WinApi.POINT( x+width, y+height );
				points[3] = new WinApi.POINT( x, y+height );
				points[4] = new WinApi.POINT( x, y );

				oldPen = WinApi.SelectObject( DC, _Pen );
				
				fixed( WinApi.POINT* p = points )
				{
					WinApi.Polyline( DC, p, 5 );
				}
				
				WinApi.SelectObject( DC, oldPen );
			}
		}

		/// <summary>
		/// Fills a rectangle with background color.
		/// Note that right and bottom edge will also be painted.
		/// </summary>
		public void FillRectangle( int x, int y, int width, int height )
		{
			IntPtr oldPen, oldBrush;

			x -= _Offset.X;
			y -= _Offset.Y;

			oldPen = WinApi.SelectObject( DC, NullPen );
			oldBrush = WinApi.SelectObject( DC, _Brush );

			WinApi.Rectangle( DC, x, y, x+width+1, y+height+1 );

			WinApi.SelectObject( DC, oldPen );
			WinApi.SelectObject( DC, oldBrush );
		}
		#endregion

		#region Utilities
		IntPtr NullPen
		{
			get
			{
				const int PS_NULL = 5;
				if( _NullPen == IntPtr.Zero )
				{
					_NullPen = WinApi.CreatePen( PS_NULL, 0, 0 );
				}
				return _NullPen;
			}
		}

		IntPtr DC
		{
			get
			{
				if( _MemDC != IntPtr.Zero )
					return _MemDC;
				else
					return _DC;
			}
		}

		class Utl
		{
			public static void SafeDeleteObject( IntPtr gdiObj )
			{
				if( gdiObj != IntPtr.Zero )
				{
					WinApi.DeleteObject( gdiObj );
				}
			}
		}
		#endregion
	}
}
