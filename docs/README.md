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

- [Slides] explain what problem Fusion solves, 
  how a simplified version of Fusion's key abstraction could be implemented in C#, 
  and points on interesting connections with many other problems
- [Tutorial] covers key abstractions and code examples helping to learn them.
- [Overview] is a bit outdated and much more boring version of what [Slides] cover
- [The Story Behind Fusion](Story.md) &ndash; maybe you'll find it interesting too

Posts:
- [Fusion: Current State and Upcoming Features](https://alexyakunin.medium.com/fusion-current-state-and-upcoming-features-88bc4201594b?source=friends_link&sk=375290c4538167fe99419a744f3d42d5)
- [The Ungreen Web: Why our web apps are terribly inefficient?](https://alexyakunin.medium.com/the-ungreen-web-why-our-web-apps-are-terribly-inefficient-28791ed48035?source=friends_link&sk=74fb46086ca13ff4fea387d6245cb52b)
- [Why real-time UI is inevitable future for web apps?](https://medium.com/@alexyakunin/features-of-the-future-web-apps-part-1-e32cf4e4e4f4?source=friends_link&sk=65dacdbf670ef9b5d961c4c666e223e2)
- [How similar is Fusion to SignalR?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-signalr-e751c14b70c3?source=friends_link&sk=241d5293494e352f3db338d93c352249)
- [How similar is Fusion to Knockout / MobX?](https://medium.com/@alexyakunin/how-similar-is-stl-fusion-to-knockout-mobx-fcebd0bef5d5?source=friends_link&sk=a808f7c46c4d5613605f8ada732e790e)
- [Fusion In Simple Terms](https://medium.com/@alexyakunin/stl-fusion-in-simple-terms-65b1975967ab?source=friends_link&sk=04e73e75a52768cf7c3330744a9b1e38)

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

You can always ask for help here:
* [Discord Server] &ndash; <a href="https://discord.gg/EKEwv6d">
  <img valign="middle" src="https://img.shields.io/discord/729970863419424788.svg" alt="Discord Server">
  </a>

## Credits

[ServiceTitan](https://www.servicetitan.com/) &ndash; for giving some of us
an opportunity to work on this project.

Contributors:
* [Vladimir Chirikov](https://github.com/vchirikov) &ndash; build system & misc. fixes
* [Alexey Ananyev](https://github.com/hypercodeplace) &ndash; misc. fixes
* [Alexey Golub](https://github.com/Tyrrrz) &ndash; minor fixes; FYI we use his 
  [CliWrap](https://github.com/Tyrrrz/CliWrap) in our build pipeline, and his
* [Alex Yakunin](https://github.com/alexyakunin) ([Medium](https://medium.com/@alexyakunin)) &ndash; 
  the creator of Fusion.

Indirect contributors & everyone else who made Fusion possible:
* [Steve Sanderson](http://blog.stevensanderson.com/) &ndash; 
  for both [Knockout](https://knockoutjs.com/), which made "computed observable" abstraction popular, 
  and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) &ndash;
  yeah, Steve is the creator of it as well!
* [Quora](https://www.quora.com/) — a huge part of the inspiration for Fusion was Quora's LiveNode framework
* [Microsoft](microsoft.com) &ndash; for .NET Core and Blazor.

[Slides]: https://alexyakunin.github.io/Stl.Fusion.Materials/Slides/Fusion_v2/Slides.html
[Overview]: Overview.md
[Tutorial]: https://github.com/servicetitan/Stl.Fusion.Samples/blob/master/docs/tutorial/README.md
[Fusion Samples]: https://github.com/servicetitan/Stl.Fusion.Samples

[Discord Server]: https://discord.gg/EKEwv6d
[Fusion Feedback Form]: https://forms.gle/TpGkmTZttukhDMRB6
