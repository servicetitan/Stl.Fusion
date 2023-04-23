using Stl.Fusion.Internal;

namespace Stl.Fusion;

public static partial class ComputedExt
{
    // IComputed overloads

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInvalidated(this IComputed computed)
        => computed.ConsistencyState == ConsistencyState.Invalidated;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConsistent(this IComputed computed)
        => computed.ConsistencyState == ConsistencyState.Consistent;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComputing(this IComputed computed)
        => computed.ConsistencyState == ConsistencyState.Computing;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConsistentOrComputing(this IComputed computed)
        => computed.ConsistencyState != ConsistencyState.Invalidated;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertConsistencyStateIs(this IComputed computed, ConsistencyState expectedState)
    {
        if (computed.ConsistencyState != expectedState)
            throw Errors.WrongComputedState(expectedState, computed.ConsistencyState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertConsistencyStateIsNot(this IComputed computed, ConsistencyState unexpectedState)
    {
        if (computed.ConsistencyState == unexpectedState)
            throw Errors.WrongComputedState(computed.ConsistencyState);
    }

    // IComputed<T> overloads

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInvalidated<T>(this Computed<T> computed)
        => computed.ConsistencyState == ConsistencyState.Invalidated;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConsistent<T>(this Computed<T> computed)
        => computed.ConsistencyState == ConsistencyState.Consistent;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComputing<T>(this Computed<T> computed)
        => computed.ConsistencyState == ConsistencyState.Computing;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConsistentOrComputing<T>(this Computed<T> computed)
        => computed.ConsistencyState != ConsistencyState.Invalidated;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertConsistencyStateIs<T>(this Computed<T> computed, ConsistencyState expectedState)
    {
        if (computed.ConsistencyState != expectedState)
            throw Errors.WrongComputedState(expectedState, computed.ConsistencyState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertConsistencyStateIsNot<T>(this Computed<T> computed, ConsistencyState unexpectedState)
    {
        if (computed.ConsistencyState == unexpectedState)
            throw Errors.WrongComputedState(computed.ConsistencyState);
    }
}
