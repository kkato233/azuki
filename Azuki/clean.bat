@echo off
setlocal

echo Cleaning Azuki...

rmdir /S /Q obj  2> NUL
rmdir /S /Q bin  2> NUL
rmdir /S /Q Debug  2> NUL
rmdir /S /Q Release  2> NUL
del /Q *.csproj.user  2> NUL

del ..\package\Azuki.pdb  2> NUL
del ..\package\Azuki.dll  2> NUL
del ..\package\Azuki.xml  2> NUL
del ..\package\AzukiCompact.pdb  2> NUL
del ..\package\AzukiCompact.dll  2> NUL
del ..\package\AzukiCompact.xml  2> NUL
del ..\package\Test.pdb  2> NUL
del ..\package\Test.exe  2> NUL

del ..\package\Azuki.exe  2> NUL
del ..\package\Azuki.vshost.*  2> NUL
del ..\package\Azuki.exe.log.txt  2> NUL

endlocal
echo.
