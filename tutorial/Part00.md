# Part 0: NuGet packages  

Overall, it's fairly simple:
* Server-side code should reference `Stl.Fusion.Server` 
* Client-side code should reference `Stl.Fusion.Client`
  * Though if it's a Blazor client, it's a good idea to reference `Stl.Fusion.Blazor` instead 
* A library that could be used both by client and server should reference only `Stl.Fusion` 

The full list of Fusion packages:
* [Stl](https://www.nuget.org/packages/Stl/) - 
  depends on [Castle.Core](https://www.nuget.org/packages/Castle.Core/) & maybe some other
  third-party packages. 
  "Stl" stands for "ServiceTitan Library" (we know, we know ☺) - it's a collection of relatively 
  isolated abstractions and methods we couldn't find in BCL.
* [Stl.Fusion](https://www.nuget.org/packages/Stl.Fusion/) - depends on `Stl`. 
  Nearly everything related to Fusion is there.
* [Stl.Fusion.Server](https://www.nuget.org/packages/Stl.Fusion.Server/) - depends on `Stl.Fusion`.
  It implements server-side WebSocket endpoint allowing client-side counterpart to communicate 
  with Fusion `Publisher`. In addition, it provides a base class for fusion API controllers 
  (`FusionController`) and a few extension methods helping to register all of that in your web app.
* [Stl.Fusion.Client](https://www.nuget.org/packages/Stl.Fusion.Client/) - depends on `Stl.Fusion`.
  Implements a client-side WebSocket communication channel and 
  [RestEase](https://github.com/canton7/RestEase) - based API client builder compatible with
  `FusionControler`-based API endpoints. All of that together allows you to get computed
  instances on the client that "mirror" their server-side counterparts.
* [Stl.Fusion.Blazor](https://www.nuget.org/packages/Stl.Fusion.Blazor/) - depends on `Stl.Fusion.Client`.
  Currently there are just two types - `LiveComponentBase<TState>` and 
  `LiveComponentBase<TLocal, TState>`. These are base classes for your own Blazor components 
  capable of updating their state in real time relying on `ILiveState`. 

#### [Next: Part 1 &raquo;](./Part01.md) | [Tutorial Home](./README.md)
