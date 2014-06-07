// file: PlatWin.cs
// brief: Platform API caller for Windows.
//=========================================================
using System;
using System.Drawing;
using System.Text;
using Control = System.Windows.Forms.Control;
using SystemInformation = System.Windows.Forms.SystemInformation;
using Marshal = System.Runtime.InteropServices.Marshal;
using Debug = Sgry.DebugUtl;

namespace Sgry.Azuki.WinForms
{
	/// <summary>
	/// Platform API for Windows.
	/// </summary>
	class PlatWin : IPlatform
	{
		#region Fields
		const string LineSelectClipFormatName = "MSDEVLineSelect";
		const string RectSelectClipFormatName = "MSDEVColumnSelect";
		UInt32 _CF_LINEOBJECT = WinApi.CF_PRIVATEFIRST + 1;
		UInt32 _CF_RECTSELECT = WinApi.CF_PRIVATEFIRST + 2;
		#endregion

		#region Init / Dispose
		public PlatWin()
		{
			_CF_LINEOBJECT = WinApi.RegisterClipboardFormatW( LineSelectClipFormatName );
			_CF_RECTSELECT = WinApi.RegisterClipboardFormatW( RectSelectClipFormatName );
		}
		#endregion

		#region UI Notification
		public void MessageBeep()
		{
			WinApi.MessageBeep( 0 );
		}
		#endregion

		#region Clipboard
		/// <summary>
		/// Gets content of the system clipboard.
		/// </summary>
		/// <param name="dataType">The type of the text data in the clipboard</param>
		/// <returns>Text content retrieved from the clipboard if available. Otherwise null.</returns>
		/// <remarks>
		/// <para>
		/// This method gets text from the system clipboard.
		/// If stored text data is a special format (line or rectangle,)
		/// its data type will be set to <paramref name="dataType"/> parameter.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.TextDataType">TextDataType enum</seealso>
		public string GetClipboardText( out TextDataType dataType )
		{
			Int32 rc; // result code
			bool clipboardOpened = false;
			IntPtr dataHandle = IntPtr.Zero;
			IntPtr dataPtr = IntPtr.Zero;
			uint formatID = UInt32.MaxValue;
			string data = null;

			dataType = TextDataType.Normal;

			try
			{
				// open clipboard
				rc = WinApi.OpenClipboard( IntPtr.Zero );
				if( rc == 0 )
				{
					return null;
				}
				clipboardOpened = true;

				// distinguish type of data in the clipboard
				if( WinApi.IsClipboardFormatAvailable(_CF_LINEOBJECT) != 0 )
				{
					formatID = WinApi.CF_UNICODETEXT;
					dataType = TextDataType.Line;
				}
				else if( WinApi.IsClipboardFormatAvailable(_CF_RECTSELECT) != 0 )
				{
					formatID = WinApi.CF_UNICODETEXT;
					dataType = TextDataType.Rectangle;
				}
				else if( WinApi.IsClipboardFormatAvailable(WinApi.CF_UNICODETEXT) != 0 )
				{
					formatID = WinApi.CF_UNICODETEXT;
				}
				else if( WinApi.IsClipboardFormatAvailable(WinApi.CF_TEXT) != 0 )
				{
					formatID = WinApi.CF_TEXT;
				}
				if( formatID == UInt32.MaxValue )
				{
					return null; // no text data was in clipboard
				}

				// get handle of the clipboard data
				dataHandle = WinApi.GetClipboardData( formatID );
				if( dataHandle == IntPtr.Zero )
				{
					return null;
				}

				// get data pointer by locking the handle
				dataPtr = Utl.MyGlobalLock( dataHandle );
				if( dataPtr == IntPtr.Zero )
				{
					return null;
				}

				// retrieve data
				if( formatID == WinApi.CF_TEXT )
					data = Utl.MyPtrToStringAnsi( dataPtr );
				else
					data = Marshal.PtrToStringUni( dataPtr );
			}
			finally
			{
				// unlock handle
				if( dataPtr != IntPtr.Zero )
				{
					Utl.MyGlobalUnlock( dataHandle );
				}
				if( clipboardOpened )
				{
					WinApi.CloseClipboard();
				}
			}

			return data;
		}

