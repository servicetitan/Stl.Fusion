# Stl.Fusion

`Stl.Fusion` is a new library for [.NET Core](https://en.wikipedia.org/wiki/.NET_Core) 
and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
providing [Knockout.js](https://knockoutjs.com/) 
/ [mobX](https://mobx.js.org/) - style computed/observable abstractions,
**but designed to power distributed apps** rather than client-side user interfaces.

The version of "computed/observable state" it provides is:
* **Fully thread-safe**
* **Built for asynchronous world** &ndash; any computation of any `IComputed<TOut>` can be 
  asynchronous, as well as all of Stl.Fusion APIs that may invoke async computations.   
* **Almost immutable** &ndash; once created, the only change that may happen to it is transition 
  to `Invalidated` state
* **Always "renewable"** &ndash; once you have some `IComputed<TOut>`, you can always ask for its
  most up to date (consistent) version. Every other version of it is guaranteed to be
  in invalidated state.
* **Supports remote replicas** &ndash; any computed instance can be *published*, which allows
  any other code that knows the publication endpoint and publication ID to create
  a replica of this computed instance in their own process. 
  
The last part is crucial: 
* The ability to replicate any server-side state to any client allows client-side code 
  to build a dependent state that changes whenever any of its server-side components
  change.
* This client-side state can be, for example, your UI model, that instantly reacts
  to the changes made not only locally, but also remotely!

This is what makes `Stl.Fusion` a great fit for real-time apps: it becomes the only abstraction
such apps need, since anything else can be easily described as a state change. For example,
if you build a chat app, you don't need to worry about delivering every message to every client
anymore. What you want to have is an endpoint that allows to get your clients a replica
of server-side computed that "stores" chat tail. Once a message gets posted to some channel, 
its chat tail gets invalidated, and the clients will automatically "pull" the updated tail.    

That's how Fusion-based Chat sample reacts to user interaction:


One other fancy sample there is "Server Screen", which literally sends screenshots captured
on server side in real-time to every client visiting it:
  
 
Note that this is *client-side Blazor app*, the real-time changes it displays are
delivered there via WebSocket channel backing computed replicas. 

There is **no single line of JavaScript code**, and below is **literally** all the 
client-side code powering Chat sample:
* [ChatState](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/UI/ChatState.cs) 
  &ndash; the view model
* [Chat.razor](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/Pages/Chat.razor) 
  &ndash; the view
* [IChatClient in Clients.cs](https://github.com/servicetitan/Stl/blob/master/samples/Stl.Samples.Blazor.Client/Services/Clients.cs#L19) 
  &ndash; the client (the actual client is generated in the runtime).  
 
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
  
> Note that the library is quite new, so we expect to see more interesting cases involving it 
in future.

That was a quick intro - thanks for reading it!
Check out [Stl.Fusion Documentation](docs/README.md) to learn more.  
