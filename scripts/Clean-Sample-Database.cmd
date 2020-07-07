@echo off

for /D %%i in ("%TEMP%\Stl_Samples_Blazor_Server*") do (
  rmdir /S /Q "%%i"
)
