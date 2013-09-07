using System;
using System.Text.RegularExpressions;

namespace Sgry.Azuki.Highlighter
{
	using CC = CharClass;

	class BatchFileHighlighter : KeywordHighlighter
	{
		public BatchFileHighlighter()
		{
			AddKeywordSet( new string[] {
				"assoc", "attrib", "bcdedit", "break", "cacls", "call", "cd",
				"chcp", "chdir", "chkdsk", "chkntfs", "cls", "cmd", "color",
				"comp", "compact", "convert", "copy", "date", "del",
				"dir", "diskcomp", "diskcopy", "diskpart", "doskey",
				"driverquery", "echo", "echo.", "endlocal", "erase", "exit",
				"fc", "find", "findstr", "for", "format", "fsutil", "ftype",
				"goto", "gpresult", "graftabl", "icacls", "if", "label", "md",
				"mkdir", "mklink", "mode", "more", "move", "openfiles", "path",
				"pause", "popd", "print", "prompt", "pushd", "rd", "recover",
				"ren", "rename", "replace", "rmdir", "robocopy", "sc",
				"schtasks", "set", "setlocal", "shift", "shutdown", "sort",
				"start", "subst", "systeminfo", "taskkill", "tasklist", "time",
				"title", "tree", "type", "ver", "verify", "wmic", "xcopy"
			}, CC.Keyword, true );

			AddKeywordSet( new string[] {
				"AUX",
				"COM0", "COM1", "COM2", "COM3", "COM4",
				"COM5", "COM6", "COM7", "COM8", "COM9",
				"CON",
				"LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
				"LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
				"NUL", "PRN"
			}, CC.Keyword2, false );

			AddLineHighlight( "REM", CC.Comment, true );
			AddLineHighlight( "::", CC.Comment );

			AddRegex( @"(?<=if )not", true, CC.Keyword );
			AddRegex( @"@?echo (on|off)", true, CC.Keyword2 );
			AddRegex( @"%\w+(:[^%]+)?%", false, CC.Variable );
			AddRegex( @"%~?[0-9\*]", false, CC.Variable );
			AddRegex( @"%%?~?[fdpnxsatz]*[0-9a-zA-Z]", false, CC.Variable );
			AddRegex( @"^:\w+", true, CC.Label );
		}
	}
}
