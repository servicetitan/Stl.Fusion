pushd ..
procgov64 --maxmem 4G --cpu 1 --recursive dotnet.exe test
popd
