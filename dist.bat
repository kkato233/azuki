@echo off
setlocal

set VER=%~1

call configure.bat

:: Determine version
if "%VER%" == "" (
    set /p VER="Please input version string (ex: 1.2.0):"
)

:PHASE1
echo ========================================
echo   [1/4] build assembly
echo ========================================
call build.bat
if not "%ERRORLEVEL%" == "0" (
    goto ERROR
)

:PHASE2
echo.
echo ========================================
echo   [2/4] run tests
echo ========================================
call test.bat
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
    echo Failed to create a binary distribution file. ^(code:%errorlevel%^)
    goto ERROR
)
popd

pushd doc
move .\Release\*.chm .\
%SEVENZIP% a -tzip -mx=9 ..\package\zip\Azuki-%VER%-api-web.zip Release
if not "%ERRORLEVEL%" == "0" (
    echo Failed to zip API document ^(web^) file. ^(code:%errorlevel%^)
    goto ERROR
)
%SEVENZIP% a -tzip -mx=9 ..\package\zip\Azuki-%VER%-api-chm.zip .\*.chm
if not "%ERRORLEVEL%" == "0" (
    echo Failed to zip API document ^(CHM^) file. ^(code:%errorlevel%^)
    goto ERROR
)
popd

echo ========================================
echo ok.
echo.
goto :EOF

:ERROR
echo failed to make distribution package.
