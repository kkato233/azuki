echo off
pushd Azuki
rmdir obj /S /Q
rmdir bin /S /Q
msbuild Azuki.vs2010.csproj -p:configuration=Release
dotnet build Azuki.Core.csproj --configuration Release
popd
rmdir build\lib /S /Q
mkdir build\lib\net20
mkdir build\lib\netcoreapp3.0
copy Azuki\bin\Release\Azuki.dll build\lib\net20
copy Azuki\bin\Release\netcoreapp3.0\Azuki.Core.dll build\lib\netcoreapp3.0
copy Azuki.nuspec build
nuget pack build\Azuki.nuspec
if errorlevel 1 goto nuget_err
goto end
:nuget_err
start https://www.nuget.org/downloads
:end
