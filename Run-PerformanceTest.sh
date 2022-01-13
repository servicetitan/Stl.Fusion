#!/bin/bash
export DOTNET_ReadyToRun=0 
export DOTNET_TieredPGO=1 
export DOTNET_TC_QuickJitForLoops=1 

dotnet run --no-launch-profile -c:Release -f:net6.0 --project tests/Stl.Fusion.Tests.PerformanceTestRunner/Stl.Fusion.Tests.PerformanceTestRunner.csproj
