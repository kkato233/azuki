@echo off
setlocal

echo Cleaning Documentation project...

rmdir /S /Q Release 2> NUL
del         *.chm   2> NUL

echo.
