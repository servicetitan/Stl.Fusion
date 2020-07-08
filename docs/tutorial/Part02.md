# Part 2: Dependencies between computed instances

Computed instances describe computations, and if these computations
involve other computed instances, the computation that uses another
one is called "dependent", and the used one is called a "dependency".

The following rules apply:   
* Once a dependency ("used" instance) gets invalidated, all the computed instances
  that depend on it (directly and indirectly) are invalidated as well. 
  This happens *synchronously* right when you call `dependency.Invalidate()`.
* Once a dependent instance gets (re)computed, it triggers computation of all its
  dependencies (unless they are already computing or computed, i.e. their most 
  recently produced `IComputed` instance is not in `Invalidated` state).

The code below (sorry, it's large) explains how it works:

``` cs --region part02_dependencies --source-file Part02.cs
// Use try-dotnet to see this part 
```

You might notice that dependent-dependency links form a 
[directed acyclic graph](https://en.wikipedia.org/wiki/Directed_acyclic_graph),
thus:
* There must be no cycles. Note, though, that the links are established
  between the instances, not the computations, so technically you're 
  totally fine to have e.g. recursive functions that return computed instances.
* If we draw the graph so that the least dependent (most used ones / low-level logic) 
  instances are at the bottom, and the most dependent ones (least used / high-level logic)
  are at the top,
  * Invalidation of a graph node also "spreads" to every node in its "upper subtree"
  * Computation of a graph node also "spreads" to every node in its "lower subtree". 


#### [Next: Part 3 &raquo;](./Part03.md) | [Tutorial Home](./README.md)
