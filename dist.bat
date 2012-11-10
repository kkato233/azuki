@echo off
setlocal

set FDIR=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319
set VER=%~1
set SEVENZIP=a
set MSBUILD_OPT=-nologo -v:m -t:Build -clp:ForceNoAlign;ShowCommandLine

:: test environment
(%FDIR%\MSBuild.exe 2>&1) > NUL
if "%ERRORLEVEL%" == "9009" (
	echo msbuild.exe was not found.
	goto ERROR
)
(7z 2>&1) > NUL
if not "%ERRORLEVEL%" == "9009" (
	set SEVENZIP=7z
)
(7za 2>&1) > NUL
if not "%ERRORLEVEL%" == "9009" (
	set SEVENZIP=7za
)
if "%SEVENZIP%" == "a" (
	echo 7-Zip commands were not found in PATH.
	goto ERROR
)

:: Determine version
if "%VER%" == "" (
	set /p VER="Please input version string (ex: 1.2.0):"
)


:PHASE1
echo ========================================
echo   [1/4] run tests
echo ========================================
%FDIR%\MSBuild.exe AzukiTest.vs9.sln %MSBUILD_OPT%
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)

pushd package
AzukiTest.exe
popd
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)

:PHASE2
echo.
echo ========================================
echo   [2/4] build assembly
echo ========================================
%FDIR%\MSBuild.exe All.vs9.sln %MSBUILD_OPT% -p:Configuration=Release
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)
echo.

:PHASE3
echo.
echo ========================================
echo   [3/4] generating API document
echo ========================================
pushd doc
	%FDIR%\MSBuild.exe  /p:Configuration=Release  /p:CleanIntermediates=True  Document.shfbproj
popd
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)

:PHASE4
echo.
echo ========================================
echo   [4/4] make archive
echo ========================================
pushd package
%SEVENZIP% a -tzip -mx=9 .\zip\Azuki-%VER%-bin.zip @dist.list
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)
popd

pushd doc
move .\Release\*.chm .\
%SEVENZIP% a -tzip -mx=9 ..\package\zip\Azuki-%VER%-api-web.zip Release
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)
%SEVENZIP% a -tzip -mx=9 ..\package\zip\Azuki-%VER%-api-chm.zip .\*.chm
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)
popd

echo ========================================
echo ok.
echo.
goto :EOF

:ERROR
echo failed to make distribution package.
