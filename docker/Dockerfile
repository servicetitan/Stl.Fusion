# Create a base image with code and restored NuGet packages
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster as base
WORKDIR /src

COPY ["src/", "src/"]
COPY ["samples/", "samples/"]
COPY ["tests/", "tests/"]
COPY ["docs/", "docs/"]
COPY ["build/", "build/"]
COPY ["scripts/", "scripts/"]

COPY Stl.Fusion.sln .
COPY Directory.Build.props .
COPY Directory.Build.targets .
COPY Packages.props .
COPY version.json .

# Collect application artifacts
FROM base as build
RUN dotnet build

# Create runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.1-alpine as runtime
RUN apk add icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /samples
COPY --from=build /src/artifacts/samples .
WORKDIR /samples/Stl.Samples.Blazor.Server
ENTRYPOINT ["dotnet", "Stl.Samples.Blazor.Server.dll"]
