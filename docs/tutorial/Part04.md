# Part 4: Computed Services: execution, caching, and invalidation

Just a reminder, we're going to use the same "shortcut" to create an instance 
of computed service:

``` cs --editable false --region part04_createHelper --source-file Part04.cs
// Use try-dotnet to see this part 
```

## Call execution

> When you simultaneously call the same method of computed service 
> with the same arguments from multiple threads, it's guaranteed 
> that:
> * At most one of these calls will be actually executed. All other  
>   calls will wait till the moment its result is ready, and once
>   it happens, they'll simply return this result.
> * At best none of these calls will be actually executed - in case
>   when the consistent call result for this set of arguments is 
>   already cached.

Let's create a simple service to show this:

``` cs --region part04_defineCalculator --source-file Part04.cs
// Use try-dotnet to see this part 
```

And a simple test:

``` cs --region part04_defineTestCalculator --source-file Part04.cs
// Use try-dotnet to see this part 
```

Now, let's compare the behavior of a "normal" instance
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

So everything stays in cache here. Let's check out how caching works.

## Cache invalidation & eviction

> Every result of computed service method call is cached:
> * (this, methodInfo, arguments) is the key; in reality,
>   it's a bit more complex: e.g. `CancellationToken` argument
>   is always ignored. `IArgumentComparerProvider` (one of services
>   you can register in the container) decides how to compare the keys,
>   but the default implementation always relies on `object.Equals`.
> * `IComputed<TOut>` is the value that's cached. It is
>   always strongly referenced for at least `ComputedOptions.KeepAliveTime`
>   after the last access operation (cache hit for this value),
>   and weakly referenced afterwards until the moment it gets invalidated. 
>   Once invalidated, it gets evicted from the cache. 
>   Weak referencing ensures there is one and only one valid instance
>   of every `IComputed` produced for a certain call. 

Let's try to make Fusion to evict some cached entries:

``` cs --region part04_useCalculator3 --source-file Part04.cs
// Use try-dotnet to see this part 
```

`ComputedRegistry.Prune` removes string references to the
entries with expired `KeepAliveTime` (1s after the last use by default)
and evicts the entries those `IComputed` instances are collected by GC.

We invoked it in above example to ensure strong references to our cached 
computed instances (method outputs) are removed, so GC can pick it up.

`Prune` is triggered once per `O(registry.Capacity)` operations (reads and
updates) with `ComputedRegistry`. This ensures the amortized cost of pruning 
is `O(1)` per operation.  

So in reality, you don't have to invoke it manually - it will be invoked 
after a certain number of operations anyway:

``` cs --region part04_useCalculator4 --source-file Part04.cs
// Use try-dotnet to see this part 
```

Ok, but we already know `IComputed` instances can be invalidated.
Can we somehow pull an instance of `IComputed` that represents the result
of method call and invalidate it manually? Yes: 

``` cs --region part04_useCalculator5 --source-file Part04.cs
// Use try-dotnet to see this part 
```

There is a shorter way to invalidate method call results:

``` cs --region part04_useCalculator6 --source-file Part04.cs
// Use try-dotnet to see this part 
```

#### [Next: Part 5 &raquo;](./Part05.md) | [Tutorial Home](./README.md)
