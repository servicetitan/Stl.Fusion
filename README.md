# Stl.Fusion

### Two-sentence description:
* **Craft a real-time UI by** adding ~1 extra line of code per every "update" endpoint
* **Get 1,000%&hellip;∞ speedup** for your API with auto-invalidating cache.

### A longer version:

`Stl.Fusion` is a new library for [.NET Core](https://en.wikipedia.org/wiki/.NET_Core) 
and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
providing [Knockout.js](https://knockoutjs.com/) 
/ [mobX](https://mobx.js.org/) - style computed/observable abstractions,
**but designed to power distributed apps** as well as client-side UIs.

The version of "computed/observable state" it provides is:
* **Fully thread-safe**
* **Built for asynchronous world** &ndash; any computation of any `IComputed<TOut>` can be 
  asynchronous, as well as all of Stl.Fusion APIs that may invoke async computations.   
* **Almost immutable** &ndash; once created, the only change that may happen to it is transition 
  to `IsConsistent == false` state
* **Always consistent** &ndash; once you have some `IComputed`, you can ask for its
  consistent version at any time. If the current version is consistent, you'll get the same 
  object, otherwise you'll get a newly computed consisntent one, and every other version of it 
  is guaranteed to be marked inconsistent.
* **Supports remote replicas** &ndash; any `IComputed` instance can be *published*, which allows
  any other code that knows the publication endpoint and publication ID to create
  a replica of this `IComputed` instance in their own process. 
  
The last part is crucial: 
* The ability to replicate any server-side state to any client allows client-side code 
  to build a dependent state that changes whenever any of its server-side components
  change.
* This client-side state can be, for example, your UI model, that instantly reacts
  to the changes made not only locally, but also remotely!

This is what makes `Stl.Fusion` a great fit for real-time apps: it becomes the only abstraction
such apps need, since anything else can be easily described as a state change. For example,
if you build a chat app, you don't need to worry about delivering every message to every client
anymore. What you want to have is an API endpoint allowing chat clients to get a replica
of server-side `IComputed` instance that "stores" the chat tail. Once a message gets posted to some 
channel, its chat tail gets invalidated, and every client will automatically "pull" the updated 
tail.

A short animation showing Fusion delivers state changes to 3 different clients:

![](docs/img/Stl-Fusion-Chat-Sample.gif)

The "Samples" app is a client-side [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
application running in browser and relying on the same abstractions from `Stl.Fusion.dll` 
that are used on the server-side too.

Note that "Composition" sample shown in a separate window in the bottom-right corner
also properly updates its page - in particular, it captures the last chat message. It's
actually the most interesting example there, since it "composes" the final state (its UI model)
by two different ways: 
* One is 
  [composed on the server side](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Server/Services/ServerSideComposerService.cs);
  its replica is published to all the clients
* And another one is 
  [composed completely on the client](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/Services/ClientSideComposerService.cs) 
  by combining other server-side replicas.
* **The surprising part:** notice two above files are almost identical!

And here is **literally** all the client-side code powering Chat sample:
* [ChatState](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/UI/ChatState.cs) 
  &ndash; the view model
* [Chat.razor](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/Pages/Chat.razor) 
  &ndash; the view
* [IChatClient in Clients.cs](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/Services/Clients.cs#L19) 
  &ndash; the client (the actual client is generated in the runtime).  
 
Another interesting sample there is "Server Screen", which shows a timeout-based state update.
If the "state" is the image of your screen, the result is:
  
![](docs/img/Stl-Fusion-Server-Screen-Sample.gif)

Obviously, the use cases aren't limited to client-side Blazor UIs. You can also use Fusion in:
* **Server-side Blazor apps** &ndash; to implement the same real-time update
  logic. The only difference here is that you don't need API controllers supporting
  Fusion publication in this case, i.e. your models might depend right on the 
  *server-side computed services* (that's an abstraction you primarily deal with, that
  "hides" all the complexities of dealing with `IComputed` & does it transparently
  for you).
* **JavaScript-based UIs (e.g. React-based)** &ndash; right now this implies you still 
  need a counterpart on Blazor that exports the "live state" it builds on the client 
  to JavaScript part of the app, but it's quite likely we'll have a native JS client 
  for Stl.Fusion in future. 
* **Server-side only** &ndash; you'll learn that any service backed by Fusion, in fact,
  gets a cache, that invalidates right when it should, so % of inconsistent reads 
  there is as tiny as possible. Which is why it is a perfect fit for scenarios where
  having a cache is crucial.

> Note that the library is quite new, so we expect to see more interesting cases 
involving it in future.

[One of tests in Stl.Fusion test suite](https://github.com/servicetitan/Stl.Fusion/blob/master/tests/Stl.Tests/Fusion/PerformanceTest.cs) 
benchmarks "raw" [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - 
based Data Access Layer (DAL) against its version relying on Fusion. 
Both tests run almost identical code - in fact, the only difference there is that Fusion
version of test uses a Fusion-provided proxy wrapping the 
[`UserService`](https://github.com/servicetitan/Stl.Fusion/blob/master/tests/Stl.Tests/Fusion/Services/UserService.cs)
(the DAL used in this test) instead of the actual type.

![](docs/img/Performance.gif)

The speed difference is quite impressive:
* ~31,500x speedup with [Sqlite](https://www.sqlite.org/index.html) EF Core provider
* ~1,000x speedup with 
  [In-memory EF Core provider](https://docs.microsoft.com/en-us/ef/core/providers/in-memory/?tabs=dotnet-core-cli)  

Obviously, you're expected to get a huge performance boost in any scenario involving
local caching, but note that here you get it almost for free in terms of extra code, 
and moreover, you get an *almost* always consistent cache. In reality, it's still 
an *eventually consistent* cache, but with extremelly short inconsistency periods per
cache entry.

That was a quick intro - thanks for reading it!
Check out [Stl.Fusion Documentation](docs/README.md) and join
our [Discord Server](https://discord.gg/EKEwv6d) to ask questions and
track project updates.
