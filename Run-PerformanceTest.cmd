@echo off
set DOTNET_ReadyToRun=0 
set DOTNET_TieredPGO=1 
set DOTNET_TC_QuickJitForLoops=1

set runtime=%1
if "%runtime%"=="" (
  set runtime=net6.0
)

pushd "artifacts/tests/%runtime%"
"Stl.Fusion.Tests.PerformanceTestRunner.exe"
popd
