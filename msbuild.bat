@echo off
setlocal

set _OPT=-nologo -v:m -t:Build -validate
set _OPT=%_OPT% -p:Configuration=Release -clp:ForceNoAlign
set _OPT=%_OPT%;ShowCommandLine
set _SOLUTION_FILE=%~1

if "%~1"=="" (
	echo # no solution file was specified; using All.vs8.sln.
	set _SOLUTION_FILE=All.vs8.sln
)

:: build
echo ===== begin msbuild =====
msbuild.exe "%_SOLUTION_FILE%" %_OPT%
echo ===== end msbuild =====