		/// <summary>
		/// Sets content of the system clipboard.
		/// </summary>
		/// <param name="text">Text data to set.</param>
		/// <param name="dataType">Type of the data to set.</param>
		/// <remarks>
		/// <para>
		/// This method set content of the system clipboard.
		/// If <paramref name="dataType"/> is TextDataType.Normal,
		/// the text data will be just a character sequence.
		/// If <paramref name="dataType"/> is TextDataType.Line or TextDataType.Rectangle,
		/// stored text data would be special format that is compatible with Microsoft Visual Studio.
		/// </para>
		/// </remarks>
		public void SetClipboardText( string text, TextDataType dataType )
		{
			Int32 rc; // result code
			IntPtr dataHdl;
			bool clipboardOpened = false;

			try
			{
				// open clipboard
				rc = WinApi.OpenClipboard( IntPtr.Zero );
				if( rc == 0 )
				{
					return;
				}
				clipboardOpened = true;

				// clear clipboard first
				WinApi.EmptyClipboard();

				// set normal text data
				dataHdl = Utl.MyStringToHGlobalUni( text );
				WinApi.SetClipboardData( WinApi.CF_UNICODETEXT, dataHdl );

				// set addional text data
				if( dataType == TextDataType.Line )
				{
					// allocate dummy text (this is needed for PocketPC)
					dataHdl = Utl.MyStringToHGlobalUni( "" );
					WinApi.SetClipboardData( _CF_LINEOBJECT, dataHdl );
				}
				else if( dataType == TextDataType.Rectangle )
				{
					// allocate dummy text (this is needed for PocketPC)
					dataHdl = Utl.MyStringToHGlobalUni( "" );
					WinApi.SetClipboardData( _CF_RECTSELECT, dataHdl );
				}
			}
			finally
			{
				if( clipboardOpened )
				{
					WinApi.CloseClipboard();
				}
			}
		}
		#endregion

		#region UI parameters
		/// <summary>
		/// It will be regarded as a drag operation by the system
		/// if mouse cursor moved beyond this rectangle.
		/// </summary>
		public Size DragSize
		{
			get
			{
				return SystemInformation.DragSize;
			}
		}
		#endregion

		#region Graphic Interface
		/// <summary>
		/// Gets a graphic device context from a window.
		/// </summary>
		public IGraphics GetGraphics( object window )
		{
			AzukiControl azuki = window as AzukiControl;
			if( azuki != null )
			{
				return new GraWin( azuki.Handle, azuki.FontInfo );
			}

			Control control = window as Control;
			if( control != null )
			{
				if( control.Font == null )
					return new GraWin( control.Handle, new FontInfo() );
				else
					return new GraWin( control.Handle, new FontInfo(control.Font) );
			}

			throw new ArgumentException( "an object of unexpected type ("+window.GetType()+") was given to PlatWin.GetGraphics.", "window" );
		}
		#endregion

		#region Utilities
		class Utl
		{
			#region Handle Allocation
			public static IntPtr MyGlobalLock( IntPtr handle )
			{
				return WinApi.GlobalLock( handle );
			}

			public static void MyGlobalUnlock( IntPtr handle )
			{
				WinApi.GlobalUnlock( handle );
			}
			#endregion

			#region String Conversion
			public static string MyPtrToStringAnsi( IntPtr dataPtr )
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

			/// <exception cref="ArgumentOutOfRangeException">Too long text was given.</exception>
			/// <exception cref="OutOfMemoryException">No enough memory.</exception>
			public static IntPtr MyStringToHGlobalUni( string text )
			{
				return Marshal.StringToHGlobalUni( text );
			}
			#endregion
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
		public GraWin( IntPtr hWnd, FontInfo fontInfo )
		{
			_Window = hWnd;
			_DC = WinApi.GetDC( _Window );
			FontInfo = fontInfo;
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
		public FontInfo FontInfo
		{
			set
			{
				Debug.Assert( value != null, "invalid operation; PlatWin.Font must not be set as null." );
				
				// delete old font
				Utl.SafeDeleteObject( _Font );

				// create font handle from Font instance of .NET
				unsafe
				{
					WinApi.LOGFONTW logicalFont;

					WinApi.CreateLogFont( IntPtr.Zero, value, out logicalFont );
					
					// apply anti-alias method that user prefered
					if( UserPref.Antialias == Antialias.None )
						logicalFont.quality = 3; // NONANTIALIASED_QUALITY
					else if( UserPref.Antialias == Antialias.Gray )
						logicalFont.quality = 4; // ANTIALIASED_QUALITY
					else if( UserPref.Antialias == Antialias.Subpixel )
						logicalFont.quality = 5; // CLEARTYPE_QUALITY
					
					_Font = WinApi.CreateFontIndirectW( &logicalFont );
				}
			}
		}

		/// <summary>
		/// Font used for drawing/measuring text.
		/// </summary>
		public Font Font
		{
			set{ FontInfo = new FontInfo(value); }
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

			// (MUST DO AFTER GETTING EXTENTS)
			// extend length if it ends with in a grapheme cluster
			if( 0 < drawableLength && Document.IsNotDividableIndex(text, drawableLength) )
			{
				do
				{
					drawableLength++;
				}
				while( Document.IsNotDividableIndex(text, drawableLength) );
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
