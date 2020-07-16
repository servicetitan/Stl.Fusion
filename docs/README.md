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

## 3. Learn Fusion

* [Overview](Overview.md) is the best place to start. 
  It describes what Stl.Fusion is on conceptual level
  and explains the most tricky concepts on relatively simple
  examples.
* [Tutorial](tutorial/README.md) &ndash; it's not fully finished yet,
  but the best part is: it is interactive, so any code you see
  there is runnable with [try-dotnet](https://github.com/dotnet/try/blob/master/DotNetTryLocal.md)!
* [The Story Behind Stl.Fusion](Story.md) &ndash; maybe you'll find
  it interesting too.
* Join our [Discord Server](https://discord.gg/EKEwv6d) 
  to ask questions and track project updates
* Check out [Q/A](QA.md) to get answers to frequent questions.
  
That's it for now, but we'll definitely add more over time. 

## 4. Use Fusion

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


## Credits

* [Knockout](https://knockoutjs.com/) by 
  [Steve Sanderson](http://blog.stevensanderson.com/) &ndash; 
  for making "computed observable" abstraction popular 
  (and likely, inventing it)
* [Quora](https://www.quora.com/) — a huge part of the inspiration for Stl.Fusion was Quora's LiveNode framework
* [ServiceTitan](https://www.servicetitan.com/) - for giving some of us
  an opportunity to work on this project
* All other contributors. For now it is
  [Vladimir Chirikov](https://github.com/vchirikov) &ndash;
  everything related to build system is written by him.
  But everyone is welcome to join &ndash; 
  [your pull request](https://github.com/servicetitan/Stl/pulls) 
  is all we need!
