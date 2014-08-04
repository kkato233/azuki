using System;
#if USEING_NUNIT
using Assert = NUnit.Framework.Assert;
#else
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
#endif

namespace Sgry.Azuki.Test
{
	static class MyAssert
	{
		public delegate void MyTestDelegate();

		public static void Throws<T>( MyTestDelegate d ) where T : Exception
		{
			try
			{
				d();
			}
			catch( Exception e )
			{
#if USEING_NUNIT
				Assert.IsInstanceOf<T>( e );
#else
				Assert.IsInstanceOfType( e, typeof(T) );
#endif
			}
		}

		public static void DoesNotThrow( MyTestDelegate d )
		{
			try
			{
				d();
			}
			catch( Exception e )
			{
				Assert.Fail( e.ToString() );
			}
		}
	}
}
