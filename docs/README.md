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

* [Overview](Overview.md) is the best place to start. 
  It describes what Stl.Fusion is on conceptual level
  and explains the most tricky concepts on relatively simple
  examples.
* [Tutorial](https://github.com/servicetitan/Stl.Fusion.Samples/blob/master/docs/tutorial/README.md) &ndash; 
  now a part of [Stl.Fusion.Samples repository](https://github.com/servicetitan/Stl.Fusion.Samples),
  not fully finished yet, but the best part is: it is interactive, so any code you see
  there is runnable!
* [The Story Behind Stl.Fusion](Story.md) &ndash; maybe you'll find
  it interesting too.
* Join our [Discord Server](https://discord.gg/EKEwv6d) 
  to ask questions and track project updates
* Check out [Q/A](QA.md) to get answers on some frequent questions.

Posts:
* [Stl.Fusion In Simple Terms](https://medium.com/@alexyakunin/stl-fusion-in-simple-terms-65b1975967ab?source=friends_link&sk=04e73e75a52768cf7c3330744a9b1e38) &ndash;
  so far the best description of what Fusion is for non-developers
* [How similar is Stl.Fusion to SignalR?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-signalr-e751c14b70c3?source=friends_link&sk=241d5293494e352f3db338d93c352249)
* [How similar is Stl.Fusion to Knockout / MobX?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-knockout-mobx-fcebd0bef5d5?source=friends_link&sk=a808f7c46c4d5613605f8ada732e790e)


That's it for now, but we'll definitely add more over time. 

## Credits

[ServiceTitan](https://www.servicetitan.com/) &ndash; for giving some of us
an opportunity to work on this project.

Contributors:
* [Vladimir Chirikov](https://github.com/vchirikov) &ndash; build system & misc. improvements
* [Alexey Ananyev](https://github.com/hypercodeplace) &ndash; minor fixes
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

P.S. [Your pull request](https://github.com/servicetitan/Stl/pulls) is all we need to list you here.
