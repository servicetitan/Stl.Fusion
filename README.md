# STL
STL stands for, obviously, "ServiceTitan Library" :)

## Conventions

### Layout
* All Stl projects are currently targeting .NET Core 3.0.
* The "big" `Stl` project is supposed to contain the most generic and most useful abstractions;
* Everything else is supposed to be in Stl.* projects, which target specific problems (e.g. `Stl.Time`).
* All the tests are currently located in `Stl.Tests` project - with subfolders & namespaces matching sub-projects & namespaces in Stl. Likely, that's temporary - eventually these tests are supposed to move to a set of dedicated test projects (one test project per one Stl project).

### Coding Style
It's based on standard coding conventions for C# with a few exceptions:
* It's fine to use "Ex" suffix instead of "Extensions" for static classes with extension methods - e.g. use `EnumerableEx` instead of `EnumerableExtensions`. Such names are shorter + they open the door for adding non-extension static methods to the same types as well, which is frequently quite desirable (e.g. `EnumerableEx.One`).
* "`{}`" might be omitted for statements like `for` and `if` - this seems to be more in line with other language changes now (e.g. expression method bodies).
* If it improves readability, it's ok to put "`=>`" on the next line for expression method bodies (i.e. have "`=> DoSomething(...);`" on a separate line).
* `Internal` namespace contains types that _typically_ aren't supposed to be used outside of the library.

## What's inside?

### Stl project:

* Async:
  * `AsyncChannel<T>` is one-way asynchronous channel suppporting both streaming and non-streaming `Push` and `Pull` operations.
  * `AsyncEnumerable` and `AsyncEnumerableEx` provide some missing extension methods for `IAsyncEnumerable<T>` - `Interval`, converstion to/from `IObservable<T>`, some LINQ methods, etc.; these classes exist mainly to close some of the existing gaps in `IAsyncEnumerable<T>` API / support in .NET Core.
  * `TaskEx`: typically conversion methods, e.g. `ToValueTask` (for tasks), `ToTask` (for `CancallationToken`) + `SuppressExceptions` and `SuppressCancellation` (for tasks).
  * `ValueTaskEx`: `CompletedTask`, `TrueTask`, `FalseTask`, `New<T>(T value)`.
  * The rest is less important.
  * `AsyncDisposable` - a struct-based `IAsyncDisposable` implementation invoking the specified delegate on disposal.

* Caching:
  * `ICache<TKey, TValue>` + its implementations (the simplest async cache w/o any kind of implicit expiration)
  * `FileSystemCache` storing its keys in a folder as files (keys are trimmed & hashed when converted to file names). Used by `Stl.Plugins` to cache plugin scan info (scan itself might take a while if there are lots of assemblies); generally fits well for all similar scenarios.
  * `FakeCache` - caches nothing, though pretends to.
  * `MemoizingCache` - plain in-memory cache implementation.
  * `ComputingCache` and `FastComputingCache` - they aren't "caches" per se, even though they use caches internally. They implement `IAsyncKeyResolver<TKey, TValue>`, i.e. the read-only API part of any cache; once constructed with a delegate computing the value by key, they make sure this value is constructed only when it isn't cached for this key - i.e. they memoize the computed values. The logic there is a bit more complex then it might seem mainly to make sure that if the value for certain key is computing right now (but isn't cached yet), all the requests to get this value will await for the completion of the computation.

* Collections:
  * Mentioned `IAsyncKeyResolver<TKey, TValue>` and `AsyncKeyResolverBase<TKey, TValue>`
  * `BinaryHeap<T>` and `FenwickTree<T>`
  * `LazyDictionary<TKey, TValue>` - an `IDictionary` implementation creating actual dictionary on demand (i.e. once the first item is added). Useful in scenarios where you are supposed to have lots of empty dictionaries :)
  * `HDictionary<TKey, TItem> where TItem : HDictionary<TKey, TItem>` - hierarchical dictionary abstraction based on `LazyDictionary`; useful mainly because it's really space-savvy + there are ready to use `ToString` and `Dump` methods.

* CommandLine:
  * A number of abstractions simplifying command line building. Written mainly to support command line tool invocation scenarios for Bach; see `ShellTest` and `Terraform*Test`.
  * TODO: a dedicated page :)

