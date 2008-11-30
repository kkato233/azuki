@echo off
setlocal

echo Cleaning Ann...

rmdir /S /Q obj  2> NUL
rmdir /S /Q bin  2> NUL
rmdir /S /Q Debug  2> NUL
rmdir /S /Q Release  2> NUL
del /Q *.csproj.user  2> NUL

del ..\package\Ann*.exe  2> NUL
del ..\package\Ann*.pdb  2> NUL
del ..\package\Ann*.xml  2> NUL
del ..\package\Ann*.exe.log.txt  2> NUL
del ..\package\Ann*.vshost.exe  2> NUL
del ..\package\Ann*.vshost.exe.manifest  2> NUL

endlocal
echo.
