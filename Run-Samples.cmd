@echo off
rem dotnet build
set ASPNETCORE_ENVIRONMENT=Development
start "Stl.Samples.Blazor.Server" /D artifacts\samples\Stl.Samples.Blazor.Server Stl.Samples.Blazor.Server.exe
call Start-Chrome-Debug.cmd
