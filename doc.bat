@echo off
setlocal

:: Sandcastle Help File Builder directory path
set SHFB_DIR=%ProgramFiles%\Sandcastle Help File Builder

:: set PATH
set PATH_BACKUP=%PATH%
set PATH=%PATH%;%SHFB_DIR%

:: execute SHFB console version to ensure existence
(SandcastleBuilderConsole.exe > NUL) 2> NUL
if not %ERRORLEVEL% == 1 (
	echo Failed to execute SandcastleBuilderConsole.exe.
	echo Please edit doc.bat and set install-path to SHFB_DIR variable.
	echo.
	goto :EOF
)

:: okay, SHFB exists. execute it.
pushd doc
SandcastleBuilderConsole.exe Document.shfb
popd

set PATH=%PATH_BACKUP%

