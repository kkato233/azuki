@echo off
setlocal

set version=
set sevenzip=a

:: test environment
(nant 2>&1) > NUL
if "%ERRORLEVEL%" == "9009" (
	echo NAnt was not found in PATH.
	goto :EOF
)
(7z 2>&1) > NUL
if not "%ERRORLEVEL%" == "9009" (
	set sevenzip=7z.exe
)
(7za 2>&1) > NUL
if not "%ERRORLEVEL%" == "9009" (
	set sevenzip=7za.exe
)
if "%sevenzip%" == "" (
	echo 7-Zip commands were not found in PATH.
	goto :EOF
)

:: ask for version
set /p version="Please input version string (ex: 1.2.0):"


:PHASE1
echo ========================================
echo   [1/4] run tests
echo ========================================
nant -nologo -q test
if not "%ERRORLEVEL%" == "0" (
	goto :EOF
)

pushd package
test.exe
popd
if not "%ERRORLEVEL%" == "0" (
	goto :EOF
)

:PHASE2
echo.
echo ========================================
echo   [2/4] build assembly
echo ========================================
nant -nologo -q build
if not "%ERRORLEVEL%" == "0" (
	goto :EOF
)

nant -nologo -q cf
if not "%ERRORLEVEL%" == "0" (
	goto :EOF
)

:PHASE3
echo.
echo ========================================
echo   [3/4] generating API document
echo ========================================
call doc.bat
echo "%ERRORLEVEL%"
if not "%ERRORLEVEL%" == "0" (
	goto :EOF
)

:PHASE4
echo.
echo ========================================
echo   [4/4] make archive
echo ========================================
pushd package
%sevenzip% a -tzip -mx=9 Azuki-%version%.zip @dist.list
popd
if not "%ERRORLEVEL%" == "0" (
	goto :EOF
)
%sevenzip% a -tzip -mx=9 .\package\Azuki-%version%-api.zip api
if not "%ERRORLEVEL%" == "0" (
	goto ERROR
)

echo ok.
goto :EOF

:ERROR
echo error.
