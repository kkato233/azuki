@echo off
setlocal

echo ----------------------------------------
pushd Azuki
nant -nologo %*
popd
if not "%ERRORLEVEL%" == "0" (
	goto :EOF
)

echo ----------------------------------------
pushd Ann
nant -nologo %*
popd
if not "%ERRORLEVEL%" == "0" (
	goto :EOF
)