* Extensibility:
  * Various types aiming to support specific extensibility scenarios.
  * `Factory<T>` is just a useful type to inject into DI containers
  * `IHasOptions<T>` supports one of common extensibility scenarios (options, where each option is defined by key & value; keys are typically types).
  * `Invoker` and `AsyncInvoker` provide an abstraction similar to `IEnumerable<T>.Aggregate`, but allowing each handler to not only update the state, but also modify the rest of the chain by both chaning it directly & invoking it recursively. The typical use case is plugin invocation, i.e. when you have a list of plugins ordered by dependency & want to invoke them in such a way that plugins in the beginning of the chain: 
  **a)** invoke the rest of plugins directly (so they can run some logig prior and after the invocation), and 
  **b)** can simply edit the rest of the chain (to e.g. suppress certain plugins completely).
  See e.g. `HostBuilderEx.UsePlugins` method to see how invokers can be used.
  * `ServiceCollectionEx` provides `HasService<T>` and `CopySingleton<T>` methods.
  * `ServiceProviderEx` provides `Empty`, `Activate` and `TryActivate` methods; the activation methods are similar to `Type.Activate`, but "borrow" the constructor arguments from the container; i.e. basically, they allow you to construct an instance that isn't registered in the container like if it would be registered there. Since the implementation is fairly simple for now, it tries to use either the constructor marked with `[ServiceConstructor]` attribute, or the constructor having the largest number of arguments. And yeah, it's fairly slow.

* IO:
  * `FileSystemWatcherEx.ToObservable` converts `FileSystemWatcher` to `IObservable<FileSystemEventArgs>`
  * `PathEx` provides a few useful path-related methods (e.g. `GetHashedName` and `GetApplicationTempDirectory`)

* Locking:
  * `IAsyncLock`, `IAsyncLock<TKey>` and its implementations -- robust async lock abstraction supporting `ReentryMode`; used by a few caches.
  * `FakeKeyLock<TKey>` pretends to be a key-based lock, though in reality it's a single `IAsyncLock`.

* Mathematics:
  * `MathEx` provides caching `BigInteger Factorial(int n)` implementation + `Min`, `Max`, `Gcd`, `ExtendedGcd` and `Lcm` methods for `long` and `ulong`.
  * `Bits` provides fast implementations for the most useful bit operations - `IsPowerOf2`, `LsbIndex` (least significant bit index), `MsbIndex` (most significant bit index), `Count`, etc.
  * `Combinatorics` provides efficient `Cnk` (Binomial coefficient) function and functions like `IEnumerable<List<int>> KOfN` or `IEnumerable<Memory<T>> Subsets<T>(Memory<T> source)`
  * `PrimeSieve` is, well... Guess what.

* OS:
  * `OSInfo.Kind` returns `OSKind` (`Unix`, `Windows`, `MacOS`); `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` is too exhausting to type.

* Reflection:
  * `TypeEx` provides `GetAllBaseTypes(Type type)` and `ToMethodName(Type type)`; the later one converts type name to a string that can be used as a part of some method name; it is intended be used in visitor-like scenarios - i.e. where you have to invoke a specific method for each specific type of instance you pass there; currently it's used by `DispatchingNodeProcessorBase`; see `TypeExTest.ToMethodNameTest` to understand the exact type name transformations it does.
  * `ExpressionEx` mainly features `MemberTypeAndName`, i.e. transforms ~ `x => x.ToString()` to `(typeof(object), methodof(object.ToString))`.
  * `TypeRef` is, basically, a serializable `Type`. Currently it's mainly used by `Stl.Plugins` (it caches plugin scan info, so it has to serialize types somehow).

* Serialization:
  * `INotifyDeserialized` is ~ a hacky way to solve notification order problem for `[OnDeserialized]` notifications: they are sent out of order, though in many cases you want some nested structures to be notified (and thus fully deserialized) first. So one of ways to make sure this happens is to manually notify them from the parent - which can be done by checking on whether they do support this interface or not. Long story short: avoid using it unless you see no other way to solve the problem.

