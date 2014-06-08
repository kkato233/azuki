@echo off
setlocal

call configure.bat

REM Sandcastle Help File Builder needs msbuild v4.0
"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe" doc\Document.shfbproj /nologo /p:Configuration=Release  /p:CleanIntermediates=True
if not "%errorlevel%" == "0" (
    echo Failed to generate document. ^(code:%errorlevel%^)
    goto :EOF
)
