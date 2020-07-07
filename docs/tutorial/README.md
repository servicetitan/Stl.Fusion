# `Stl.Fusion` Tutorial

## Prerequisites

Install: 
- [.NET Core SDK 3.1](https://dotnet.microsoft.com/download) - you need it
  to build `Stl.Fusion`, its samples, and this tutorial
- [try-dotnet](https://github.com/dotnet/try/blob/master/DotNetTryLocal.md) -
  it's the tool to "run" the tutorial. If its release version fails to run
  the code (right now it does, the bug is reported), you'll need to install 
  its preview version using the following command:
  ```bash
  dotnet tool install -g --add-source "https://dotnet.myget.org/F/dotnet-try/api/v3/index.json" Microsoft.dotnet-try
  ```

To run the tutorial, `cd` to the "tutorial" folder and type:
```bash
dotnet try
```

## Tutorial

The code based on `Stl.Fusion` (we'll refer to it as "Fusion" further)
might look completely weird at first - that's because it is based
on abstractions you need to learn about before starting
to dig into the code. 

Understanding how they work will also eliminate a lot
of questions you might get further, so we highly recommend you
to complete this tutorial *before* digging into the source
code of Fusion samples.

Without further ado:
* [Part 0: NuGet packages](./Part00.md)
* [Part 1: `IComputed<TOut>` and `SimpleComputed<TOut>`](./Part01.md)
* [Part 2: Dependencies between computed instances](./Part02.md)
* [Part 3: `IComputedService` and a nicer way to create `IComputed<TOut>`](./Part03.md)
* [Part 4: Playing with `IComputedService`](./Part04.md)
