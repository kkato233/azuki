@echo off
setlocal

call configure.bat

%MSBUILD% /nologo /v:quiet AzukiTest\AzukiTest.vs9.csproj
if not %errorlevel% == 0 (
    echo Failed to execute msbuild command. ^(code:%errorlevel%^)
    goto :EOF
)
echo.

%NUNIT% /nologo /noxml AzukiTest\bin\Debug\AzukiTest.dll
if not %errorlevel% == 0 (
    echo Failed to execute NUnit runner. ^(code:%errorlevel%^)
    goto :EOF
)
