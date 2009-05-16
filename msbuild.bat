@echo off
setlocal

set _OPT=-nologo -v:m -t:Build
set _OPT=%_OPT% /clp:ForceNoAlign
::set _OPT=%_OPT%;ShowCommandLine

:: determine build configuration
if "%1"=="debug" (
	set _CONF=Debug
	echo # debug build
) else (
	set _CONF=Release
)
set _OPT=%_OPT% -p:Configuration=%_CONF%

:: build
echo ===== begin msbuild =====
msbuild.exe All.vs9.sln %_OPT%
echo ===== end msbuild =====
