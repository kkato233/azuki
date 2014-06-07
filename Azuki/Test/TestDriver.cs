#if TEST
using System;

namespace Sgry.Azuki.Test
{
	static class Tester
	{
		static int Main()
		{
			SplitArrayTest.Test();
			RleArrayTest.Test();
			TextUtilTest.Test();
			DocumentTest.Test();
			DefaultWordProcTest.Test();
			CaretMoveLogicTest.Test();
			EditHistoryTest.Test();
			KeywordHighlighterTest.Test();
			UriMarkerTest.Test();
			FixedBugsTest.Test();
			
			if( TestUtl.ErrorOccured )
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Error.WriteLine( "ERROR (count:{0})", TestUtl.ErrorCount );
				Console.ResetColor();
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Error.WriteLine( "SUCCESS" );
				Console.ResetColor();
			}

			return TestUtl.ErrorCount;
		}
	}
}
#endif
