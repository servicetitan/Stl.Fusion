![](docs/img/Banner.jpg)

> All project updates are published on our [Discord Server](https://discord.gg/EKEwv6d); it's also the best place for Q/A.\
> [![Build](https://github.com/servicetitan/Stl.Fusion/workflows/Build/badge.svg)](https://github.com/servicetitan/Stl.Fusion/actions?query=workflow%3A%22Build%22)
> [![NuGetVersion](https://img.shields.io/nuget/v/Stl.Fusion)](https://www.nuget.org/packages/Stl.Fusion) 

**Stl.Fusion** is .NET Core & Blazor library that attempts to dramatically
improve the way we write real-time services and UIs. If you ever dreamed 
of an abstraction that **automatically delivers every modification made to your 
server-side data to every client who uses (e.g. displays) the affected data**, 
you've just found it.


## Create Real-Time User Interfaces With Almost No Extra Code

`Stl.Fusion` is a new library for [.NET Core](https://en.wikipedia.org/wiki/.NET_Core) 
and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
providing [Knockout](https://knockoutjs.com/) / [mobX](https://mobx.js.org/) - style 
"computed observable" abstraction designed to power distributed real-time applications. 
Contrary to KO / MobX, **Fusion is designed in assumption the state it tracks is 
huge** &ndash; in fact, it's every bit of server-side data your app uses, 
including DBs, blob storages, etc., so there is no way to fit it in RAM.
But we still *can* track changes there, because **we only care about the
part of the state that is *observed* by someone**.

That's the reason Fusion uses a different pattern to provide access to this 
state &ndash; instead of providing you with a huge model that's full of 
nested "observables", it lets you to spawn and consume the parts of your 
state piece-by-piece. And you already know how to design such an API &ndash; 
any "regular" Web API providing access to some parts of server-side data
implements exactly this pattern! The only missing part is change tracking, 
and that's what Fusion provides.

If you're curious how Fusion compares to other libraries, check out:
* [How similar is Stl.Fusion to SignalR?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-signalr-e751c14b70c3?source=friends_link&sk=241d5293494e352f3db338d93c352249)
* [How similar is Stl.Fusion to Knockout / MobX?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-knockout-mobx-fcebd0bef5d5?source=friends_link&sk=a808f7c46c4d5613605f8ada732e790e)

Below is a short animation showing Fusion delivers state changes to 3 different clients 
&ndash; instances of the same Blazor app running in browser and relying on the same 
abstractions from `Stl.Fusion.dll`:

![](docs/img/Stl-Fusion-Chat-Sample.gif)

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

"Server Screen" sample captures and shares server screen in real time, and
the code there is almost identical to "Server Time" (the most straightforward 
state update example):
  
![](docs/img/Stl-Fusion-Server-Screen-Sample.gif)

## Get 10&times;&hellip;&infin; Better Performance (*)

> (*) Keep in mind a lot depends on your specific case &ndash; 
> and even though the examples presented below are absolutely real,
> they are still synthetic. That's the reason we carefully 
> put the low boundary to 10&times; rather than 10,000&times; &ndash;
> it's reasonable to expect at least 90% cache hit ratio in a vast
> majority of cases we are aiming at.

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

## So What Is Fusion?

It's a state change tracking abstraction built in assumption that **every piece of data 
you have is a part of the state / model you want to track**, and since there is 
no way to fit it in RAM, Fusion is designed to “spawn” the **observed part** of this 
state on-demand, and destroy the unused parts quickly.

It provides three key abstractions to implement this:
* **Computed services** are services exposing methods "backed" by Fusion's 
  version of "computed observables". Computed services are responsible for 
  "spawning" parts of the state on-demand.
* **Replica services** - remote proxies of "computed services".
  They allow clients to consume ("observe") the parts of remote state.
* And finally, **`IComputed<TOut>` &ndash; a "computed observable" abstraction**, 
  that's in some ways similar to the one you can find in Knockout, MobX, or Vue.js,
  but very different, if you look at its fundamental properties.
    
`IComputed<TOut>` is:
* **Thread-safe**
* **Asynchronous** &ndash; any computation of any `IComputed<TOut>` can be 
  asynchronous; Fusion APIs dependent on this feature are also asynchronous.
* **Almost immutable** &ndash; once created, the only change that may happen to it is transition 
  to `IsConsistent == false` state
* **GC-friendly** &ndash; if you know about 
  [Pure Computed Observables](https://knockoutjs.com/documentation/computed-pure.html) 
  from Knockout, you understand the problem. `IComputed` solves it even better &ndash;
  dependent-dependency relationships are explicit there, and the reference pointing
  from dependency to dependent is [weak](https://en.wikipedia.org/wiki/Weak_reference), 
  so any dependent `IComputed` is available for GC unless it's referenced by something 
  else (i.e. used).

All above make it possible to use `IComputed` on the server side &ndash; 
you don't have to synchronize access to it, you can use it everywhere, including
async functions, and you don't need to worry about GC.

But there is more &ndash; any `IComputed`:

* **Is computed just once** &ndash; when you request the same `IComputed` at the same time 
  from multiple (async) threads and it's not cached yet, just one of these threads will
  actually run the computation.  Every other async thread will await till its completion 
  and return the newly cached instance.
* **Updated on demand** &ndash; once you have an `IComputed`, you can ask for its
  consistent version at any time. If the current version is consistent, you'll get the 
  same object, otherwise you'll get a *newly computed* consistent version, 
  and every other version of it  is guaranteed to be marked inconsistent.
  At glance, it doesn't look like a useful property, but together with immutability and
  "computed just once" model, it de-couples invalidations (change notifications) 
  from updates, so ultimately, you are free to decide for how long to delay the 
  update once you know certain state is inconsistent.
* **Supports remote replicas** &ndash; any `IComputed` instance can be *published*, which allows
  any other code that knows the publication endpoint and publication ID to create
  a replica of this `IComputed` instance in their own process. Replica services mentioned
  above rely on this feature.

### Why these features are game changing?

> The ability to replicate any server-side state to any client allows client-side code 
  to build a dependent state that changes whenever any of its server-side components
  change. 
  This client-side state can be, for example, your UI model, that instantly reacts
  to the changes made not only locally, but also remotely!

> De-coupling updates from invalidation events enables such apps to scale. 
  You absolutely need the ability to control the update delay, otherwise 
  your app is expected to suffer from `O(N^2)` update rate on any 
  piece of popular content (that's both viewed and updated by a large number of users).

The last issue is well-described in 
["Why not LiveQueries?" part in "Subscriptions in GraphQL"](https://graphql.org/blog/subscriptions-in-graphql-and-relay/), 
and you may view `Stl.Fusion` as 95% automated solution for this problem:
* **It makes recomputations cheap** by caching of all the intermediates
* It de-couples updates from the invalidations to ensure 
  **any subscription has a fixed / negligible cost**.
  
If you have a post viewed by 1M users and updated with 1 KHz frequency 
(usually the frequency is proportional to the count of viewers too), 
it's 1B of update messages per second to send for your servers
assuming you try to deliver every update to every user. 
**This can't scale.** 
But if you switch to 10-second update delay, your update frequency 
drops by 10,000x to just 100K updates per second. 
Note that 10 second delay for seeing other people's updates is 
something you probably won't even notice.

`Stl.Fusion` allows you to control such delays precisely.
You may use a longer delay (10 seconds?) for components rendering
"Likes" counters, but almost instantly update comments. 
The delays can be dynamic too &ndash; the simplest example of 
behavior is instant update for any content you see that was invalidated 
right after your own action.

## Next Steps

* If above description looks too complicated for you, please check out
  [Stl.Fusion In Simple Terms](https://medium.com/@alexyakunin/stl-fusion-in-simple-terms-65b1975967ab?source=friends_link&sk=04e73e75a52768cf7c3330744a9b1e38)
* Otherwise, go to [Overview](docs/Overview.md) 
  or [Documentation Home](docs/README.md)
* Join our [Discord Server](https://discord.gg/EKEwv6d) 
  to ask questions and track project updates.
