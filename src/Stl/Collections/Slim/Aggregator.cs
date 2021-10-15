namespace Stl.Collections.Slim;

public delegate void Aggregator<TState, in TArg>(ref TState state, TArg arg);
public delegate void Aggregator<TState, in TArg1, in TArg2>(ref TState state, TArg1 arg1, TArg2 arg2);
public delegate void Aggregator<TState, in TArg1, in TArg2, in TArg3>(
    ref TState state, TArg1 arg1, TArg2 arg2, TArg3 arg3);
public delegate void Aggregator<TState, in TArg1, in TArg2, in TArg3, in TArg4>(
    ref TState state, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);
