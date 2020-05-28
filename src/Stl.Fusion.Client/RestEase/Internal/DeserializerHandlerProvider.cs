using System;
using System.Linq;
using System.Net.Http;
using RestEase;
using Stl.Extensibility;
using Stl.Fusion.Bridge;
using Stl.Reflection;

namespace Stl.Fusion.Client.RestEase.Internal
{
    internal class DeserializeComputedHandlerProvider
    {
        public static readonly HandlerProvider<DeserializeMethodArgs, object?> Instance = 
            new HandlerProvider<DeserializeMethodArgs, object?>(CreateHandler);

        private static HandlerProvider<DeserializeMethodArgs, object?>.IHandler CreateHandler(Type forType)
        {
            var handlerType = typeof(DeserializeComputedHandler<>);
            object handler;
            var forTypeGeneric = forType.GetGenericTypeDefinition();
            if (forTypeGeneric != typeof(IComputed<>) && forTypeGeneric != typeof(IComputedReplica<>)) {
                // This will mark the handler as "wrong", which in turn
                // will enable deserializer to switch to a regular deserialization
                // flow.
                var wrongType = typeof(DeserializeComputedHandlerProvider);
                handler = handlerType.MakeGenericType(wrongType).CreateInstance();
            }
            else {
                var forTypeTArg = forType.GetGenericArguments().Single();
                handler = handlerType.MakeGenericType(forTypeTArg).CreateInstance();
            }
            return (HandlerProvider<DeserializeMethodArgs, object?>.IHandler) handler;
        }
    }
}
