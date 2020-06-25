@echo off
docker-compose build
start "Stl.Samples.Blazor.Server" docker-compose up
timeout 2
start "Chrome" chrome.exe "http://localhost:5000/"
