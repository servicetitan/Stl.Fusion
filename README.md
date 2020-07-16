![](docs/img/Banner.jpg)

[![Build](https://github.com/servicetitan/Stl.Fusion/workflows/Build/badge.svg)](https://github.com/servicetitan/Stl.Fusion/actions?query=workflow%3A%22Build%22)
[![codecov](https://codecov.io/gh/servicetitan/Stl.Fusion/branch/master/graph/badge.svg)](https://codecov.io/gh/servicetitan/Stl.Fusion)


> Have you ever dreamed of an abstraction that magically delivers
  every change made to you server-side data to every client that displays it?

> Have you thought of a caching API that automatically evicts a 
  cached entry right at the moment it becomes inconsistent with the
  ground truth?

`Stl.Fusion` is an abstraction that solves both these problems &ndash; morever,
it does it mostly transparently for you, so most of your code won't even change!
And yes, it is scalable and crafted for performance.

If this sounds interesting, skip the marketing part below and go straight
to the [Overview](docs/Overview.md).

## A Longer Description 

`Stl.Fusion` is a new library for [.NET Core](https://en.wikipedia.org/wiki/.NET_Core) 
and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
providing [Knockout.js](https://knockoutjs.com/) 
/ [mobX](https://mobx.js.org/) - style computed/observable abstractions,
**but designed to power distributed apps** as well as client-side UIs.

The version of "computed/observable state" it provides is:
* **Thread-safe**, so you don't have to synchronize access to it
* **Asynchronous** &ndash; any computation of any `IComputed<TOut>` can be 
  asynchronous, as well as all of Stl.Fusion APIs that may invoke async computations.   
* **Almost immutable** &ndash; once created, the only change that may happen to it is transition 
  to `IsConsistent == false` state
* **Computed just once** &ndash; when you request the same `IComputed` at the same time 
  from multiple (async) threads and it's not cached yet, just one of these threads will
  actually run the computation.  Every other async thread will await till its completion 
  and return the newly cached instance.
* **Updated on demand** &ndash; once you have an `IComputed`, you can ask for its
  consistent version at any time. If the current version is consistent, you'll get the 
  same object, otherwise you'll get a newly computed consisntent version, 
  and every other version of it  is guaranteed to be marked inconsistent.
  At glance, it doesn't look like a useful property, but together with immutability and
  "computed just once" model, it de-couples invalidations (change notifications) 
  from updates, so ultimately, you are free to decide for how long to delay the 
  update once you know certain state is inconsistent.
* **Supports remote replicas** &ndash; any `IComputed` instance can be *published*, which allows
  any other code that knows the publication endpoint and publication ID to create
  a replica of this `IComputed` instance in their own process. 
  
The last points are crucial:
* The ability to replicate any server-side state to any client allows client-side code 
  to build a dependent state that changes whenever any of its server-side components
  change.
* This client-side state can be, for example, your UI model, that instantly reacts
  to the changes made not only locally, but also remotely!
* De-coupling updates from invalidation events enables such apps to scale. 
  You absolutely need the ability to control the update delay, otherwise 
  your app is expected to suffer from `O(N^2)` update rate on any 
  piece of popular content (that's both viewed and updated by a large number of users).

The issue is well-described in 
["Why not LiveQueries?" part in "Subscriptions in GraphQL"](https://graphql.org/blog/subscriptions-in-graphql-and-relay/), and you may view `Stl.Fusion` 
as 95% automated solution for the this problem:
* **It makes recomputations cheap** by caching of all the intermediates
* It de-couples updates from invalidations to ensure 
  **any subscription costs ~ nothing**.

"Nothing" means that the cost of subscription is fixed relatively to the 
cost of some prior operation, because always a single invalidation following
either the "intiaal subscribe" or "update" action. 
And since you can control the delay between the invalidation and the update, 
you can throttle the update rate as much as you need.
  
> If you have a post viewed by 1M users and updated with 1 KHz frequency 
  (usually the frequency is proportional to the count of viewers too), 
  it's 1B of update messages per second to send for your servers
  assuming you try to deliver every update to every user. 
  In other words, **this can't scale**.
  
> But if you switch to 1-second update delay, your update frequency 
  drops to "just" 1M updates per second. That's still a lot, of course 
  but already 1000x better - and note that 1 second delay for 
  seeing other people's updates is something you won't even notice! 
  `Stl.Fusion` allows to control such delay quite precisely. 
  You may use a larger delay (10 seconds?) for e.g. "Likes" counters, 
  but almost instantly update comments. 
  The delays can be dynamic too &ndash; the simplest example of 
  behavior is instant update for any content you see that was invalidated 
  right after your own action.

All of this makes `Stl.Fusion` the only abstraction real-time apps need:
**any notification can be described as a state change**. 

> For example, if you build a chat app, you don't need to worry about delivering 
  every message to every client anymore. What you want to have is an API endpoint 
  allowing chat clients to get a replica of server-side `IComputed` instance that 
  "stores" the chat tail. Once a message gets posted to some channel, its chat tail 
  gets invalidated, and every client will automatically "pull" the updated tail.

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

### Next Steps

* Check out the [Overview](docs/Overview.md)
  or go to [Documentation Home](docs/README.md)
* Join our [Discord Server](https://discord.gg/EKEwv6d) 
  to ask questions and track project updates.
