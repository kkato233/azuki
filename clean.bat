@echo off

for %%i in (Ann,Azuki,AzukiTest,doc) do (
    pushd %%i
    call clean.bat
    popd
)

del         *.sln.cache     2> NUL
del         *.userprefs     2> NUL
del /ah     *.suo           2> NUL
del         package\log.txt 2> NUL
rmdir /s /q package\zip     2> NUL
