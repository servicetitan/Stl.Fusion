using Stl.Fusion.Internal;

namespace Stl.Fusion;

public enum ConsistencyState
{
    Computing = 0,
    Consistent,
    Invalidated,
}

public static class ConsistencyStateExt
{
    // IsXxx

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInvalidated(this ConsistencyState state)
        => state == ConsistencyState.Invalidated;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConsistent(this ConsistencyState state)
        => state == ConsistencyState.Consistent;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComputing(this ConsistencyState state)
        => state == ConsistencyState.Computing;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConsistentOrComputing(this ConsistencyState state)
        => state != ConsistencyState.Invalidated;

    // AssertXxx

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertIs(this ConsistencyState state, ConsistencyState expectedState)
    {
        if (state != expectedState)
            throw Errors.WrongComputedState(expectedState, state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertIsNot(this ConsistencyState state, ConsistencyState unexpectedState)
    {
        if (state == unexpectedState)
            throw Errors.WrongComputedState(state);
    }
}
