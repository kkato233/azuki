@echo off

set SEVENZIP=
set NUNIT=
set MSBUILD=
set MSBUILD_OPT=-nologo -v:m -t:Build -clp:ForceNoAlign;ShowCommandLine

REM --------------------------------------------------------
:MSBUILD
for %%i in (msbuild.exe) do (
    set MSBUILD=%%~$PATH:i
)
if not "%MSBUILD%" == "" goto SEVENZIP

set MSBUILD=%SystemRoot%\Microsoft.NET\Framework\v3.5\msbuild.exe
if not "%MSBUILD%" == "" goto SEVENZIP

if "%MSBUILD%" == "" (
    echo msbuild.exe was not found.
    goto :EOF
)

REM --------------------------------------------------------
:SEVENZIP
for %%i in (tools\7za\7za.exe) do (
    set SEVENZIP=%%~fi
)

REM --------------------------------------------------------
:NUNIT
for %%i in (tools\NUnit\bin\nunit-console.exe) do (
    set NUNIT=%%~fi
)
