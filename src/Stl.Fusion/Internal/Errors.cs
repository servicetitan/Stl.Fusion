using System;

namespace Stl.Fusion.Internal
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

        public static Exception ComputedCurrentIsNull()
            => new NullReferenceException($"Computed.Current() == null.");
        public static Exception ComputedCurrentIsOfIncompatibleType(Type expectedType)
            => new InvalidCastException(
                $"Computed.Current() can't be converted to '{expectedType.Name}'.");
        public static Exception ComputedCapturedIsOfIncompatibleType(Type expectedType)
            => new InvalidCastException(
                $"Computed.Captured() can't be converted to '{expectedType.Name}'.");
        public static Exception NoComputedCaptured()
            => new InvalidOperationException("No IComputed was captured.");

        public static Exception PublicationTypeMustBeOpenGenericType(string paramName)
            => new ArgumentOutOfRangeException(paramName, "Publication type must be open generic type.");

        public static Exception ReplicaHasNeverBeenUpdated()
            => new InvalidOperationException("The replica has never been updated.");

        public static Exception CouldNotUpdateReplica()
            => new InvalidOperationException("Couldn't update replica. Likely, the connection to Publisher is down.");
    }
}
