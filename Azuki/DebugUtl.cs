// file: DebugUtl.cs
// brief: Sgry's utilities for debug
// update: 2009-05-16
//=========================================================
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Assembly = System.Reflection.Assembly;

namespace Sgry
{
	/// <summary>
	/// Exception class for testable assertion.
	/// </summary>
	class AssertException : Exception
	{
		public AssertException()  {}
		public AssertException( string message ) : base(message)  {}
		public AssertException( string message, Exception innerException ) : base(message, innerException)  {}
	}

	/// <summary>
	/// Debug utilities.
	/// </summary>
	static class DebugUtl
	{
		#region Fields and Constants
#		if !PocketPC
		public const string kernel32_dll = "kernel32";
		public const string LogDateHeader = "yyyy-MM-dd hh:mm:ss ";
#		else
		public const string kernel32_dll = "coredll";
		public const string LogDateHeader = "mm.ss ";
#		endif
		static Object LockKey = new Object();
		static string _LogFilePath = null;
		static AutoLogger _AutoLogger = null;
		#endregion

		#region Logging
		public static string LogFilePath
		{
			get
			{
				if( _LogFilePath == null )
				{
					Assembly exe = Assembly.GetExecutingAssembly();
					string exePath = exe.GetModules()[0].FullyQualifiedName;
					string exeDirPath = Path.GetDirectoryName( exePath );
					_LogFilePath = Path.Combine( exeDirPath, "log.txt" );
				}
				return _LogFilePath;
			}
		}

		/// <summary>
		/// Writes message to a log file with date and time.
		/// </summary>
		public static void LogOut( string format, params object[] p )
		{
			try
			{
				lock( LockKey )
				{
					DateTime now = DateTime.Now;

					using( StreamWriter file = new StreamWriter(LogFilePath, true) )
					{
						file.Write( now.ToString(LogDateHeader) );
						file.WriteLine( String.Format(format, p) );
					}
				}
			}
			catch{}
		}

		/// <summary>
		/// Writs message to a log file with precise time and process/thread ID.
		/// </summary>
		public static void LogOutEx( string format, params object[] p )
		{
			try
			{
				lock( LockKey )
				{
					int pid;
					int tid = 0;
					DateTime now = DateTime.Now;
					
					pid = Process.GetCurrentProcess().Id;
					tid = Thread.CurrentThread.ManagedThreadId;

					using( StreamWriter file = new StreamWriter(LogFilePath, true) )
					{
						file.Write(
								now.ToString(LogDateHeader)
								+ String.Format("[{0},{1}] ", pid.ToString("X4"), tid.ToString("X2"))
							);
						file.WriteLine( String.Format(format, p) );
					}
				}
			}
			catch{}
		}

		/// <summary>
		/// Writs only message to a log file.
		/// </summary>
		public static void LogOut_Raw( string format, params object[] p )
		{
			try
			{
				lock( LockKey )
				{
					using( StreamWriter file = new StreamWriter(LogFilePath, true) )
					{
						file.WriteLine( String.Format(format, p) );
					}
				}
			}
			catch{}
		}

		/// <summary>
		/// Log writer object that actually write just before the application ends.
		/// </summary>
		public static AutoLogger AutoLogger
		{
			get
			{
				if( _AutoLogger == null )
				{
					_AutoLogger = new AutoLogger();
				}

				return _AutoLogger;
			}
		}
		#endregion

		#region Diagnostics
		[DllImport(kernel32_dll)]
		public static extern void Sleep( int millisecs );

		/// <summary>
		/// Gets system performance counter value in millisecond.
		/// </summary>
		public static double GetCounterMsec()
		{
			long count;
			long freq;
			QueryPerformanceCounter( out count );
			QueryPerformanceFrequency( out freq );
			return count / (double)freq * 1000;
		}

		[DllImport(kernel32_dll)]
		static extern Int32 QueryPerformanceCounter( out Int64 count );

		[DllImport(kernel32_dll)]
		static extern Int32 QueryPerformanceFrequency( out Int64 count );
		#endregion

		#region Testable Assertion
		[Conditional("DEBUG")]
		public static void Fail( string message )
		{
			throw new AssertException( message );
		}

		[Conditional("DEBUG")]
		public static void Assert( bool condition )
		{
			if( !condition )
				throw new AssertException();
		}

		[Conditional("DEBUG")]
		public static void Assert( bool condition, string message )
		{
			if( !condition )
				throw new AssertException( message );
		}
		#endregion
	}

	class AutoLogger
	{
		#region Fields
		StringBuilder _Buf = new StringBuilder();
		#endregion

