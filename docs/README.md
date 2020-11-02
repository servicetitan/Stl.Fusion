# Fusion Documentation

## Samples and Tutorial

1\. Clone [Fusion Samples] repository:
```
git clone git@github.com:servicetitan/Stl.Fusion.Samples.git
```

2\. Follow the instructions in its
[README.md](https://github.com/servicetitan/Stl.Fusion.Samples/blob/master/README.md)
to build and run everything.

## Documentation

* [Overview] describes the fundamentals and key concepts.
* [Tutorial] covers key abstractions and code examples helping to learn them.
  Although you can simply browse it, you can also run and modify any
  C# code featured here. All you need is
  [Try .NET](https://github.com/dotnet/try/blob/master/DotNetTryLocal.md)
  or [Docker](https://www.docker.com/).  
* [The Story Behind Fusion](Story.md) &ndash; maybe you'll find
  it interesting too.
* [Q/A](QA.md) answers some of the most frequent questions.


Videos:
* [Tutorial Videos](https://www.youtube.com/playlist?list=PLKM0mLUUiLWHsvS6eOLb3IlhMiL9y3X_Z) &ndash;
  a playlist with videos from the [Tutorial].
* [Modern Real-Time Apps With Fusion + Blazor: Intro + Samples Overview](https://youtu.be/jYVe5yd0xuQ)
  Sorry in advance: the video is long, it implies you already played with Blazor, 
  and finally, the commenter there clearly needs more practice :/ 
  On a bright side, likely it will still save you more time than 
  you'll spend on it.
  **Check out its description - there is TOC + links to interesting parts.**
* More videos are upcoming.

Posts:
* [Fusion In Simple Terms](https://medium.com/@alexyakunin/stl-fusion-in-simple-terms-65b1975967ab?source=friends_link&sk=04e73e75a52768cf7c3330744a9b1e38) &ndash;
  so far the best description of what Fusion is for non-developers
* [How similar is Fusion to SignalR?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-signalr-e751c14b70c3?source=friends_link&sk=241d5293494e352f3db338d93c352249)
* [How similar is Fusion to Knockout / MobX?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-knockout-mobx-fcebd0bef5d5?source=friends_link&sk=a808f7c46c4d5613605f8ada732e790e)

Please remember that you can always ask for help:
* [Discord Server] &ndash; <a href="https://discord.gg/EKEwv6d">
  <img valign="middle"  src="https://img.shields.io/discord/729970863419424788.svg" alt="Discord Server">
  </a>
* [Gitter] &ndash; <a href="https://gitter.im/Stl-Fusion/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge">
  <img valign="middle"  src="https://badges.gitter.im/Stl-Fusion/community.svg" alt="Gitter">
  </a>

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
  the creator of Fusion.

Indirect contributors & everyone else who made Fusion possible:
* [Steve Sanderson](http://blog.stevensanderson.com/) &ndash; 
  for both [Knockout](https://knockoutjs.com/), which made "computed observable" abstraction popular, 
  and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) &ndash;
  yeah, Steve is the creator of it as well!
* [Quora](https://www.quora.com/) — a huge part of the inspiration for Fusion was Quora's LiveNode framework
* [Microsoft](microsoft.com) &ndash; for .NET Core and Blazor.
* The authors and maintainers of every library used by Fusion. Most notably,
  [DynamicProxy from Castle.Core](http://www.castleproject.org/projects/dynamicproxy/),
  [RestEase](https://github.com/canton7/RestEase), and 
  [Json.NET](https://www.newtonsoft.com/json).

[Overview]: Overview.md
[Tutorial]: https://github.com/servicetitan/Stl.Fusion.Samples/blob/master/docs/tutorial/README.md
[Fusion Samples]: https://github.com/servicetitan/Stl.Fusion.Samples

[Gitter]: https://gitter.im/Stl-Fusion/community
[Discord Server]: https://discord.gg/EKEwv6d
[Fusion Feedback Form]: https://forms.gle/TpGkmTZttukhDMRB6
