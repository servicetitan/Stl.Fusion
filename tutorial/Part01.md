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

``` cs --region part01_create --source-file Part01.cs --project Tutorial.csproj
```

#### [Next: Part 2 &raquo;](./Part02.md) | [Tutorial Home](./README.md)
