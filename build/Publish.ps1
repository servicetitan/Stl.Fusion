function Rebuild() {
    Remove-Item -Recurse artifacts -ErrorAction Ignore
    dotnet build -c Release
    dotnet pack -c Release --no-restore
}

function Publish($name) {
    $nupkgVersion = nbgv get-version -v NuGetPackageVersion
    dotnet nuget push "artifacts/nupkg/$name.$nupkgVersion.nupkg" -k $env:NUGET_ORG_API_KEY -s https://api.nuget.org/v3/index.json
    dotnet nuget push "artifacts/nupkg/$name.$nupkgVersion.nupkg" -k $env:MYGET_API_KEY -s $env:MYGET_FEED
}

function PublishAll() {
    foreach ($package in Get-ChildItem "artifacts/nupkg/*.nupkg" ) {
        dotnet nuget push $package -k $env:NUGET_ORG_API_KEY -s https://api.nuget.org/v3/index.json
        dotnet nuget push $package -k $env:MYGET_API_KEY -s $env:MYGET_FEED
    }
}

Push-Location ".."
Rebuild
PublishAll
Pop-Location
