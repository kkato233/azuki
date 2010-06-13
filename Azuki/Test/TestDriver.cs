// 2008-12-31
#if TEST
using System;

namespace Sgry.Azuki.Test
{
	static class Tester
	{
		static int Main()
		{
			CaretMoveLogicTest.Test();
			EditHistoryTest.Test();
			SplitArrayTest.Test();
			LineLogicTest.Test();
			DocumentTest.Test();
			KeywordHighlighterTest.Test();
			
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
