@echo off

pushd ..
dotnet build
set ASPNETCORE_ENVIRONMENT=Development
start "Stl.Samples.Blazor.Server" /D artifacts\samples\Stl.Samples.Blazor.Server Stl.Samples.Blazor.Server.exe
popd

call Start-Chrome-Debug.cmd
