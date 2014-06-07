// file: DebugUtl.cs
// brief: Sgry's utilities for debug
//=========================================================
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
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
		public const string kernel32_dll = "kernel32";
		static AutoLogger _AutoLogger = null;
		#endregion

		#region Logging
		/// <summary>
		/// Log writer object that actually write just before the application ends.
		/// </summary>
		public static AutoLogger Log
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
		/// <summary>
		/// Stops current thread for specified time.
		/// </summary>
		public static void Sleep( int millisecs )
		{
			Thread.CurrentThread.Join( millisecs );
		}

		public static string GetStackTrace()
		{
			const int TraceBackCount = 5;
			StringBuilder buf = new StringBuilder( 256 );
			StringBuilder indent = new StringBuilder( 32 );

			for( int i=2; i<TraceBackCount; i++ )
			{
				// get method information
				StackFrame frame = new StackFrame( i );
				MethodBase method = frame.GetMethod();

				// format stack trace message
				buf.Append( indent.ToString() );
				buf.Append( method.ReflectedType.FullName + "." + method.Name );
				if( 0 < frame.GetFileLineNumber() )
				{
					buf.Append(
						frame.GetFileName() + " ("
						+ frame.GetFileLineNumber() + ", "
						+ frame.GetFileColumnNumber() + ")"
					);
				}
				buf.Append( "\r\n" );

				indent.Append( " " );
			}

			return buf.ToString();
		}

		/// <summary>
		/// Gets system performance counter value in millisecond.
		/// </summary>
		public static double PC
		{
			get
			{
				return (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
			}
		}
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

		[Conditional("DEBUG")]
		public static void Assert( bool condition, string format, params object[] args )
		{
			if( !condition )
				throw new AssertException( String.Format(format, args) );
		}
		#endregion
	}

	/// <summary>
	/// A logger for both .NET Framework and .NET Compact Framework.
	/// </summary>
	class AutoLogger
	{
		#region Fields
		const long MaxLogFileSize = 8 * 1024 * 1024;
		readonly StringBuilder _Buffer = new StringBuilder( 4096 );
		bool _Realtime = true;
		bool _WriteProcessID = false;
		bool _WriteThreadID = false;
		string _LogFilePath = null;
		string _OldLogFilePath = null;
		readonly static StringBuilder _IndentStr = new StringBuilder( 8 );
		bool _HeaderNotWritten = true;
		TextWriter _SecondOutput = Console.Out;
		public const string LogDateHeader = "[yyyy-MM-dd hh:mm:ss.fff] ";
		#endregion

		#region Init / Dispose
		~AutoLogger()
		{
			Flush();
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets whether process ID will be written in each log lines or not.
		/// </summary>
		public bool WritePID
		{
			get{ return _WriteProcessID; }
			set{ _WriteProcessID = value; }
		}

		/// <summary>
		/// Gets or sets whether thread ID will be written in each log lines or not.
		/// </summary>
		public bool WriteTID
		{
			get{ return _WriteThreadID; }
			set{ _WriteThreadID = value; }
		}

		/// <summary>
		/// Gets or sets whether written log lines are actually written to file instantly or not.
		/// </summary>
		public bool Realtime
		{
			get{ return _Realtime; }
			set{ _Realtime = value; }
		}

		/// <summary>
		/// Gets or sets additional message output target.
		/// </summary>
		public TextWriter SecondOutput
		{
			get{ return _SecondOutput; }
			set
			{
				if( value == null )
				{
					value = TextWriter.Null;
				}
				_SecondOutput = value;
			}
		}

		/// <summary>
		/// Gets path of log file.
		/// </summary>
		public string LogFilePath
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
		/// Gets path of backup of old log file.
		/// </summary>
		public string OldLogFilePath
		{
			get
			{
				if( _OldLogFilePath == null )
				{
					Assembly exe = Assembly.GetExecutingAssembly();
					string exePath = exe.GetModules()[0].FullyQualifiedName;
					string exeDirPath = Path.GetDirectoryName( exePath );
					_OldLogFilePath = Path.Combine( exeDirPath, "log.old" );
				}
				return _OldLogFilePath;
			}
		}
		#endregion

		#region Write
		/// <summary>
		/// Writes buffered data to file.
		/// </summary>
		public void Flush()
		{
			try
			{
				lock( this )
				{
					FileStream file;

					// if buffer is empty, do nothing
					if( _Buffer.Length <= 0 )
					{
						return;
					}

					// back up log if it is so large
					if( File.Exists(LogFilePath) )
					{
						if( MaxLogFileSize < new FileInfo(LogFilePath).Length )
						{
							File.Delete( OldLogFilePath );
							File.Move( LogFilePath, OldLogFilePath );
						}
					}

					// write buffered data
					using( file = File.Open(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite) )
					{
						byte[] bytes = Encoding.UTF8.GetBytes( _Buffer.ToString() );
						file.Write( bytes, 0, bytes.Length );
					}

					// clear buffer
					_Buffer.Length = 0;
				}
			}
			catch( IOException )
			{}
		}

		/// <summary>
		/// Writes message to a log file.
		/// </summary>
		public void Write( string format, params object[] p )
		{
			lock( this )
			{
				Write_Impl( format, p );
			}
		}

		void Write_Impl( string format, params object[] p )
		{
			TextWriter writer = null;

			try
			{
				// write header
				writer = new StringWriter( _Buffer );
				if( _HeaderNotWritten )
				{
					int pid;
					int tid = 0;
					DateTime now = DateTime.Now;
					StringBuilder pidPart = new StringBuilder( 32 );

					// append extra header info
					if( _WriteProcessID )
					{
						pid = Process.GetCurrentProcess().Id;
						tid = Thread.CurrentThread.ManagedThreadId;
						pidPart.Append( "[" + pid.ToString("X4") );
						if( _WriteThreadID )
						{
							pidPart.Append( "," + tid.ToString("X2") );
						}
						pidPart.Append( "] " );
					}

					writer.Write( now.ToString(LogDateHeader) );
					writer.Write( pidPart.ToString() );
					writer.Write( _IndentStr.ToString() );
					if( SecondOutput != null )
					{
						SecondOutput.Write( now.ToString(LogDateHeader) );
						SecondOutput.Write( pidPart.ToString() );
						SecondOutput.Write( _IndentStr.ToString() );
					}

					_HeaderNotWritten = false;
				}

				// write message
				writer.Write( String.Format(format, p) );
				if( SecondOutput != null )
				{
					SecondOutput.Write( String.Format(format, p) );
				}

				// flush
				if( Realtime )
				{
					Flush();
				}
			}
			catch( IOException )
			{}
			catch( UnauthorizedAccessException )
			{}
			catch( System.Security.SecurityException )
			{}
			catch( Exception ex )
			{
				Debug.Fail( ex.ToString() );
			}
			finally
			{
				if( writer != null )
				{
					writer.Close();
				}
			}
		}

		/// <summary>
		/// Writes message to a log file and terminate the line.
		/// </summary>
		public void WriteLine( string format, params object[] p )
		{
			lock( this )
			{
				Write( format, p );
				Write( Console.Out.NewLine );
				_HeaderNotWritten = true;
			}
		}

		/// <summary>
		/// Writes message to a log file and adds indent.
		/// </summary>
		public void WriteLineI( string format, params object[] p )
		{
			lock( this )
			{
				WriteLine( format, p );
				Indent();
			}
		}

		/// <summary>
		/// Writes message to a log file and adds unindent.
		/// </summary>
		public void WriteLineU( string format, params object[] p )
		{
			lock( this )
			{
				Unindent();
				WriteLine( format, p );
			}
		}

		/// <summary>
		/// Increases indentation of log message.
		/// </summary>
		public void Indent()
		{
			lock( this )
			{
				_IndentStr.Append( "  " );
			}
		}

		/// <summary>
		/// Decreases indentation of log message.
		/// </summary>
		public void Unindent()
		{
			lock( this )
			{
				_IndentStr.Length = Math.Max( 0, _IndentStr.Length - 2 );
			}
		}
		#endregion
	}

	#region Minimal Testing Framework
#	if DEBUG
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
					throw new AssertException( "Objects were not equal.\nExpected: null\nActual: " + actual.ToString() );
			}
			else if( !expected.Equals(actual) )
			{
				if( actual != null )
					throw new AssertException( "Objects were not equal.\nExpected: " + expected.ToString() + "\nActual:   "+actual.ToString() );
				else
					throw new AssertException( "Objects were not equal.\nExpected: " + expected.ToString() + "\nActual:   null" );
			}
		}

		public static void AssertType<T>( object obj )
		{
			if( obj.GetType() != typeof(T) )
				throw new AssertException( "object type is not "+typeof(T).Name+" but "+obj.GetType()+"." );
		}

		public static void AssertExceptionType<T>( Exception ex )
		{
			AssertExceptionType( ex, typeof(T) );
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
