pushd ..
procgov64 --maxmem=4G --cpu=0x1 --recursive dotnet.exe test
popd
