using System;
using System.Linq;
using Stl.Extensibility;
using Stl.Reflection;

namespace Stl.Fusion.Client.RestEase.Internal
{
    internal class DeserializeComputedHandlerProvider
    {
        public static readonly HandlerProvider<DeserializeMethodArgs, object?> Instance = 
            new HandlerProvider<DeserializeMethodArgs, object?>(CreateHandler);
        private static readonly Type HandlerType = typeof(DeserializeComputedHandler<>);

        private static HandlerProvider<DeserializeMethodArgs, object?>.IHandler CreateHandler(Type forType)
        {
            object handler;
            if (!forType.IsGenericType || forType.GetGenericTypeDefinition() != typeof(IComputed<>))
                handler = HandlerType.MakeGenericType(forType).CreateInstance();
            else {
                var forTypeTArg = forType.GetGenericArguments().Single();
                handler = HandlerType.MakeGenericType(forTypeTArg).CreateInstance();
            }
            return (HandlerProvider<DeserializeMethodArgs, object?>.IHandler) handler;
        }
    }
}
