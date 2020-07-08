# Part 1: `IComputed<TOut>` and `SimpleComputed<TOut>`

Nearly everything in `Stl.Fusion` is based on `IComputed<TOut>` - an abstraction encapsulating 
a _computation_ and _its output_.

At glance, these instances are very similar to "Observable" \ "Computed observable" abstractions 
from such libraries as [Knockout.js](https://knockoutjs.com/) or [mobX](https://mobx.js.org/),
but there are a few quite significant differences.

Before jumping into the details, let's play with `IComputed` first.  

## Creating `IComputed<TOut>` instance

The simplest way to create a new `IComputed<TOut>` instance ("computed" further) is
to use `SimpestComputed.New` shortcuts.

``` cs --region part01_create --source-file Part01.cs
// Use try-dotnet to see this part 
```

As you might notice, every computed has `Value`, which stores the cached result of 
the computation that is tied to the instance. 

In reality, `Value` is only a part of the `Output`, the other one is `Error` - 
an exception you'll see trying to access the `Value` if the computation completed 
with an error. `IComputed<TOut>` implements `IResult<TOut>` (for convenience), 
but also exposes the underlying `Result<TOut>` via its `Output` property.

You'll notice this design pattern (i.e. both expose `Result<TOut> Output` and 
implement `IResult<TOut>`) is used in some other abstractions - in particular,
in `ILiveState`.

Computed instances are *semi-immutable*:
* Most of them are "born" in `Computing` state indicating the computation 
  that backs them is still running.
* Once computed, they enter `Consistent` state. That's the moment they
  turn immutable: no changes can be made to their `Output` after that.
* The only thing that *may* happen is transition to `Invalidated` state.
  It happens when either you manually call `Invalidate()` method on
  computed instance, or when this happens automatically because one of
  its dependencies was invalidated.
  
Let's see how it works:    

``` cs --region part01_invalidateAndUpdate --source-file Part01.cs
// Use try-dotnet to see this part 
```

As you might notice, the instances we were playing with so far were "born"
in `Consistent` state. That's because they were constructed using 
`SimpleComputed.New(..., Result<TOut> initialOutput)`; but there is a way
to construct computed instances that are "born" invalidated:
  
``` cs --region part01_createNoDefault --source-file Part01.cs
// Use try-dotnet to see this part 
```


#### [Next: Part 2 &raquo;](./Part02.md) | [Tutorial Home](./README.md)
