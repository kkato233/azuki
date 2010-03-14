// 2010-02-13
using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace Sgry.Ann
{
	/// <summary>
	/// Named Mutex object.
	/// </summary>
	/// <remarks>
	/// <para>
	/// CF.NET has no named mutex but WinCE or Windows Mobile has it.
	/// This class realizes named mutex by P/Invoke on CF.NET,
	/// and realizes named mutex by standard Mutex object on Full .NET.
	/// </para>
	/// </remarks>
	class MyMutex : IDisposable
	{
#		if !PocketPC
		Mutex _Mutex;
#		else
		IntPtr _Mutex;
#		endif

		public MyMutex( bool initiallyOwned, string name )
		{
#			if !PocketPC
			_Mutex = new Mutex( initiallyOwned, name );
#			else
			_Mutex = CreateMutexW( IntPtr.Zero, initiallyOwned ? 1 : 0, name );
#			endif
		}

		~MyMutex()
		{
			Dispose();
		}

		public void Dispose()
		{
#			if !PocketPC
			if( _Mutex != null )
			{
				_Mutex.Close();
				_Mutex = null;
			}
#			else
			if( _Mutex != IntPtr.Zero )
			{
				CloseHandle( _Mutex );
				_Mutex = IntPtr.Zero;
			}
#			endif
		}

		public bool WaitOne( int millisecondsTimeout )
		{
#			if !PocketPC
			return _Mutex.WaitOne( millisecondsTimeout, false );
#			else
			const int WAIT_OBJECT_0 = 0;
			UInt32 rc = WaitForSingleObject( _Mutex, millisecondsTimeout );
			return (rc == WAIT_OBJECT_0);
#			endif
		}

		public void ReleaseMutex()
		{
#			if !PocketPC
			_Mutex.ReleaseMutex();
#			else
			ReleaseMutex( _Mutex );
#			endif
		}

#		if PocketPC
		[DllImport("coredll")]
		public static extern Int32 ReleaseMutex( IntPtr mutex );

		[DllImport("coredll")]
		public static extern Int32 CloseHandle( IntPtr h );

		[DllImport("coredll", CharSet=CharSet.Unicode)]
		public static extern IntPtr CreateMutexW( IntPtr NULL, Int32 bInitialOwner, string name );

		[DllImport("coredll")]
		public static extern UInt32 WaitForSingleObject( IntPtr handle, Int32 milliseconds );
#		endif
	}
}
