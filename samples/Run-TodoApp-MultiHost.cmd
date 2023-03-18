@echo off

set ASPNETCORE_ENVIRONMENT=Development
set ExePath=../artifacts/samples/Host/net7.0/Templates.TodoApp.Host.dll

set ASPNETCORE_URLS=http://localhost:5005/
start cmd /C timeout 5 ^& start http://localhost:5005/"
start cmd /C dotnet %ExePath%

set ASPNETCORE_URLS=http://localhost:5006/
start cmd /C timeout 5 ^& start http://localhost:5006/"
start cmd /C dotnet %ExePath%
