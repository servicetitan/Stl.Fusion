@echo off

pushd ..
rmdir /S /Q artifacts 2>nul
dotnet build -c Release
dotnet pack -c Release
pushd artifacts\nupkg
call :publish Stl.nupkg
call :publish Stl.Fusion.nupkg
call :publish Stl.Fusion.Client.nupkg
call :publish Stl.Fusion.Server.nupkg
call :publish Stl.Fusion.Blazor.nupkg
call :publish Stl.Plugins.nupkg
popd
popd
goto :eof

:publish
dotnet nuget push %1 -k %NUGET_ORK_API_KEY% -s https://api.nuget.org/v3/index.json
goto :eof
