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
Yes, we can! 

``` cs --region part05_useServices_part2 --source-file Part05.cs
// Use try-dotnet to see this part 
```

That's all on computed services. Let's summarize the most important
pieces:

`IComputed<TOut>` has:
* `State` property, which transitions from 
  `Computing` to `Computed` and `Invalidated`. It's fine, btw, 
  to invalidate a computed instance in `Computing` state -
  this will trigger later invalidation, that will happen right
  after it enters `Computed` state.
* `IsConsistent` property - a shortcut to check whether its state 
  is exactly `Consistent`
* `Output` property - it stores either a value or an error; 
  its actual type is `Result<TOut>`.
* `Value` property - a shortcut to `Output.Value`
* `Invalidate()` method - turns computed into `Invalidated` state.
  You can call it multiple times, subsequent calls do nothing.   
* `Invalidated` event - raised on invalidation. Handlers of this event
  should never throw exceptions; besides that, this event is raised
  first, and only after that the dependencies get similar `Invalidate()`
  call.
* `InvalidatedAsync` - an extension method that allows you to await
  for invalidation.
* `UpdateAsync` method - returns the most up-to-date (*most likely*, 
  consistent - unless it was invalidated right after the update)
  computed instance for the same computation.
* `UseAsync` method - the same as `UpdateAsync(true).Value`.
  Gets the most up-to-date value of the current computed and
  makes sure that if this happens inside the computation of another
  computed, this "outer" computed registers the current one
  as its own dependency.       

`IComputedService`:
* Is any type that implements this interface and is registered
  in `IServiceCollection` via `AddComputedService` extension method.
* It typically should   


#### [Next: Part 6 &raquo;](./Part06.md) | [Tutorial Home](./README.md)
