using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Text;

namespace Stl.Fusion.Internal
{
    public static class Errors
    {
        public static Exception TypeMustBeOpenGenericType(Type type)
            => new InvalidOperationException($"'{type}' must be open generic type.");
        public static Exception MustHaveASingleGenericArgument(Type type)
            => new InvalidOperationException($"'{type}' must have a single generic argument.");

        public static Exception WrongComputedState(
            ConsistencyState expectedState, ConsistencyState state)
            => new InvalidOperationException(
                $"Wrong Computed.State: expected {expectedState}, was {state}.");
        public static Exception WrongComputedState(ConsistencyState state)
            => new InvalidOperationException(
                $"Wrong Computed.State: {state}.");

        public static Exception ComputedCurrentIsNull()
            => new NullReferenceException($"Computed.Current() == null.");
        public static Exception ComputedCurrentIsOfIncompatibleType(Type expectedType)
            => new InvalidCastException(
                $"Computed.Current() can't be converted to '{expectedType.Name}'.");
        public static Exception CapturedComputedIsOfIncompatibleType(Type expectedType)
            => new InvalidCastException(
                $"Computed.Captured() can't be converted to '{expectedType.Name}'.");
        public static Exception NoComputedCaptured()
            => new InvalidOperationException($"No {nameof(IComputed)} was captured.");

        public static Exception WrongPublisher(IPublisher expected, Symbol providedPublisherId)
            => new InvalidOperationException($"Wrong publisher: {expected.Id} (expected) != {providedPublisherId} (provided).");
        public static Exception UnknownChannel(Channel<BridgeMessage> channel)
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

        public static Exception UnsupportedComputedOptions(Type unsupportedBy)
            => new NotSupportedException($"Specified {nameof(ComputedOptions)} aren't supported by '{unsupportedBy}'.");
        public static Exception OutputIsUnloaded()
            => new InvalidOperationException($"{nameof(IAsyncComputed.MaybeOutput)} is unloaded.");
    }
}
