# Part 6: Computed Instances and Computed Services - Review

## Computed Instance

Computed Instances are instances of types implementing `IComputed<TOut>`.

All of them have:
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
  call. Invalidation is *always cascading*.
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

Besides that:
* Computed instances are semi-immutable: once constructed, the only
  thing that might happen to them is transition to `Inconsistent` state.
* Despite immutability, you're free to add `Invalidated` event handlers and
  invoke `Invalidate()` any time.
* There can be only one "consistent" instance for any computation at any time.
  This policy is enforced via `IComputedRegistry`.
* Computed instances maintain dependent-dependency relationships to ensure 
  cascading invalidation works. But contrary to other libraries, these
  relationships aren't "symmetric":
  * References to dependencies ("used") instances are always strong ones,
    so if you store a reference to some output, it's guaranteed that any 
    of its inputs (and their inputs) will also reside in memory. This,
    combined with "only one consistent instance" rule, ensures that 
    invalidation of any dependency reaches every instance that depends on
    it.
  * And on contrary, references to dependent instances are "weak" - in fact,
    just keys of these instances are stored, so unreferenced dependants are 
    available for GC even if what they "use" is still referenced.

## Computed Service

Computed Service is any type that implements `IComputedService` interface 
(just a tagging one) and gets registered in `IServiceCollection` via 
`AddComputedService` extension method.

* These services are always registered as singletons in the container.
  This is totally logical, assuming the keys of computed instances they
  produce include the reference to the service itself (`this`).
* Normally it should declare methods decorated with [ComputedServiceMethod]
  attribute (btw, check out its properties - they are useful)
* Such methods have to be virtual, otherwise it's impossible to generate
  a proxy type that will override and "wrap" them.
* Moreover, these methods should return one of the following types:
  * `Task<T>` or `ValueTask<T>` - we recommend use these types
  * `Task<IComputed<T>>` or `ValueTask<IComputed<T>>` - such methods
    are fully supported as well, and they really return the computed
    instances created behind the scenes. You may find how to properly
    return the result in such cases in our tests, but overall,
    we don't recommend to use this syntax, and there is a high 
    likelihood we'll discard it in future (eliminating one extra check
    for something no one uses is always a good thing).
* Computed service methods may call each other. When this happens,
  a dependency between corresponding computed instances is created 
  automatically. 
  * They don't have to share the same container
    to be able to call each other; same is relevant for almost anything
    else in Fusion.
  * It's ok for computed service method to call itself recursively,
    though keep in mind that longer dependency chains mean longer
    invalidation and larger set of instances to keep in RAM.
    
## Useful helpers

Most of them are a part of `Computed` type (static class).
* `Computed.CaptureAsync(Func<CancellationToken, Task<T>> producer)` captures
  *the last* computed instance, which `Output` was assigned. Typically it's 
  the final output of the computation executed by `producer`.
* `Computed.Invalidate(Func<Task<T>> producer)` invalidates the computed
  instance corresponding to the first computed service method (incl. arguments) 
  invoked by `producer`. Notice the method isn't async, even though the producer
  is returning `Task<T>` &ndash; that's because Fusion proxies are completing
  method calls with "invalidate request" synchronously: all they need is 
  the cache key (method arguments), and it becomes available once proxy method 
  gets invoked.
  * Remember that invalidation is always cascading.
* Similarly, there is `Computed.TryGetCached<T>(Func<Task<T>> producer)` method,
  which retrieves cached computed instance corresponding to the computed
  service method invoked by `producer`. It is also synchronous - hopefully,
  now it's clear, why. 

#### [Next: Part 7 &raquo;](./Part07.md) | [Tutorial Home](./README.md)