		#region Init / Dispose
		~AutoLogger()
		{
			using( StreamWriter file = new StreamWriter(DebugUtl.LogFilePath, true) )
			{
				file.Write( _Buf.ToString() );
				file.WriteLine();
			}
		}
		#endregion

		#region Write
		/// <summary>
		/// Writes message to a log file with date and time.
		/// </summary>
		public void WriteLine( string format, params object[] p )
		{
			Console.Error.WriteLine( format, p );
			try
			{
				DateTime now = DateTime.Now;

				_Buf.Append( now.ToString(DebugUtl.LogDateHeader) );
				_Buf.Append( String.Format(format, p) );
				_Buf.Append( Console.Out.NewLine );
			}
			catch{}
		}
		#endregion
	}

	#region Minimal Testing Framework
#	if DEBUG && !PocketPC
	/// <summary>
	/// Test utility.
	/// </summary>
	class TestUtl
	{
		public static int ErrorCount = 0;

		public static bool ErrorOccured
		{
			get{ return (0 < ErrorCount); }
		}

		public static void Fail( string message )
		{
			throw new AssertException( message );
		}

		public static void AssertEquals( object expected, object actual )
		{
			if( expected == null )
			{
				if( actual != null )
					throw new AssertException( "Objects were not equal.\nExpected: null\nActual: "+actual );
			}
			else if( !expected.Equals(actual) )
			{
				throw new AssertException( "Objects were not equal.\nExpected: "+expected+"\nActual:   "+actual );
			}
		}

		public static void AssertType<T>( object obj )
		{
			if( obj.GetType() != typeof(T) )
				throw new AssertException( "object type is not "+typeof(T).Name+" but "+obj.GetType()+"." );
		}

		public static void AssertExceptionType( Exception ex, Type type )
		{
			if( ex.GetType() == type )
				return;

			ConsoleColor orgColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			WriteLineWithChar( Console.Error, '=' );

			// print error message
			Console.Error.WriteLine( "Wrong type of exception was thrown (not "+type.Name+" but "+ex.GetType()+")" );
			
			// print exception info
			WriteLineWithChar( Console.Error, '-' );
			Console.Error.WriteLine( ex );

			WriteLineWithChar( Console.Error, '=' );
			Console.Error.WriteLine();
			Console.ForegroundColor = orgColor;
		}

		public static void Do( System.Threading.ThreadStart testProc )
		{
			try
			{
				testProc();
			}
			catch( Exception ex )
			{
				string stackTrace;

				ConsoleColor orgColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				WriteLineWithChar( Console.Error, '=' );

				// print error message
				Console.Error.WriteLine( ex.Message );

				// trim last line of the stack trace because it always be this method.
				stackTrace = ex.StackTrace;
				try
				{
					int firstLineEnd = Utl.NextLineHead( stackTrace, 0 );
					int lastLineHead = Utl.PrevLineHead( stackTrace, stackTrace.Length );
					stackTrace = stackTrace.Substring( firstLineEnd, lastLineHead-firstLineEnd-1 );
				}
				catch
				{}

				// print stack trace
				WriteLineWithChar( Console.Error, '-' );
				Console.Error.WriteLine( stackTrace );

				WriteLineWithChar( Console.Error, '=' );
				Console.Error.WriteLine();
				Console.ForegroundColor = orgColor;
				ErrorCount++;
			}
		}
		
		#region Utilities
		static void WriteLineWithChar( TextWriter stream, char ch )
		{
			for( int i=0; i<Console.BufferWidth-1; i++ )
			{
				stream.Write( ch );
			}
			stream.WriteLine();
		}

		class Utl
		{
			public static int NextLineHead( string str, int searchFromIndex )
			{
				for( int i=searchFromIndex; i<str.Length; i++ )
				{
					// found EOL code?
					if( str[i] == '\r' )
					{
						if( i+1 < str.Length
							&& str[i+1] == '\n' )
						{
							return i+2;
						}

						return i+1;
					}
					else if( str[i] == '\n' )
					{
						return i+1;
					}
				}

				return -1; // not found
			}

			public static int PrevLineHead( string str, int searchFromIndex )
			{
				Debug.Assert( searchFromIndex <= str.Length, "invalid argument; searchFromIndex is too large ("+searchFromIndex+" but str.Length is "+str.Length+")" );

				if( str.Length <= searchFromIndex )
				{
					searchFromIndex = str.Length - 1;
				}

				for( int i=searchFromIndex-1; 0<=i; i-- )
				{
					// found EOL code?
					if( str[i] == '\n' )
					{
						return i+1;
					}
					else if( str[i] == '\r' )
					{
						if( i+1 < str.Length
							&& str[i+1] == '\n' )
						{
							continue;
						}
						return i+1;
					}
				}

				return 0;
			}
		}
		#endregion
	}
#	endif
	#endregion
}