And finally there are a few useful types residing right in the `Stl` namespace:
* `Option<T>` - an option type implementation. Initially I used `Optional` NuGet package, but ended up realizing there is almost nothing there, so it's more logical to simply re-implement it right in Stl.
* `Result<T>` and related types (`IResult<T>`, `IResult`, `IMutableResult<T>`, `IMutableResult`) - stores either a result of type `T` or an error.
* `Symbol` - a `struct` storing `(string Value, int ValueHashCode)`. Useful when you store lots of such strings in dictionaries, i.e. when the hash code has to be computed frequently.
* `SymbolPath` - ~ like a functional list of `Symbol`s, i.e. stores the `(Symbol Head, SymbolPath Tail, int PathHashCode)`. A good abstraction for scenarios where you want to search by these paths in dictionaries and build them dynamically. E.g. when you extend a path, no string concatenation happens, + its hash is computed from hashes of `Head` and `Tail` (i.e. already cached hashes).
* Both `Symbol` and `SymbolPath` are used in `Stl.ImmutableModel`, though the abstractions seems to be useful in other places - that's why they're in Stl. The location in Stl root is arguable though, i.e. maybe I'll move them to `Stl.Strings` eventually.
* `IgnoreEx` - `whatever.Ignore()` (returns void), `.IgnoreAsUnit()` (returns `Unit`), and `.ToUnitFunc()` extension methods
* `Disposable` - a struct-based `IDisposable` implementation invoking the specified delegate on disposal.
* `KeyValuePair` - `New<TKey, TValue>(key, value)` (why it's missing in .NET Core?) & `ToKeyValuePair(this (TKey Key, TValue Value) pair)`
* `EnumerableEx` - `One`, `Concat(params IEnumerable<T> sequences)`, a pair of `ToDictionary` overloads, atomic `concurrentDictionary.TryRemove(key, value`), and finally, `sequence.ToDelimitedString(string demimiler = ", ")`.


### Stl.Testing project

Various testing-related helpers.

TODO: Write a dedicated page for this project.


### Stl.Experimental project

Various experimental stuff that might go into other Stl proejcts later.

Currently there is just `TerraformCmd` - an implementation of Terraform command line API:

```csharp
var r = await TerraformCmd.ApplyAsync();
r.ExitCode.Should().Be(0);
r.StandardOutput.Should().Contain("Apply complete!");
```

### Stl.Time project

* `Moment`: a `DateTime`-like abstraction storing Unix epoch time (as `TimeSpan`). Freely convertable to `DateTime` and `DateTimeOffset`, serializable, more compact than a regular `DateTime`, and since it always stores time in UTC, there is no any ambiguity on what kind of time it stores. In short, it's a bit more compact & faster version of `DateTime` without any problems it has. The intended use case are mostly HPC scenarios involving time (e.g. metric stream processing).
* `IClock` - a robust clock abstraction. The nice part is that it supports `DelayAsync` & a few other methods through it (see `ClockEx.Timer`, `Interval`), so if you use it in tests & switch to `TestClock` there, you'll be able to speed up / slow down & rewind time. Moreover, note that `TestClock.DelayAsync` implementation is designed to _instantly_ react to test clock changes, i.e. if you'll rewind your test clocks to +1 year, all the `DelayAsync` calls that are supposed to complete by this moment will complete instantly.

See `ClockTest` for some examples of how it works.


### Stl.Plugins project

An apllication extensiblity framework similar to MEF, but aiming to solve a few fundamental problems it has:
* Plugins / extensions must "live" in any IoC container implementing `IServiceCollection` and `IServiceProvider` from `Microsoft.DependencyInjection.Abstractions`
* The plugin discovery / scan results must be cached & re-used on any subsequent creation of plugin container. The only operation triggering plugin assembly load should be the creation of a plugin stored there (or a plugin that depends on it).

TODO: Write a dedicated page for this project.


### Stl.Plugins.Extensions project

A few useful extensions to Stl.Plugins targeting the most generic plugin-based extensibility scenarios:
* `ICliPlugin` + `commandLineBuilder.UsePlugins<TPlugin>(IEnumerable<TPlugin> plugins)  where TPlugin : ICliPlugin`
* `IHostPlugin` + `hostBuilder.UsePlugins<TPlugin>(IEnumerable<TPlugin> plugins)  where TPlugin : IHostPlugin`
* `IWebHostPlugin` + `webHostBuilder.UsePlugins<TPlugin>(IEnumerable<TPlugin> plugins)  where TPlugin : IWebHostPlugin`

See the source code / usage of these methods to see how it works.


### Stl.Hosting project

Provides a generic plugin-based host (app bootstrapper) enabling plugins to:
* Inject & process their own console commands & arguments via `CommandLineBuilder`
* Tune the host in ~ arbitrary way via `IHostBuilder` 
* Tune the web host in ~ arbitrary way via `IWebHostBuilder`
* Inject plugin services to any of available `IServiceCollections`

Used by Bach. See `BachHostBuilder` (in fact, a tiny class) to understand how it's intended to be used.


### Stl.ImmutableModel project

A framework allowing to define & use immutable models. Used by Bach. 

`Bach.ExampleAddon` is probably the only good example of how it's supposed to be used. + Tests in `Stl.Tests.ImmutableModel`.

TODO: Write a dedicated page for this project.


### Stl.Reactionist project

These are my experiments with mobX-style observables; originally I thought to use this in Bach models, but ended up switching to `Stl.ImmutableModel`-based abstractions there.

TODO: Write more robust description + decide whether to even keep this.

