@echo off
setlocal

:: Sandcastle Help File Builder directory path
set SHFB_DIR=C:\Program Files\Sandcastle Help File Builder

:: execute SHFB console version to ensure existence
("%SHFB_DIR%\SandcastleBuilderConsole.exe" > NUL) 2> NUL
if not %ERRORLEVEL% == 1 (
	echo Failed to execute SandcastleBuilderConsole.exe.
	echo Please edit doc.bat and set install-path to SHFB_DIR variable.
	echo Currently SHFB_DIR is set as next:
	echo %SHFB_DIR%
	echo.
	goto :EOF
)

:: okay, SHFB exists. execute it.
pushd doc
"%SHFB_DIR%\SandcastleBuilderConsole.exe" api.shfb
popd
