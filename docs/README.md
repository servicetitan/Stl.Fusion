# Stl.Fusion Documentation

## 1. Get Fusion

Even though all Fusion packages are 
[available on NuGet](https://www.nuget.org/packages?q=Owner%3Aservicetitan+Tags%3Astl_fusion),
we highly recommend you to clone the repository, since it includes
the tutorial and samples.

```bash
git clone git@github.com:servicetitan/Stl.Fusion.git
```

You also need to install:
- [.NET Core SDK 3.1](https://dotnet.microsoft.com/download) 
  &ndash; to use or build Fusion. 
- [try-dotnet](https://github.com/dotnet/try/blob/master/DotNetTryLocal.md) 
  &ndash; to run the [Tutorial](tutorial/README.md).
- If you only intend to run samples, you need just 
  [docker-compose](https://docs.docker.com/compose/install/). 

## 2. Run Samples

Using IDE:
* Open the `Stl.sln` in your favorite IDE
(note that Blazor *debugging* is currently supported only in Visual Studio and VSCode though)
* Run "Stl.Samples.Blazor.Server" project
* Open http://localhost:5000 unless it didn't happen automatically.

If you prefer a CLI-only way, `cd` to the repository folder and run:

*   Windows:
    ```cmd
    dotnet build
    # The next line is optional - you need it if you want to debug Blazor client
    set ASPNETCORE_ENVIRONMENT=Development
    start "Stl.Samples.Blazor.Server" dotnet artifacts/samples/Stl.Samples.Blazor.Server/Stl.Samples.Blazor.Server.dll
    start "Samples" http://localhost:5000/
    ``` 
*   Unix:
    ```
    dotnet build
    # The next line is optional - you need it if you want to debug Blazor client
    export ASPNETCORE_ENVIRONMENT=Development
    dotnet artifacts/samples/Stl.Samples.Blazor.Server/Stl.Samples.Blazor.Server.dll
    ```
*   Finally, if you don't want to install .NET Core SDK, you can run the samples 
    in Docker:
    ```cmd
    cd docker
    docker-compose up 
    start "Samples" http://localhost:5000/
    ```

> A few other useful scripts can be found in 
> ["scripts" folder](https://github.com/servicetitan/Stl/tree/master/scripts).

## 3. Use Fusion

Overall, it's fairly simple:
* Server-side code should reference 
  [Stl.Fusion.Server](https://www.nuget.org/packages/Stl.Fusion.Server/) NuGet package
* Client-side code should reference
  [Stl.Fusion.Client](https://www.nuget.org/packages/Stl.Fusion.Client/)
  * Though if it's a Blazor client, it's a good idea to reference 
    [Stl.Fusion.Blazor](https://www.nuget.org/packages/Stl.Fusion.Blazor/) instead
* A library that could be used both by client and server should reference only 
  [Stl.Fusion](https://www.nuget.org/packages/Stl.Fusion/). 

Once it's done, you can start using it. 
Check out the [Tutorial](tutorial/README.md) to learn how.

## 4. Documentation and other materials

* [Tutorial](tutorial/README.md)
* That's it for now, but we'll definitely add more. 
  You're absolutely welcome to contribute!

## 5. Q/A

> Q: Can I contribute to the project?

Absolutely - just create your first 
[pull request](https://github.com/servicetitan/Stl/pulls) or 
[report a bug](https://github.com/servicetitan/Stl/issues).

> Q: Does ServiceTitan use Stl.Fusion now?

Yes, but not in production. We're currently using it on our internal DevPortal web site, 
which aggregates the information about all of our Kubernetes-based app instances.
The intent is to turn DevPortal into a "home page" for all of our developers, 
so it will aggregate much more useful information over time.

This is actually pretty good, taking into account the following timeline:
* End of March 2020: first lines of Stl.Fusion code were written
* Late May 2020: "Server Time" sample was added, i.e. Fusion got its 
  distributed state replication working
* Mid-June 2020: We actually started to use it on DevPortal
* July 6, 2020: This line was written :)    
  
> Q: What's the best place to ask questions related to Stl.Fusion?

[Stl.Fusion Discord Server](https://discord.gg/jpdnjM) is currently the best 
place to ask questions & track project updates. 
