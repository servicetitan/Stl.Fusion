@echo off

pushd ..\docker
docker-compose build
start "Stl.Samples.Blazor.Server" docker-compose up
timeout 2
popd

start "Samples" http://localhost:5000/
