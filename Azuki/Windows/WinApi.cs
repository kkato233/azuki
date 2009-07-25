// file: WinApi.cs
// brief: Sgry's Win32API glues.
// author: YAMAMOTO Suguru
// update: 2009-07-25
//=========================================================
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.Windows
{
	/// <summary>
	/// Win32API wrapper for modules which is used only in the Windows environment.
	/// </summary>
	static class WinApi
	{
		#region DLL Names
#		if !PocketPC
		const string kernel32_dll = "kernel32";
		const string user32_dll = "user32";
		const string gdi32_dll = "gdi32";
		const string imm32_dll = "imm32";
#		else
		const string kernel32_dll = "coredll";
		const string user32_dll = "coredll";
		const string gdi32_dll = "coredll";
		const string imm32_dll = "coredll";
#		endif
		#endregion

		#region Constants
		public const int WM_PAINT = 0x000F;
		public const int WM_HSCROLL = 0x0114;
		public const int WM_VSCROLL = 0x0115;
		
		public const int WM_KEYFIRST = 0x0100;
		public const int WM_KEYDOWN = 0x0100;
		public const int WM_KEYUP = 0x0101;
		public const int WM_KEYCHAR = 0x0102;
		public const int WM_KEYUNICHAR = 0x0109;
		public const int WM_KEYLAST = 0x0109;

		public const int WM_MOUSEMOVE = 0x0200;
		public const int WM_LBUTTONDOWN = 0x0201;
		public const int WM_LBUTTONUP = 0x0202;
		public const int WM_LBUTTONDBLCLK = 0x0203;
		public const int WM_RBUTTONDOWN = 0x0204;
		public const int WM_RBUTTONUP = 0x0205;
		public const int WM_RBUTTONDBLCLK = 0x0206;
		public const int WM_MOUSEWHEEL = 0x020a;

		public const int WM_IME_STARTCOMPOSITION =  0x010D;
		public const int WM_IME_ENDCOMPOSITION = 0x010E;
		public const int WM_IME_NOTIFY = 0x0282;
		public const int WM_IME_REQUEST = 0x0288;
		public const int IMR_RECONVERTSTRING = 0x0004;
		public const int SCS_QUERYRECONVERTSTRING = 0x00020000;
		public const int SCS_SETRECONVERTSTRING = 0x00010000;
		public const int SCS_CAP_SETRECONVERTSTRING = 0x00000004;
		public const int IME_PROP_UNICODE = 0x00080000;
		public const int IGP_PROPERTY = 0x00000004;
		public const int IGP_SETCOMPSTR = 0x00000014;

		public const long WS_HSCROLL = 0x00100000L;
		public const long WS_BORDER = 0x00800000L;
		public const long WS_EX_CLIENTEDGE = 0x00000200L;
		public const int GWL_STYLE = -16;
		public const int GWL_EXSTYLE = -20;
		public const int SWP_FRAMECHANGED = 0x0020;

		public const int SB_LINEUP = 0;
		public const int SB_LINEDOWN = 1;
		public const int SB_PAGEUP = 2;
		public const int SB_PAGEDOWN = 3;
		public const int SB_THUMBPOSITION = 4;
		public const int SB_THUMBTRACK = 5;
		public const int SB_TOP = 6;
		public const int SB_BOTTOM = 7;
		public const int SB_ENDSCROLL = 8;

		public const UInt32 CF_TEXT = 1;
		public const UInt32 CF_UNICODETEXT = 13;
		public const UInt32 CF_PRIVATEFIRST = 0x200;
		public const UInt32 CF_LINEOBJECT = 0x201;
		public const UInt32 CF_PRIVATELAST = 0x2ff;

		const int SIF_RANGE = 0x01;
		const int SIF_PAGE  = 0x02;
		const int SIF_POS   = 0x04;
		const int SIF_DISABLENOSCROLL = 0x08;
		const int SIF_TRACKPOS = 0x10;
		const int SW_INVALIDATE = 0x0002;

		const uint TA_LEFT = 0;
		const uint TA_RIGHT = 2;
		const uint TA_CENTER = 6;
		const uint TA_NOUPDATECP = 0;
		const uint TA_TOP = 0;
		const int PATINVERT = 0x005A0049;
		#endregion

		#region Types
		public delegate IntPtr WNDPROC( IntPtr window, Int32 message, IntPtr wParam, IntPtr lParam );

		[StructLayout(LayoutKind.Sequential)]
		struct SIZE
		{
			public Int32 width, height;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public POINT( int x, int y ){ this.x=x; this.y=y; }
			public POINT( Point pt ){ x=pt.X; y=pt.Y; }
			public Int32 x, y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
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

		[StructLayout(LayoutKind.Sequential)]
		public struct PAINTSTRUCT
		{
			public IntPtr hDC;
			public Int32 bErase;
			public RECT paint;
			public Int32 bRestore;
			public Int32 bIncUpdate;
			public Int64 reserved0, reserved1, reserved2, reserved3, reserved4;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SCROLLINFO
		{
			public UInt32 size;
			public UInt32 mask;
			public Int32 min;
			public Int32 max;
			public UInt32 page;
			public Int32 pos;
			public Int32 trackPos;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct COMPOSITIONFORM
		{
			public UInt32 style;
			public POINT currentPos;
			public RECT area;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
		public struct TEXTMETRIC
		{
			public Int32 height;
			public Int32 ascent;
			public Int32 descent;
			public Int32 internalLeading;
			public Int32 externalLeading;
			public Int32 aveCharWidth;
			public Int32 maxCharWidth;
			public Int32 weight;
			public Int32 overhang;
			public Int32 digitizedAspectX;
			public Int32 digitizedAspectY;
			public char firstChar;
			public char lastChar;
			public char defaultChar;
			public char breakChar;
			public byte italic;
			public byte underlined;
			public byte struckOut;
			public byte pitchAndFamily;
			public byte charSet;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
		public class LogFont
		{
			public int height;
			public int width;
			public int escapement;
			public int orientation;
			public int weight;
			public byte italic;
			public byte underline;
			public byte strikeOut;
			public byte charSet;
			public byte outPrecision;
			public byte clipPrecision;
			public byte quality;
			public byte pitchAndFamily;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)]
			public string faceName;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
		public struct RECONVERTSTRING
		{
			/// <summary>Size of this instance.</summary>
			public UInt32 dwSize;

			/// <summary>Version (must be 0).</summary>
			public UInt32 dwVersion;

			/// <summary>Length of the string given to IME.</summary>
			public UInt32 dwStrLen;

			/// <summary>
			/// Byte-offset of the string given to IME
			/// from the memory address of this structure.
			/// </summary>
			public UInt32 dwStrOffset;

			/// <summary>Length of the string that will be able to be reconverted.</summary>
			public UInt32 dwCompStrLen;

			/// <summary>
			/// Byte-offset of the string that will be able to be reconverted
			/// from the start position of where specified with dwStrOffset.
			/// </summary>
			public UInt32 dwCompStrOffset;

			/// <summary>Length of the exact string that will be reconverted.</summary>
			public UInt32 dwTargetStrLen;

			/// <summary>
			/// Byte-offset of the exact string that will be reconverted
			/// from the start position of where specified with dwStrOffset.
			/// </summary>
			public UInt32 dwTargetStrOffset;
		}
		#endregion

		#region Caret
		public static void CreateCaret( IntPtr window, Size size )
		{
			CreateCaret_( window, IntPtr.Zero, size.Width, size.Height );
		}

		public static Point GetCaretPos()
		{
			unsafe {
				POINT point;
				Int32 rc = GetCaretPos( &point );
				if( rc == 0 )
				{
					throw new Exception( "failed to get caret location" );
				}

				return new Point( point.x, point.y );
			}
		}

		public static bool SetCaretPos( int x, int y )
		{
			Int32 rc = SetCaretPos_( x, y );
			return (rc != 0);
		}

		[DllImport(user32_dll)]
		static unsafe extern Int32 GetCaretPos( POINT* outPos );
		
		[DllImport(user32_dll, EntryPoint="SetCaretPos")]
		static extern Int32 SetCaretPos_( Int32 x, Int32 y );
			
		[DllImport(user32_dll, EntryPoint="CreateCaret")]
		static extern Int32 CreateCaret_( IntPtr window, IntPtr hBitmap, Int32 width, Int32 height );

		[DllImport(user32_dll)]
		public static extern Int32 DestroyCaret();
			
		[DllImport(user32_dll)]
		public static extern Int32 ShowCaret( IntPtr window );

		[DllImport(user32_dll)]
		public static extern Int32 HideCaret( IntPtr window );
		#endregion

		#region Core
		[DllImport(kernel32_dll)]
		public static extern UInt32 GetLastError();

		[DllImport(kernel32_dll)]
		public static extern IntPtr GlobalLock( IntPtr handle );

		[DllImport(kernel32_dll)]
		public static extern Int32 GlobalUnlock( IntPtr handle );

		[DllImport(user32_dll)]
		public static extern int MessageBeep( Int32 type );
		#endregion

		#region Clipboard
		[DllImport(user32_dll)]
		public static extern UInt32 RegisterClipboardFormatW( string format );

		[DllImport(user32_dll)]
		public static extern Int32 IsClipboardFormatAvailable( UInt32 format );

		[DllImport(user32_dll)]
		public static extern Int32 OpenClipboard( IntPtr ownerWnd );

		[DllImport(user32_dll)]
		public static extern Int32 EmptyClipboard();

		[DllImport(user32_dll)]
		public static extern Int32 CloseClipboard();

		[DllImport(user32_dll)]
		public static extern IntPtr GetClipboardData( UInt32 format );

		[DllImport(user32_dll)]
		public static extern IntPtr SetClipboardData( UInt32 format, IntPtr data );
		#endregion

		#region GDI - Device Context Manipulation
		[DllImport(gdi32_dll)]
		public static extern Int32 GetDeviceCaps( IntPtr dc, Int32 index );

		[DllImport(user32_dll)]
		public static extern IntPtr GetDC( IntPtr hWnd );

		[DllImport(user32_dll)]
		public static extern Int32 ReleaseDC( IntPtr hWnd, IntPtr dc );

		[DllImport(user32_dll)]
		public static unsafe extern IntPtr BeginPaint( IntPtr hWnd, PAINTSTRUCT* ps );

		[DllImport(user32_dll)]
		public static extern unsafe Int32 EndPaint( IntPtr hWnd, PAINTSTRUCT* ps );

		[DllImport(gdi32_dll)]
		public static extern IntPtr CreateCompatibleDC( IntPtr hdc );

		[DllImport(gdi32_dll)]
		public static extern IntPtr CreateCompatibleBitmap( IntPtr hdc, Int32 width, Int32 height );

		[DllImport(gdi32_dll)]
		public static unsafe extern Int32 DeleteDC( IntPtr hdc );

		[DllImport(gdi32_dll)]
		public static extern Int32 BitBlt(
			IntPtr destination, Int32 destX, Int32 destY, Int32 width, Int32 height,
			IntPtr source, Int32 srcX, Int32 srcY, UInt32 rasterOpCode );

		[DllImport(gdi32_dll)]
		public static extern Int32 SelectClipRgn( IntPtr hdc, IntPtr regionHdl );

		[DllImport(gdi32_dll)]
		public unsafe static extern IntPtr CreateRectRgnIndirect( RECT * rect );

		[DllImport(gdi32_dll)]
		public unsafe static extern IntPtr SelectObject( IntPtr hdc, IntPtr gdiObj );
		
		[DllImport(gdi32_dll)]
		public unsafe static extern Int32 DeleteObject( IntPtr gdiObj );
		#endregion

		#region GDI - Text and Fonts
		[DllImport(gdi32_dll, CharSet=CharSet.Unicode)]
		unsafe static extern bool ExtTextOutW( IntPtr hdc, Int32 x, Int32 y, UInt32 formatOptions, RECT* bounds, string text, UInt32 textLength, IntPtr zero );
		public static bool ExtTextOut( IntPtr hdc, int x, int y, int formatOptions, string text )
		{
			unsafe {
				return ExtTextOutW( hdc, x, y, (uint)formatOptions, null, text, (UInt32)text.Length, IntPtr.Zero );
			}
		}
		public static bool ExtTextOut( IntPtr hdc, int x, int y, Rectangle bounds, int formatOptions, string text )
		{
			unsafe {
				RECT rect = new RECT( bounds );
				return ExtTextOutW( hdc, x, y, (uint)formatOptions, &rect, text, (UInt32)text.Length, IntPtr.Zero );
			}
		}

		[DllImport(gdi32_dll, CharSet=CharSet.Unicode)]
		unsafe static extern Int32 GetTextExtentExPointW( IntPtr hdc, string text, int textLen, int maxWidth, int* out_fitLength, int* out_x, SIZE* out_size );
		public static Size GetTextExtent( IntPtr hdc, string text, int textLen, int maxWidth, out int fitLength, out int[] extents )
		{
			Int32 bOk;
			SIZE size;
			extents = new int[text.Length];

			unsafe {
				fixed( int* pExtents = extents )
				fixed( int* pFitLength = &fitLength )
					bOk = GetTextExtentExPointW( hdc, text, textLen, maxWidth, pFitLength, pExtents, &size );
				Debug.Assert( bOk != 0, "failed to calculate text width" );
				return new Size( size.width, size.height );
			}
		}

		public static Size GetTextExtent( IntPtr hdc, string text, int textLen )
		{
			SIZE size;
			
			unsafe {
				GetTextExtentExPointW( hdc, text, textLen, 0, null, null, &size );
				return new Size( size.width, size.height );
			}
		}

		[DllImport(gdi32_dll)]
		static extern UInt32 SetTextAlign( IntPtr hdc, UInt32 mode );
		public static void SetTextAlign( IntPtr hdc, bool alignRight )
		{
			uint flag = TA_TOP | TA_NOUPDATECP;
			flag |= alignRight ? TA_RIGHT : TA_LEFT;
			uint rc = SetTextAlign( hdc, flag );
			Debug.Assert( rc != UInt32.MaxValue, "failed to set text alignment by SetTextAlign." );
		}

		public static unsafe LogFont CreateLogFont( IntPtr window, Font font )
		{
			const int LOGPIXELSY = 90;
			LogFont lf = new LogFont();
			IntPtr dc;
			int dpi_y;

			dc = GetDC( window );
			{
				dpi_y = GetDeviceCaps( dc, LOGPIXELSY );
			}
			ReleaseDC( window, dc );
			lf.height = -(int)( font.Size * dpi_y / 72 );
			lf.weight = (font.Style & FontStyle.Bold) != 0 ? 700 : 400; // FW_BOLD or FW_NORMAL
			lf.italic = (byte)( (font.Style & FontStyle.Italic) != 0 ? 1 : 0 );
//			lf.quality = 5; // CLEARTYPE_QUALITY

			// set font name
			lf.faceName = font.Name;

			return lf;
		}

		[DllImport(gdi32_dll)]
		public unsafe static extern IntPtr CreateFontIndirectW( [In, MarshalAs(UnmanagedType.LPStruct)] LogFont lf );

		[DllImport(gdi32_dll)]
		public static extern Int32 SetTextColor( IntPtr hdc, Int32 color );
		public static Int32 SetTextColor( IntPtr hdc, Color color )
		{
			int bgr = (color.B << 16) | (color.G << 8) | color.R;
			return SetTextColor( hdc, bgr );
		}

		[DllImport(gdi32_dll)]
		public static extern Int32 SetBkColor( IntPtr hdc, Int32 color );
		public static Int32 SetBkColor( IntPtr hdc, Color color )
		{
			int bgr = (color.B << 16) | (color.G << 8) | color.R;
			return SetBkColor( hdc, bgr );
		}

		[DllImport(gdi32_dll)]
		public static extern Int32 SetBkMode( IntPtr hdc, Int32 mode );
		#endregion

		#region GDI - Drawing Other Graphics
		[DllImport(gdi32_dll)]
		public unsafe static extern UInt32 MoveToEx( IntPtr hdc, Int32 x, Int32 y, IntPtr nul );

		[DllImport(gdi32_dll)]
		public unsafe static extern Int32 LineTo( IntPtr hdc, Int32 x, Int32 y );
		
		[DllImport(gdi32_dll)]
		public unsafe static extern Int32 SetPixel( IntPtr hdc, Int32 x, Int32 y, Int32 color );
		
		[DllImport(gdi32_dll)]
		public unsafe static extern Int32 Polyline( IntPtr hdc, POINT* points, Int32 count );
		
		[DllImport(gdi32_dll)]
		public unsafe static extern Int32 Rectangle( IntPtr hdc, Int32 left, Int32 top, Int32 right, Int32 bottom );

		[DllImport(gdi32_dll)]
		public unsafe static extern IntPtr CreatePen( Int32 style, Int32 width, Int32 color );

		[DllImport(gdi32_dll)]
		public unsafe static extern IntPtr CreateSolidBrush( Int32 color );
		#endregion

		#region Window Scrolling
		[DllImport(user32_dll)]
		static unsafe extern int SetScrollInfo( IntPtr window, int barType, SCROLLINFO* si, Int32 bRedraw );
		public static void SetScrollPos( IntPtr window, bool isHScroll, int pos )
		{
			unsafe {
				SCROLLINFO si;
				si.size = (uint)sizeof( SCROLLINFO );
				si.mask = SIF_POS;
				si.pos = pos;
				SetScrollInfo( window, isHScroll?0:1, &si, 1 );
			}
		}

		public static void SetScrollRange( IntPtr window, bool isHScroll, int min, int max, int pageSize )
		{
			Debug.Assert( 0 <= pageSize );
			unsafe {
				SCROLLINFO si;
				si.size = (uint)sizeof( SCROLLINFO );
				si.mask = SIF_RANGE|SIF_PAGE|SIF_DISABLENOSCROLL;
				si.min = min;
				si.max = max;
				si.page = (uint)pageSize;
				SetScrollInfo( window, isHScroll?0:1, &si, 1 );
			}
		}

		[DllImport(user32_dll)]
		static unsafe extern Int32 GetScrollInfo( IntPtr window, Int32 bar, SCROLLINFO * si );
		public static int GetScrollPos( IntPtr window, bool isHScroll )
		{
			unsafe {
				SCROLLINFO si;
				int rc;

				si.size = (uint)sizeof( SCROLLINFO );
				si.mask = SIF_POS;
				rc = GetScrollInfo( window, isHScroll?0:1, &si );
				Debug.Assert( rc != 0, "failed to call GetScrollInfo" );
				return si.pos;
			}
		}

		public static void GetScrollRange( IntPtr window, bool isHScroll, out int min, out int max )
		{
			unsafe {
				SCROLLINFO si;
				si.size = (uint)sizeof( SCROLLINFO );
				si.mask = SIF_RANGE;
				GetScrollInfo( window, isHScroll?0:1, &si );
				min = si.min;
				max = si.max;
			}
		}

		public static int GetScrollTrackPos( IntPtr window, bool isHScroll )
		{
			unsafe {
				SCROLLINFO si;
				int rc;

				si.size = (uint)sizeof( SCROLLINFO );
				si.mask = SIF_TRACKPOS;
				rc = GetScrollInfo( window, isHScroll?0:1, &si );
				Debug.Assert( rc != 0, "failed to call GetScrollInfo" );
				return si.trackPos;
			}
		}

		[DllImport(user32_dll)]
		static unsafe extern Int32 ScrollWindowEx( IntPtr window, int x, int y, RECT* scroll, RECT* clip, IntPtr updateRegion, RECT* update, UInt32 flags );
		public static void ScrollWindow( IntPtr window, int x, int y )
		{
			unsafe {
				ScrollWindowEx( window, x, y, null, null, IntPtr.Zero, null, SW_INVALIDATE );
				//ScrollWindowEx( window, x, y, null, null, IntPtr.Zero, null, 0 );
			}
		}
		public static void ScrollWindow( IntPtr window, int x, int y, Rectangle clipRect )
		{
			unsafe {
				RECT clip = new RECT( clipRect );
				ScrollWindowEx( window, x, y, &clip, &clip, IntPtr.Zero, null, SW_INVALIDATE );
				//ScrollWindowEx( window, x, y, &clip, &clip, IntPtr.Zero, null, 0 );
			}
		}
		#endregion

		#region Keyboard
		public static Keys GetCurrentModifierKeyStates()
		{
			Keys modifiers = Keys.None;

			modifiers |= IsKeyDown(Keys.Menu) ? Keys.Alt : Keys.None;
			modifiers |= IsKeyDown(Keys.ControlKey) ? Keys.Control : Keys.None;
			modifiers |= IsKeyDown(Keys.ShiftKey) ? Keys.Shift : Keys.None;

			return modifiers;
		}

		[DllImport(user32_dll)]
		static extern Int16 GetKeyState( Int32 vKeyCode );
		public static bool IsKeyDown( Keys keyCode )
		{
			return GetKeyState((Int32)keyCode) < 0;
		}

		[DllImport(user32_dll)]
		static extern Int16 GetAsyncKeyState( Int32 vKeyCode );
		public static bool IsKeyDownAsync( Keys keyCode )
		{
			return GetAsyncKeyState((Int32)keyCode) < 0;
		}
		#endregion

		#region IME (Input Method Editor)
		/// <summary>Sets location of the IME composition window (pre-edit window) </summary>
		public static void SetImeWindowPos( IntPtr window, Point screenPos )
		{
			IntPtr imContext = ImmGetContext( window );
			unsafe
			{
				SetImeWindowPos( imContext, window, screenPos );
			}
			ImmReleaseContext( window, imContext );
		}

		/// <summary>Sets location of the IME composition window (pre-edit window) </summary>
		public static void SetImeWindowPos( IntPtr imContext, IntPtr window, Point screenPos )
		{
			const int CFS_POINT = 0x0002;
			COMPOSITIONFORM compForm = new COMPOSITIONFORM();

			unsafe
			{
				compForm.style = CFS_POINT;
				compForm.currentPos = new POINT( screenPos );
				compForm.area = new RECT();

				ImmSetCompositionWindow( imContext, &compForm );
			}
		}

		/// <summary>Sets font of the IME composition window (pre-edit window) </summary>
		public static void SetImeWindowFont( IntPtr window, Font font )
		{
			IntPtr imContext;
			LogFont logFont;

			imContext = ImmGetContext( window );
			unsafe
			{
				logFont = CreateLogFont( window, font );
				
				ImmSetCompositionFontW( imContext, logFont );
			}
			ImmReleaseContext( window, imContext );
		}

		[DllImport(imm32_dll)]
		public static extern IntPtr ImmGetContext( IntPtr hWnd );

		[DllImport(imm32_dll)]
		public static extern Int32 ImmReleaseContext( IntPtr hWnd, IntPtr context );

		[DllImport(imm32_dll)]
		public static unsafe extern Int32 ImmSetCompositionStringW( IntPtr imContext, UInt32 index, void* lpComp, UInt32 dwCompLen, void* lpRead, UInt32 readLen );

		[DllImport(imm32_dll)]
		static unsafe extern Int32 ImmSetCompositionWindow( IntPtr imContext, COMPOSITIONFORM* compForm );

		[DllImport(imm32_dll)]
		static unsafe extern Int32 ImmSetCompositionFontW( IntPtr imContext,  [In, MarshalAs(UnmanagedType.LPStruct)] LogFont logFont );

		[DllImport(imm32_dll)]
		public static extern UInt32 ImmGetProperty( IntPtr inputLocale, UInt32 index );

		[DllImport(user32_dll)]
		public static extern IntPtr GetKeyboardLayout( UInt32 threadID );
		#endregion

		#region Window Position
		[DllImport(user32_dll)]
		public static extern Int32 SetWindowPos( IntPtr window, IntPtr insertAfter, Int32 x, Int32 y, Int32 width, Int32 height, Int32 flags );
		#endregion

		#region GetWindowLong
		[DllImport(user32_dll)]
		static extern IntPtr GetWindowLongPtrW( IntPtr hWnd, Int32 code );

		[DllImport(user32_dll)]
		static extern IntPtr GetWindowLongW( IntPtr hWnd, Int32 code );
		
		public static IntPtr GetWindowLong( IntPtr hWnd, Int32 code )
		{
			if( Marshal.SizeOf(IntPtr.Zero) == 4 )
				return GetWindowLongW( hWnd, code );
			else
				return GetWindowLongPtrW( hWnd, code );
		}

		[DllImport(user32_dll)]
		static extern Int32 SetWindowLongPtrW( IntPtr hWnd, Int32 code, WNDPROC newLong );
		
		[DllImport(user32_dll)]
		static extern Int32 SetWindowLongW( IntPtr hWnd, Int32 code, WNDPROC newLong );
		
		public static Int32 SetWindowLong( IntPtr hWnd, Int32 code, WNDPROC newLong )
		{
			if( Marshal.SizeOf(IntPtr.Zero) == 4 )
				return SetWindowLongW( hWnd, code, newLong );
			else
				return SetWindowLongPtrW( hWnd, code, newLong );
		}

		[DllImport(user32_dll)]
		static extern IntPtr SetWindowLongPtrW( IntPtr hWnd, Int32 code, IntPtr newLong );

		[DllImport(user32_dll)]
		static extern IntPtr SetWindowLongW( IntPtr hWnd, Int32 code, IntPtr newLong );
		
		public static IntPtr SetWindowLong( IntPtr hWnd, Int32 code, IntPtr newLong )
		{
			if( Marshal.SizeOf(IntPtr.Zero) == 4 )
				return SetWindowLongW( hWnd, code, newLong );
			else
				return SetWindowLongPtrW( hWnd, code, newLong );
		}
		
		[DllImport(user32_dll)]
		public static extern IntPtr CallWindowProc( IntPtr wndProc, IntPtr window, Int32 message, IntPtr wParam, IntPtr lParam );
		#endregion
	}
}
