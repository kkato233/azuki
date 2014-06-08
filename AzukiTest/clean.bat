@echo off
setlocal

echo Cleaning AzukiTest...

rmdir /S /Q obj  2> NUL
rmdir /S /Q bin  2> NUL
rmdir /S /Q Debug  2> NUL
rmdir /S /Q Release  2> NUL
del /Q *.csproj.user  2> NUL
del /Q *.pidb         2> NUL

endlocal
echo.
