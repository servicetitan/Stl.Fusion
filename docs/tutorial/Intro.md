# Stl.Fusion - Conceptual Introduction

## The story behind Stl.Fusion

TODO: Write the story of how I came up with the concept :)

## What is "real-time application"? 

The question seems quite simple, right? Real-time apps are the apps
that update the content user sees in real-time. 

Wait, did I just explained "real-time" using another "real-time"?

Ok, let's try to come up with a more precise definition:
* If an app almost always requires an explicit user action to update the content
  user sees, it's *clearly not real-time*.
* On contrary, if the content displayed to the user stays always up-to-date, 
  even if user doesn't take actions, it's *possibly a real-time app*.
  
"Possibly" above could imply a huge difference:
* The app that simply refreshes the page every minute doesn't seem to be
  even close to real-time.
* On contrary, apps like Facebook, Quora, Slack, and lots of others - in fact,
  the ones that almost instantly display at least the most critical updates -
  are considered real-time.
  
Let's define this in scientific terms:
* Let's assume the content user sees (the UI) is produced by some function 
  (which is usually the case)
* And the output of this function changes over time - mostly, due to changes
  in state that this function uses to produce the content.
  
So how UI update pseudo-code could look like:

```
ui_state = function(app_state, client_state)
await any(app_state.change(), client_state.change())  
ui_state = function(app_state, client_state)
```  

Now, we want to make sure that `ui_state` gets "in sync" with the `app_state`
and the `client_state` as quickly as possible once any of them changes.

Does this problem sound familiar to you? 

Yeah, **"real-time" is all about eventual consistency and caching**. Compare
what I just described with a pseudo-code that tries to invalidate some cached
value in real time:
```
cache[(function, arguments)] = function(app_state, arguments)
await app_state.change()
cache[(function, arguments)] = <none>  
```  

Long story short, you may think about the UI presented to the user
(or its state / model) as a value that's cached remotely on the client,
and your goal is to "invalidate" it automatically once the data it depends 
on changes. The code on the client may react to the invalidation with
by either immediate or delayed update request.

So since we "reduced" this problem to cache invalidation, let's talk a bit
more about it.

## Caching, invalidation and eventual consistency

Quick recap of what consistency and caching is:

1. **Consisntency** is the state when the values observed satisfy the
   relationship rules defined for them. Even though relationships are
   predicates (or assertions) about the values, the most common relationship
   probably looks like `x == fn(a, b)`, i.e. it says `x` is always
   an output of some function `fn` applied to `(a, b)`.

2. Consistency can be **partial** - e.g. you can say that triplet `(x, a, b)`
   is in consistent state for all the relationships defined for it, but
   it's not true for some other values and/or relationships. In short,
   "consistency" always implies some scope of it, and this scope can be as
   narrow as a single value or a consistency rule.

3. Consistency can be **eventual** - this is a fancy way of saying that
   if you'll leave the system "untouched" (i.e. won't introduce new changes), 
   at some future point it will be in a consistent state. Obviously, eventual
   consistency can be partial too.
   
4. **Any non-malfunctioning system is at least eventually consistent**.
   Being worse then eventually consistent is the same as "being prone
   to a failure you won't recover from". In short, it's a property of
   malfunctioning / broken system.
   
5. "Caching" is just a fancy way of saying "we store the results of 
   computations somewhere and reuse them without running the actual
   computation again". 
   * Typically "caching" implies use of high-performance key-value 
     stores with some built-in invalidation policies (LRU, timer-based 
     expiration, etc.), but...
   * If we define "caching" broadly, even storing the data in CPU
     register is an example of caching. Further I'll be using
     "caching" mostly in this sense, i.e. implying it is a "reuse 
     of previously stored computation results w/o running a computation
     again".
     
Now, let's say we have two systems, A and B, and both are eventually
consistent. Are they equally good? No. The biggest differentiating factor 
between eventually consistent systems is inconsistency period:
* If it's tiny (milliseconds?), this state is quite similar to 
  absence of caching at all, because most of the reads are consistent. 
  So e.g. you can optimistically ignore the inconsistencies on reads,
  and check for them only when you apply the updates.
* On contrary, large inconsistency periods are quite painful -
  you have to take them into account everywhere, including the
  code that reads the data.

Long story short, we want tiny inconsistency periods. But wait...
If we look at what most caches offer, there are just two ways of
controlling this:
* Setting entry expiration time  
* Evicting the entry manually. 

The first option is easy to code, but has a huge trade-off:
* Tiny expiration time gives you smaller inconsistency periods,
  but simultaneously, it decreases your cache hit rate.
* And on contrary, large expiration time can give you a good cache
  hit ratio, but much longer inconsistency periods might turn
  into a huge pain.

![](../img/InconsistencyPeriod.gif)
      
So here is the "grand plan":
* Real-time UI updates <=> cache consistency problem
* Assuming we care only about `x == f(...)`-style consistency rules,
  we need something that will tell us when an output of a certain function
  changes -- as quickly as possible.      

... To be continued.
