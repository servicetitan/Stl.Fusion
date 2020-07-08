# Part 4: Playing with `IComputedService`

Just a reminder, we're going to use the same "shortcut" to create an instance 
of computed service:

``` cs --editable false --region part04_createHelper --source-file Part04.cs
// Use try-dotnet to see this part 
```

## Computed Service Methods compute just once for the same set of arguments  

Let's create a simple service to show this:

``` cs --region part04_defineCalculator --source-file Part04.cs
// Use try-dotnet to see this part 
```

And a simple test:

``` cs --region part04_defineCalculator --source-file Part04.cs
// Use try-dotnet to see this part 
```

Now, let's compares the behavior of a "normal" instance
and the instance provided by Fusion:

``` cs --region part04_useCalculator1 --source-file Part04.cs
// Use try-dotnet to see this part 
```

As you see, even though the final sum is the same, the way it works
is drastically different:
* The computed service version runs every computation just
  once for each unique set of arguments
* The computations for each unique set of arguments are running
  in parallel.  

Let's check for how long it actually caches the results:

``` cs --region part04_useCalculator2 --source-file Part04.cs
// Use try-dotnet to see this part 
```

Long enough, right? Most likely you saw that everything stays in cache.
Let's try to get rid of what's cached:

``` cs --region part04_useCalculator3 --source-file Part04.cs
// Use try-dotnet to see this part 
```

`ComputedRegistry.Prune` removes string references to the
entries with expired KeepAliveTime (1s after the last use by default)
and evicts the entries those `IComputed` instances are collected by GC 
already. We invoked it to ensure the strong reference to
our cached computed instances is removed, so GC can pick it up.

`Prune` is triggered once per `O(registry.Capacity)` operations with
the registry - e.g. reads or updates. This ensures the amortized cost
of pruning is O(1) per operation.  

Let's show it is truly invoked implicitly:

``` cs --region part04_useCalculator4 --source-file Part04.cs
// Use try-dotnet to see this part 
```

#### [Next: Part 5 &raquo;](./Part05.md) | [Tutorial Home](./README.md)
