@echo off
setlocal

set version=
set sevenzip=a

:: test environment
(nant 2>&1) > NUL
if "%ERRORLEVEL%" == "9009" (
	echo NAnt was not found in PATH.
	goto ERROR
)
(7z 2>&1) > NUL
if not "%ERRORLEVEL%" == "9009" (
	set sevenzip=7z
)
(7za 2>&1) > NUL
if not "%ERRORLEVEL%" == "9009" (
	set sevenzip=7za
)
if "%sevenzip%" == "a" (
	echo 7-Zip commands were not found in PATH.
	goto ERROR
)

:: ask for version
set /p version="Please input version string (ex: 1.2.0):"


:PHASE1
echo ========================================
echo   [1/4] run tests
echo ========================================
nant -nologo -q test
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)

pushd package
test.exe
popd
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)

:PHASE2
echo.
echo ========================================
echo   [2/4] build assembly
echo ========================================
echo building FF version...
nant -nologo -q build
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)

echo building CF version...
nant -nologo -q cf
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)
echo.

:PHASE3
echo.
echo ========================================
echo   [3/4] generating API document
echo ========================================
call doc.bat
::if not "%ERRORLEVEL%" == "0" (
::	goto ERROR
::)

:PHASE4
echo.
echo ========================================
echo   [4/4] make archive
echo ========================================
pushd package
%sevenzip% a -tzip -mx=9 .\zip\Azuki-%version%-bin.zip @dist.list
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)
popd

pushd doc
move .\Release\*.chm .\
%sevenzip% a -tzip -mx=9 ..\package\zip\Azuki-%version%-api-web.zip Release
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)
%sevenzip% a -tzip -mx=9 ..\package\zip\Azuki-%version%-api-chm.zip .\*.chm
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
