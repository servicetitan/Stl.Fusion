# Part 3: `IComputedService` and a nicer way to create `IComputed<TOut>`

So far we were creating computed instances relying on `SimpleComputed.New`,
but there is a much better way to do this, which gives a number of advantages. 

Let's create a helper we'll be using for a few next samples first:

``` cs --editable false --region part03_createHelper --source-file Part03.cs
// Use try-dotnet to see this part 
```

As you might notice, it:
* Creates an `IServiceCollection` - likely the same one you
  use in most of other cases you rely on 
  [Dependency Injection](https://stackify.com/net-core-dependency-injection/) 
  in .NET Core.
* Registers core Fusion services there. We aren't going to use all of them
  in the next example (and honestly, there aren't a lot of them - you're 
  welcome to look at the source code of this method to understand what 
  it really does), but we need a couple of them to make our Computed Service
  work.
* And finally, it registers the service itself, but using a special helper:
  `AddComputedService`. This method actually registers a singleton of the 
  same type, but instead of returning the actual instance of this type
  it returns a Fusion-provided proxy for this class. You'll see what it 
  does further.

Now, let's declare our first computed service:

``` cs --editable false --region part03_service1 --source-file Part03.cs
// Use try-dotnet to see this part 
```

And finally, let's play with it:

``` cs --region part03_useService1_part1 --source-file Part03.cs
// Use try-dotnet to see this part 
```

So what's going on here?
* The actual service type is `Castle.Proxies.Service1Proxy` - a runtime-generated 
  type, and probably you've correctly guessed we use 
  [Castle.DynamicProxy](http://www.castleproject.org/projects/dynamicproxy/)
  to generate it. 
* You've noticed the first call triggered 2 method evaluations: one for `GetTimeWithOffsetAsync`, 
  and another one for `GetTimeAsync`. But the second call evaluated just `GetTimeWithOffsetAsync` -
  why?
  
The reason is: 
* Both methods marked as `[ComputedServiceMethod]` were "wrapped" into a code similar
  to the one you used to create computed instances earlier - but the instances that
  was created under the hood were "stripped off" (i.e. the proxy returned their `.Value` 
  property), because otherwise the return type wouldn't match (it could be e.g. a 
  `Task<IComputed<DateTime>>` otherwise, but it's incompatible with the
  original method signature).
* Nevertheless, the computed instance was created, which in turn enabled it to capture 
  the dependencies that were "used" during the execution - namely, other computed instances
  accessed there.
* The "wrapper" logic behind computed services doesn't create a new instance every
  time you call the method - it does something that's a bit more complex:
  1.  It transforms the method call arguments (including `this`, i.e. the service 
      instance itself) into a `ComputedInput` instance - an object allowing to identify
      the most "current" computed instance computed for the same input.
  2.  It uses `IComputedRegistry` to find out if there is a computed instance
      available for the same key. Computed registry is one of services registered 
      in IoC container by `AddFusionCore`, which registers its default instance 
      (which is shared across all the containers) unless you register your own registry 
      earlier.
  3.  If there is an `IComputed` instance that corresponds to the same input *and*
      this instance is still consistent, it simply returns it.
      Otherwise it creates a new instance, runs the underlying computation to
      compute its value, and registers it in computed registry.
* In other words, any computed service provides a cache that enables it to
  avoid the computation, while *presumably* its result must be exactly the same as 
  the cached one.       

This is why the second call to `GetTimeWithOffsetAsync` doesn't trigger `GetTimeAsync`
computation: `GetTimeAsync` doesn't have any arguments, and its previous result wasn't
invalidated, so when the second call to `GetTimeWithOffsetAsync` tried to invoke it,
the proxy provided by Fusion resolved this call via cache (`IComputedRegistry`).   

> *Note:* In reality, much more was going on behind the scene. We'll talk
> more about the specific aspects of this further.

Now, let's look at some fancy consequences of what you just saw, 
and also learn how to "pull" the underlying `IComputed` instances 
that back such methods. 

#### [Next: Part 4 &raquo;](./Part04.md) | [Tutorial Home](./README.md)
