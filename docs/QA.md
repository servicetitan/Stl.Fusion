# Q/A

## General questions

> Q: What's the best place to ask questions related to Fusion?

[Gitter Chat Room] is currently the best 
place to ask questions & track project updates. 

[![Gitter](https://badges.gitter.im/Stl-Fusion/community.svg)](https://gitter.im/Stl-Fusion/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

> Q: Can I contribute to the project?

Absolutely - just create your first 
[pull request](https://github.com/servicetitan/Stl.Fusion/pulls) or 
[report a bug](https://github.com/servicetitan/Stl.Fusion/issues).

You can also contribute to [Fusion Samples].

> Q: What "Stl" stands for?

It's an acronym for "ServiceTitan Library". We certainly know about "STL" 
in C++, but nothing similar is widely used on .NET, so we ended up taking it :)

## Stability and production use

> Q: How stable is Fusion?

It looks fairly stable on samples and one of our internal apps 
(~ runs for days without any issues for about a hundred users), 
but its test coverage is fairly low (sorry, **the actual coverage is about 55%** &ndash; the reports in CodeCov are broken now, we'll fix this soon):

[![Build](https://github.com/servicetitan/Stl.Fusion/workflows/Build/badge.svg)](https://github.com/servicetitan/Stl.Fusion/actions?query=workflow%3A%22Build%22)
[![NuGetVersion](https://img.shields.io/nuget/v/Stl.Fusion)](https://www.nuget.org/packages?q=Owner%3Aservicetitan+Tags%3Astl_fusion) 
[![codecov](https://codecov.io/gh/servicetitan/Stl.Fusion/branch/master/graph/badge.svg)](https://codecov.io/gh/servicetitan/Stl.Fusion)

So we definitely need more tests and more use cases to claim it's ready for production use.
But if you love the concept, it's probably not the reason to wait till the moment
we check all the boxes.

What's fair to claim is that Fusion is ready for prototyping & internal use; 
rapid prototyping of real-time UIs is another good use case for it right now.
Note that you can save a fair amount of time by having much simpler invalidation
logic in such prototypes (i.e. invalidating the data much more broadly), 
assuming you tweak it to a more robust one later.

> Q: Does ServiceTitan use Fusion now?

Yes, but not in production. We're currently using it on our internal DevPortal web site, 
which aggregates the information about all of our Kubernetes-based app instances.
The intent is to turn DevPortal into a "home page" for all of our developers, 
so it will aggregate much more useful information over time.

This is actually pretty good, taking into account the following timeline:
* End of March 2020: first lines of Fusion code were written
* Late May 2020: "Server Time" sample was added, i.e. Fusion got its 
  distributed state replication working
* Mid-June 2020: We actually started to use it on DevPortal
* July 6, 2020: This line was written :)    

## Comparison to other libraries

* [How similar is Fusion to SignalR?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-signalr-e751c14b70c3?source=friends_link&sk=241d5293494e352f3db338d93c352249)
* [How similar is Fusion to Knockout / MobX?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-knockout-mobx-fcebd0bef5d5?source=friends_link&sk=a808f7c46c4d5613605f8ada732e790e)

## Possible use cases, pros and cons

> Q: Can I use Fusion with server-side Blazor?

Yes, you can use it to implement the same real-time update logic there. 
The only difference here is that you don't need API controllers supporting
Fusion publication in this case, i.e. your models might depend right on the 
*server-side compute services* (that's an abstraction you primarily deal with, 
that "hides" all the complexities of dealing with `IComputed` 
and does it transparently for you).

> Q: Can I use Fusion *without* Blazor at all?

The answer is yes &ndash; you can use Fusion in all kinds of .NET Core 
apps, though I guess the real question is:

> Q: Can I use Fusion with some native JavaScript client for it?

Right now there is no native JavaScript client for Fusion, so if you
want to use Fusion subscriptions / auto-update features in JS,
you still need a counterpart in Blazor that e.g. exports the "live state" 
maintained by Fusion to the JavaScript part of the app after every update.

There is a good chance we (or someone else) will develop a native 
JavaScript client for Fusion in future.

> Q: Are there any benefits of using Fusion on server-side only?

Yes. Any service backed by Fusion, in fact, gets a cache, that invalidates 
right when it should. This makes % of inconsistent reads there is as small
as possible. 

Which is why Fusion is also a very good fit for caching scenarios requiring
nearly real-time invalidation / minimum % of inconsistent reads.

## API related questions

TBD.

[Fusion Discord Server]: https://discord.gg/EKEwv6d
[Fusion Samples]: https://github.com/servicetitan/Stl.Fusion.Samples

[Gitter]: https://gitter.im/Stl-Fusion/community
[Gitter Chat Room]: https://gitter.im/Stl-Fusion/community
[Discord Server]: https://discord.gg/EKEwv6d
[Fusion Feedback Form]: https://forms.gle/TpGkmTZttukhDMRB6
