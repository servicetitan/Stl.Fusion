pushd ..
dotnet run --project build --configuration Release --no-launch-profile -- --configuration Debug restore restore-tools
dotnet run --project build --configuration Release --no-launch-profile -- --configuration Debug build
set GITHUB_ACTIONS=true
procgov64 --maxmem 4G --cpu 1 --recursive "dotnet.exe" "run --project build --configuration Release --no-launch-profile -- --configuration Debug coverage"
popd
