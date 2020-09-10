# Stl.Fusion Documentation

## Samples and Tutorial

1\. Clone [Stl.Fusion.Samples repository](https://github.com/servicetitan/Stl.Fusion.Samples):
```
git clone git@github.com:servicetitan/Stl.Fusion.Samples.git
```

2\. Follow the instructions in its
[README.md](https://github.com/servicetitan/Stl.Fusion.Samples/blob/master/README.md)
to build and run everything.

## Documentation

* [Overview] describes the fundamentals and key concepts of Stl.Fusion.
* [Tutorial] covers all key abstractions Stl.Fusion has and provides
  a fair number of code examples helping to understand them.
  Although you can simply browse it, you can also run and modify any
  C# code featured here. All you need is
  [Try .NET](https://github.com/dotnet/try/blob/master/DotNetTryLocal.md)
  or [Docker](https://www.docker.com/).  
* [The Story Behind Stl.Fusion](Story.md) &ndash; maybe you'll find
  it interesting too.
* [Q/A](QA.md) answers the most frequent questions; 
  join our [Discord Server](https://discord.gg/EKEwv6d) to ask yours.

Videos:
* [Stl.Fusion Tutorial Videos](https://www.youtube.com/playlist?list=PLKM0mLUUiLWHsvS6eOLb3IlhMiL9y3X_Z) &ndash;
  a playlist with videos from the [Tutorial].
* [Modern Real-Time Apps With Stl.Fusion + Blazor: Intro + Samples Overview](https://youtu.be/jYVe5yd0xuQ)
  Sorry in advance: the video is long, it implies you already played with Blazor, 
  and finally, the commenter there clearly needs more practice :/ 
  On a bright side, likely it will still save you more time than 
  you'll spend on it.
  **Check out its description - there is TOC + links to interesting parts.**
* More videos are upcoming.

Posts:
* [Stl.Fusion In Simple Terms](https://medium.com/@alexyakunin/stl-fusion-in-simple-terms-65b1975967ab?source=friends_link&sk=04e73e75a52768cf7c3330744a9b1e38) &ndash;
  so far the best description of what Fusion is for non-developers
* [How similar is Stl.Fusion to SignalR?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-signalr-e751c14b70c3?source=friends_link&sk=241d5293494e352f3db338d93c352249)
* [How similar is Stl.Fusion to Knockout / MobX?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-knockout-mobx-fcebd0bef5d5?source=friends_link&sk=a808f7c46c4d5613605f8ada732e790e)

Join our [Discord Server](https://discord.gg/EKEwv6d) 
to ask questions and track project updates.


## Credits

[ServiceTitan](https://www.servicetitan.com/) &ndash; for giving some of us
an opportunity to work on this project.

Contributors:
* [Vladimir Chirikov](https://github.com/vchirikov) &ndash; build system & misc. fixes
* [Alexey Ananyev](https://github.com/hypercodeplace) &ndash; misc. fixes
* [Alexey Golub](https://github.com/Tyrrrz) &ndash; minor fixes; FYI we use his 
  [CliWrap](https://github.com/Tyrrrz/CliWrap) in our build pipeline, and his
  [CliFx](https://github.com/Tyrrrz/CliFx) is pretty amazing too!
* [Alex Yakunin](https://github.com/alexyakunin) ([Medium](https://medium.com/@alexyakunin)) &ndash; 
  the creator of Stl.Fusion.

Indirect contributors & everyone else who made Stl.Fusion possible:
* [Steve Sanderson](http://blog.stevensanderson.com/) &ndash; 
  for both [Knockout](https://knockoutjs.com/), which made "computed observable" abstraction popular, 
  and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) &ndash;
  yeah, Steve is the creator of it as well!
* [Quora](https://www.quora.com/) — a huge part of the inspiration for Stl.Fusion was Quora's LiveNode framework
* [Microsoft](microsoft.com) &ndash; for .NET Core and Blazor.
* The authors and maintainers of every library used by `Stl.Fusion`. Most notably,
  [DynamicProxy from Castle.Core](http://www.castleproject.org/projects/dynamicproxy/),
  [RestEase](https://github.com/canton7/RestEase), and 
  [Json.NET](https://www.newtonsoft.com/json).

[Overview]: Overview.md
[Tutorial]: https://github.com/servicetitan/Stl.Fusion.Samples/blob/master/docs/tutorial/README.md
