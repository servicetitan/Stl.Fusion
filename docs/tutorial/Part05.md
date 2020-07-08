# Part 5: Computed Services: dependencies

> When a computed service method is executed, its output becomes
> dependent on every computed service output it calls.

Here is how it works:

``` cs --region part05_defineServices --source-file Part05.cs
// Use try-dotnet to see this part 
```

We'll use a bit different container builder here:

``` cs --region part05_createServiceProvider --source-file Part05.cs
// Use try-dotnet to see this part 
```

Now, let's run some code:

``` cs --region part05_useServices_part1 --source-file Part05.cs
// Use try-dotnet to see this part 
```

I hope it's clear how it works now:
* Any result produced by computed service gets cached till the moment 
  it gets either invalidated or evicted. Eviction is possible only while 
  no one uses or depends on it.
* Invalidations are always cascading, i.e. if A uses B, B uses C, and C
  gets invalidated, the whole chain gets invalidated.
  
Now, can we await for invalidation and update the computed instance? 

``` cs --region part05_useServices_part2 --source-file Part05.cs
// Use try-dotnet to see this part 
```

#### [Next: Part 5 &raquo;](./Part05.md) | [Tutorial Home](./README.md)
