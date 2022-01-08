@set PUBLIC_BUILD=1
@dotnet run --project Build.csproj --configuration Release --no-launch-profile -- --configuration Release publish
