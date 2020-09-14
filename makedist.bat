@echo off
setlocal

set VER=%~1
set MSBUILD_OPT=-nologo -v:m -t:Build -clp:ForceNoAlign;ShowCommandLine
set SRC=
set SRC=%SRC% resource\*
set SRC=%SRC% Azuki\bin\Release\Azuki.dll
set SRC=%SRC% Azuki\bin\Release\Azuki.xml
set SRC=%SRC% Ann\bin\Release\Ann.exe
set SEVENZIP=%~dp0tools\7za\7za.exe

cd "%~dp0"

:: Determine version
if "%VER%" == "" (
    set /p VER="Please input version string (ex: 1.2.0):"
)

echo ------------------------------------------------------------
echo Making binary distribution...
if not exist package      mkdir package
if not exist package\temp mkdir package\temp
for %%I in (%SRC%) do (
    if not exist "%%I" (
        echo %%I was not found.
        goto :EOF
    )
    copy /b /v /d /y "%%I" package\temp\ > NUL
)
if not "%ERRORLEVEL%" == "0" (
    echo Failed to copy %%I.
    goto :EOF
)

pushd package\temp\
"%SEVENZIP%" a -tzip -mx=9 ..\Azuki-%VER%-bin.zip @dist.list > NUL
if not "%ERRORLEVEL%" == "0" (
    echo Failed to create a binary distribution file. ^(code:%errorlevel%^)
    goto :EOF
)
popd


echo ------------------------------------------------------------
echo Making document archive...
pushd doc
if exist "Help\*.chm" move "Help\*.chm" .\ > NUL
"%SEVENZIP%" a -tzip -mx=9 "..\package\Azuki-%VER%-api-web.zip" Help > NUL
if not "%ERRORLEVEL%" == "0" (
    echo Failed to zip API document ^(web^) file. ^(code:%errorlevel%^)
    goto :EOF
)
"%SEVENZIP%" a -tzip -mx=9 "..\package\Azuki-%VER%-api-chm.zip" *.chm > NUL
if not "%ERRORLEVEL%" == "0" (
    echo Failed to zip API document ^(CHM^) file. ^(code:%errorlevel%^)
    goto :EOF
)
popd

echo done.
