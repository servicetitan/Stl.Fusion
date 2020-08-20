using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Caching;
using Stl.Text;

namespace Stl.Fusion.Internal
{
    public static class Errors
    {
        public static Exception MustImplement<TExpected>(Type type, string? argumentName = null)
        {
            var message = $"{type.Name} must implement {typeof(TExpected).Name}.";
            return string.IsNullOrEmpty(argumentName)
                ? (Exception) new InvalidOperationException(message)
                : new ArgumentOutOfRangeException(argumentName, message);
        }
        public static Exception TypeMustBeOpenGenericType(Type type)
            => new InvalidOperationException($"'{type}' must be open generic type.");

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
            => new InvalidOperationException($"No {nameof(IComputed)} was captured.");

        public static Exception WrongPublisher(IPublisher expected, Symbol providedPublisherId)
            => new InvalidOperationException($"Wrong publisher: {expected.Id} (expected) != {providedPublisherId} (provided).");
        public static Exception UnknownChannel(Channel<Message> channel)
            => new InvalidOperationException("Unknown channel.");

        public static Exception PublicationAbsents()
            => new InvalidOperationException("The Publication absents on the server.");
        public static Exception NoPublicationStateInfoCaptured()
            => new InvalidOperationException($"No {nameof(PublicationStateInfo)} was captured.");

        public static Exception ReplicaHasNeverBeenUpdated()
            => new InvalidOperationException("The Replica has never been updated.");

        public static Exception WebSocketConnectTimeout()
            => new WebSocketException("Connection timeout.");

        public static Exception ComputeServiceMethodAttributeOnStaticMethod(MethodInfo method)
            => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to static method '{method}'.");
        public static Exception ComputeServiceMethodAttributeOnNonVirtualMethod(MethodInfo method)
            => new InvalidOperationException($"{nameof(ComputeMethodAttribute)} is applied to non-virtual method '{method}'.");

        public static Exception UnsupportedReplicaType(Type replicaType)
            => new NotSupportedException(
                $"IReplica<{replicaType.Name}> isn't supported by the current client, " +
                $"most likely because there is no good way to intercept the deserialization " +
                $"of results of this type.");

        public static Exception UnsupportedRequiresCachingComputed()
            => new NotSupportedException($"{nameof(ICachingComputed)} is required to perform this operation.");
        public static Exception OutputIsAlreadyReleased()
            => new InvalidOperationException($"{nameof(ICachingComputed.MaybeOutput)} is already released.");
    }
}
