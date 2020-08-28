using System.Runtime.CompilerServices;
using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public enum ConsistencyState
    {
        Computing = 0,
        Consistent,
        Invalidated,
    }

    public interface IHasConsistencyState
    {
        ConsistencyState ConsistencyState { get; }
    }

    public static class ConsistencyStateEx
    {
        // IsXxx

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInvalidated(this ConsistencyState state)
            => state == ConsistencyState.Invalidated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConsistent(this ConsistencyState state)
            => state == ConsistencyState.Consistent;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConsistentOrComputing(this ConsistencyState state)
            => state != ConsistencyState.Invalidated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInvalidated(this IHasConsistencyState hasConsistencyState)
            => hasConsistencyState.ConsistencyState == ConsistencyState.Invalidated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConsistent(this IHasConsistencyState hasConsistencyState)
            => hasConsistencyState.ConsistencyState == ConsistencyState.Consistent;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConsistentOrComputing(this IHasConsistencyState hasConsistencyState)
            => hasConsistencyState.ConsistencyState != ConsistencyState.Invalidated;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertConsistencyStateIs(this IHasConsistencyState hasConsistencyState, ConsistencyState expectedState)
        {
            if (hasConsistencyState.ConsistencyState != expectedState)
                throw Errors.WrongComputedState(expectedState, hasConsistencyState.ConsistencyState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertConsistencyStateIsNot(this IHasConsistencyState hasConsistencyState, ConsistencyState unexpectedState)
        {
            if (hasConsistencyState.ConsistencyState == unexpectedState)
                throw Errors.WrongComputedState(hasConsistencyState.ConsistencyState);
        }
    }
}
