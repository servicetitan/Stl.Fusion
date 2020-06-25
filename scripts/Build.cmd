@echo off

pushd ..
dotnet build %*
popd
