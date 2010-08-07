@echo off

cd Azuki
call clean.bat
cd ..

cd Ann
call clean.bat
cd ..

cd doc
call clean.bat
cd ..

del         *.sln.cache     2> NUL
del         *.userprefs     2> NUL
del /ah     *.suo           2> NUL
del         package\log.txt 2> NUL
rmdir /s /q package\zip     2> NUL
