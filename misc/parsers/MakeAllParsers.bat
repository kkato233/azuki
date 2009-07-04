@echo off
setlocal

echo # begin generating parsers.
for %%i in (Coco.exe) do (
	if exist "%%i" (
		echo # using "%%~fi"
		goto main
		echo 1
	)
	if exist "%%~$PATH:i" (
		echo # using "%%~$PATH:i"
		goto main
		echo 2
	)
	echo # ERROR: Coco.exe not found.
)
goto :EOF
::---------------------------------------------------------
:main
	for %%i in (*.atg) do (
		echo [%%~nxi]
		call :GenerateParser "%%i"
		echo.
	)
	
	exit /b 0
::---------------------------------------------------------
:GenerateParser
	if not exist "%~1" goto :EOF

	set outdir=..\..\Azuki\Highlighter\Coco
	set fullpath=%~f1
	set filename=%~n1

	coco -namespace Sgry.Azuki.Highlighter.Coco.%filename% "%fullpath%"
	if not "%ErrorLevel%" == "0" (
		exit /b 1
	)
	move Scanner.cs "%outdir%\%filename%Scanner.cs" > NUL
	echo generated %outdir%\%filename%Scanner.cs

	move Parser.cs "%outdir%\%filename%Parser.cs" > NUL
	echo generated %outdir%\%filename%Parser.cs
	
	exit /b 0
::---------------------------------------------------------
