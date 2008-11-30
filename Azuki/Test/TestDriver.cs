// 2008-06-17
#if DEBUG
using System;

namespace Sgry.Azuki.Test
{
	static class Tester
	{
		static int Main()
		{
			LineLogicTest.Test();
			EditHistoryTest.Test();
			SplitArrayTest.Test();
			DocumentTest.Test();
			KeywordHighlighterTest.Test();
			if( TestUtl.ErrorOccured )
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Error.WriteLine( "{0} error(s) occured.", TestUtl.ErrorCount );
				Console.ResetColor();
				return 1;
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.Error.WriteLine( "test succeeded." );
			Console.ResetColor();
			return 0;
		}
	}
}
#endif
