// 2010-02-13
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Sgry.Ann
{
	/// <summary>
	/// A pseudo half-duplex pipe emulated by using file.
	/// </summary>
	class PseudoPipe : IDisposable
	{
		readonly byte[] EOL = new byte[]{ (byte)'\r', (byte)'\n' };
		string _FilePath;
		FileStream _File = null;
		Mutex _Mutex = null;

		/// <summary>
		/// Finalize an instance.
		/// </summary>
		~PseudoPipe()
		{
			Dispose();
		}

		/// <summary>
		/// Disposes all resources used by this instance.
		/// </summary>
		public void Dispose()
		{
			if( _File != null )
			{
				_File.Close();
				_File = null;
			}
			if( _Mutex != null )
			{
				_Mutex.Close();
				_Mutex = null;
			}
		}

		/// <summary>
		/// Creates pseudo pipe for receiveing message from other applications.
		/// </summary>
		/// <param name="filePath">Path of a file which is used to buffer messages.</param>
		public void Create( string filePath )
		{
			Debug.Assert( filePath != null );

			// remember parameters
			_FilePath = filePath;
			_Mutex = new Mutex( false, MakeMutexName(filePath) );

			// open pseudo pipe file
			_File = File.Open( filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite );
			_File.SetLength( 0 );
		}

		/// <summary>
		/// Connects already existing pseudo pipe.
		/// </summary>
		/// <param name="filePath">Path of a file which is used to buffer messages.</param>
		public void Connect( string filePath )
		{
			Debug.Assert( filePath != null );
			if( _FilePath != null )
				throw new InvalidOperationException( "This pseudo pipe was already connected." );

			// remember parameters
			_FilePath = filePath;
			_Mutex = new Mutex( false, MakeMutexName(filePath) );

			// open pseudo pipe file
			_File = File.Open( filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite );
		}

		/// <summary>
		/// Gets date and time a message was lastly written to this pipe.
		/// </summary>
		public DateTime GetLastWriteTime()
		{
			return File.GetLastWriteTime( _FilePath );
		}

		/// <summary>
		/// Reads all lines from pseudo pipe.
		/// </summary>
		/// <exception cref="TimeoutException">Timed out</exception>
		public string[] ReadLines( int millisecondsTimeout )
		{
			bool owned;
			StreamReader reader;
			string line;
			List<string> lines = new List<string>();

			// enter critical section
			owned = _Mutex.WaitOne( millisecondsTimeout );
			if( owned == false )
			{
				throw new TimeoutException();
			}

			try
			{
				// read lines
				reader = new StreamReader( _File, Encoding.UTF8 );
				line = reader.ReadLine();
				while( line != null )
				{
					// remember this line
					lines.Add( line );

					// read next line
					line = reader.ReadLine();
				}

				// make the file empty
				_File.SetLength( 0 );
			}
			finally
			{
				_Mutex.ReleaseMutex();
			}

			return lines.ToArray();
		}

		/// <summary>
		/// Writes a line to pseudo pipe.
		/// </summary>
		public void WriteLine( string line, int millisecondsTimeout )
		{
			bool owned;
			byte[] encodedLine;

			// enter critical section
			owned = _Mutex.WaitOne( millisecondsTimeout );
			if( owned == false )
			{
				return; // timeout
			}

			try
			{
				// read lines
				encodedLine = Encoding.UTF8.GetBytes( line );
				_File.Write( encodedLine, 0, encodedLine.Length );
				_File.Write( EOL, 0, EOL.Length );
				_File.Flush();
			}
			finally
			{
				_Mutex.ReleaseMutex();
			}
		}

		#region Utilities
		static string MakeMutexName( string filePath )
		{
			StringBuilder buf = new StringBuilder( 256 );

			buf.Append( "Sgry.PseudoPipe." );
			for( int i=0; i<filePath.Length; i++ )
			{
				if( filePath[i] == '\\' || filePath[i] == '/' )
				{
					buf.Append( '.' );
				}
				else if( filePath[i] == ':' )
				{
					;
				}
				else
				{
					buf.Append( filePath[i] );
				}
			}

			return buf.ToString();
		}
		#endregion
	}
}
