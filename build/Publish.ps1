
function Rebuild() {
    Remove-Item -Recurse artifacts -ErrorAction Ignore
    dotnet build -c Release
    dotnet pack -c Release --no-restore
}

function Publish($name) {
    $nupkgVersion = nbgv get-version -v NuGetPackageVersion
    dotnet nuget push "artifacts/nupkg/$name.$nupkgVersion.nupkg" -k $env:NUGET_ORG_API_KEY -s https://api.nuget.org/v3/index.json
}

function PublishAll() {
    dotnet nuget push "artifacts/nupkg/*.nupkg" -k $env:NUGET_ORG_API_KEY -s https://api.nuget.org/v3/index.json
}

Push-Location ".."
Rebuild
PublishAll
Pop-Location
