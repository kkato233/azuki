@echo off
setlocal

call configure.bat

%MSBUILD% Azuki.vs9.sln %MSBUILD_OPT% -p:Configuration=Release
if not "%errorlevel%" == "0" (
    echo Failed to build. ^(code:%errorlevel%^)
    goto :EOF
)
