![](docs/img/Banner.jpg)

[![Build](https://github.com/servicetitan/Stl.Fusion/workflows/Build/badge.svg)](https://github.com/servicetitan/Stl.Fusion/actions?query=workflow%3A%22Build%22)

> Have you ever dreamed of an abstraction that magically delivers
  every change made to you server-side data to every client that displays it?

> Have you thought of a caching API that automatically evicts a 
  cached entry right at the moment it becomes inconsistent with the
  ground truth?

`Stl.Fusion` solves both these problems &ndash; 
moreover, it does this almost transparently for you, so most of your code won't even change!
And yes, it is scalable and crafted for performance.

If this sounds interesting, skip the marketing part below and go straight
to the [Overview](docs/Overview.md).

## Create Real-Time User Interfaces With Almost No Extra Code (*)

> (*) Lika Tesla's Autopilot, Fusion can't solve the problem without your help &ndash;
> but similarly, it takes care of 90% of it and reduces the amount of extra code you need to 
> write to a tiny fraction of what's reasonable to expect otherwise.

`Stl.Fusion` is a new library for [.NET Core](https://en.wikipedia.org/wiki/.NET_Core) 
and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
providing [Knockout](https://knockoutjs.com/) / [mobX](https://mobx.js.org/) - style 
"computed observable" abstraction **designed to power distributed applications**. 
It works on the client, server, and even connects them together!

Here is a short animation showing Fusion delivers state changes to 3 different clients 
&ndash; instances of the same Blazor app running in browser and relying on the same 
abstractions from `Stl.Fusion.dll`:

![](docs/img/Stl-Fusion-Chat-Sample.gif)

"Server Screen" sample captures and shares server screen in real time, and
the code there is almost identical to "Server Time" (the most straightforward 
state update example):
  
![](docs/img/Stl-Fusion-Server-Screen-Sample.gif)

Fusion is built on three "pillars":
* **Computed services** - services that expose methods "backed" by Fusion's 
  version of "computed observables"
* **Replica services** - remote proxies of "computed services". 
  Replicas are quite simple to define: they are, in fact, just interfaces.
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

## Stl.Fusion is the only communication library your real-time app needs

Imagine you're building a real-time chat app on .NET Core and Blazor,
and [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr)
is the library you want to use to deliver real-time updates to every client.

Here is how a typical "change-to-update" sequence involving SignalR looks like:
* One of clients makes an API call to post a new message `M` to channel `C`. 
  This could be done via SignalR as well, but SignalR usage here isn't 
  quite necessary here - any Web API allows to do the same nearly as efficiently
  (~ except some extra payload like headers & cookies).
* Server persists the message to the DB and responds with "Ok" to the 
  original client. If it wouldn't be a real-time chat, that's 
  where we would stop.
* But a real-time app has to do more &ndash; now server has to notify other 
  clients about this change. Initially this looks like a simple problem
  *(just broadcast the  message to everyone connected)*, but it turns into
  a much more complex one if you want your chat to *scale*:
  * Ideally, you want notify just the clients that are interested
    in this update &ndash; the ones keeping this channel open 
    (so they see the chat tail), "pinned" (so they see the number 
    of unread messages), or maybe looking at the settings page
    of this channel, which also displays the number of unread messages.
  * SignalR's 
    ["Groups" feature](https://docs.microsoft.com/en-us/aspnet/signalr/overview/guide-to-the-api/working-with-groups) is designed to address almost
    exactly such cases. 
    All you need is to dynamically add/remove users to a group that corresponds 
    to channel `C`. Once you have this, you can broadcast a message to this group.
  * But if you think what's required to implement this "dynamic add/remove" behavior,
    you may quickly conclude it's totally not as easy as it seems, because
    *you have to do this based on the client-side UI state* &ndash;
    in other words, now you have to notify server each time client
    opens or closes settings page of channel `C` to let it decide
    whether the client still has to be a part of "channel `C` group" or not,
    and so on!
  * Worse, SignalR state (groups, etc.) doesn't survive restarts, so 
    if your client reconnects to same or another server (and typically
    you want to retain the UI state on reconnection), the first thing 
    that has to happen is figuring out how its UI state maps to SingalR 
    channels it has to be subscribed to.
  * **The gist:** even this part is complex.
* And that's not the end: once you received the message on the client, 
  you have to apply it to the UI state there, which typically means 
  to dispatch through a set of reducers (if you use Redux) so that some
  of them will apply it to the corresponding part of the UI state,
  which in turn will make related UI components to re-render.

**But if you think what you really achieved by doing all of this**, you'll 
quickly conclude the only end result of above actions is that
client-side state of every client became consistent with the server-side state!

In other words, SignalR, messaging, etc. &ndash; all of this isn't essential
to the problem you were solving. **Your problem was to "replicate" the
state change happened on the server to every client that's interested
in this part of server's state.**

Stl.Fusion was designed to solve exactly this problem. Instead of making you 
to dynamically subscribe/unsubscribe every client to a fair number of SignalR 
channels, it:
  * Makes your server-side code to track dependencies between the pieces
    of data produced and used by every service.
  * Tracks remote replicas of every piece of such data consumed by clients
  * And once one of such pieces of data changes, it notifies every of its
    direct and indirect dependencies about this, including the remote ones.
  * This allows clients to refresh the data that's changed &ndash; 
    either immediately or later.

> If above description looks too complicated, please check out 
  [Stl.Fusion In Simple Terms](https://medium.com/@alexyakunin/stl-fusion-in-simple-terms-65b1975967ab?source=friends_link&sk=04e73e75a52768cf7c3330744a9b1e38).

This makes Fusion the only client-to-server notification library 
most of real-time apps need:
having an ability to send just a single type of notification
("the value X you requested earlier is now obsolete") is enough 
assuming it's not a problem to get the actual update later.

Finally, let's look again at the first animation:

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

And here is **literally** all the client-side code powering Chat sample:
* [ChatState](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/UI/ChatState.cs) 
  &ndash; the view model
* [Chat.razor](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/Pages/Chat.razor) 
  &ndash; the view
* [IChatClient in Clients.cs](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/Services/Clients.cs#L19) 
  &ndash; the client (the actual client is generated in the runtime).  

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

## Next Steps

* If you are not a developer, but still want to understand what it's all about, 
  [Stl.Fusion In Simple Terms](https://medium.com/@alexyakunin/stl-fusion-in-simple-terms-65b1975967ab?source=friends_link&sk=04e73e75a52768cf7c3330744a9b1e38)
* Otherwise, go to [Overview](docs/Overview.md) 
  or [Documentation Home](docs/README.md)
* Join our [Discord Server](https://discord.gg/EKEwv6d) 
  to ask questions and track project updates.
