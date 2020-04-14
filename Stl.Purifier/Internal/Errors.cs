using System;

namespace Stl.Purifier.Internal
{
    public static class Errors
    {
        public static Exception WrongComputedState(
            ComputedState expectedState, ComputedState state)
            => new InvalidOperationException(
                $"Wrong Computed.State: expected {expectedState}, was {state}.");
        public static Exception WrongComputedState(ComputedState state)
            => new InvalidOperationException(
                $"Wrong Computed.State: {state}.");

        public static Exception NoCurrentComputed()
            => new NullReferenceException($"Computed.Current == null.");
        public static Exception CurrentComputedIsOfIncompatibleType(Type expectedType, Type actualType)
            => new NullReferenceException(
                $"Computed.Current.Value type is {actualType.Name}, " +
                $"but expected type is {expectedType.Name}.");
    }
}
