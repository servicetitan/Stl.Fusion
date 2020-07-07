@echo off

pushd ..
dotnet build
# The next line is optional - you need it if you want to debug Blazor client
set ASPNETCORE_ENVIRONMENT=Development
start "Stl.Samples.Blazor.Server" dotnet artifacts/samples/Stl.Samples.Blazor.Server/Stl.Samples.Blazor.Server.dll
# start "Samples" http://localhost:5000/
popd

call Start-Chrome-Debug.cmd
