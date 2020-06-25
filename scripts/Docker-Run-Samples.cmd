@echo off

pushd ..
docker-compose build
start "Stl.Samples.Blazor.Server" docker-compose up
timeout 2
popd

call Start-Chrome-Debug.cmd
