using System;
using System.Net.WebSockets;
using System.Reflection;

namespace Stl.Fusion.Internal
{
    public static class Errors
    {
        public static Exception MustImplement<TExpected>(Type serviceType)
            => new InvalidOperationException($"{serviceType.Name} must implement {typeof(TExpected).Name}.");

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
        public static Exception PublicationAbsents()
            => new InvalidOperationException("The Publication absents on the server.");

        public static Exception ReplicaHasNeverBeenUpdated()
            => new InvalidOperationException("The Replica has never been updated.");

        public static Exception WebSocketConnectTimeout()
            => new WebSocketException("Connection timeout.");

        public static Exception AlreadyStarted()
            => new InvalidOperationException("Already started.");

        public static Exception ComputedServiceMethodAttributeOnStaticMethod(MethodInfo method)
            => new InvalidOperationException($"{nameof(ComputedServiceMethodAttribute)} is applied to static method '{method}'.");
        public static Exception ComputedServiceMethodAttributeOnNonVirtualMethod(MethodInfo method)
            => new InvalidOperationException($"{nameof(ComputedServiceMethodAttribute)} is applied to non-virtual method '{method}'.");

        public static Exception UnsupportedReplicaType(Type replicaType)
            => new NotSupportedException(
                $"IReplica<{replicaType.Name}> isn't supported by the current client, " +
                $"most likely because there is no good way to intercept the deserialization " +
                $"of results of this type.");
    }
}
