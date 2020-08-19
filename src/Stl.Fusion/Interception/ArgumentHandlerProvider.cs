using System;
using System.Reflection;
using Stl.DependencyInjection;
using Stl.Extensibility;
using Stl.Reflection;

namespace Stl.Fusion.Interception
{
    public interface IArgumentHandlerProvider
    {
        ArgumentHandler GetInvocationTargetHandler(MethodInfo methodInfo, Type invocationTargetType);
        ArgumentHandler GetArgumentHandler(MethodInfo methodInfo, ParameterInfo parameterInfo);
    }

    public class ArgumentHandlerProvider : IArgumentHandlerProvider
    {
        public class Options
        {
            public IMatchingTypeFinder MatchingTypeFinder { get; set; } = null!;
        }

        public static readonly IArgumentHandlerProvider Default = new ArgumentHandlerProvider();
        protected IMatchingTypeFinder MatchingTypeFinder { get; }
        protected IServiceProvider? Services { get; }

        public ArgumentHandlerProvider(
            Options? options = null,
            IServiceProvider? services = null)
        {
            options ??= new Options() {
                MatchingTypeFinder = new MatchingTypeFinder(GetType().Assembly),
            };
            MatchingTypeFinder = options.MatchingTypeFinder;
            Services = services;
        }

        public ArgumentHandler GetInvocationTargetHandler(MethodInfo methodInfo, Type invocationTargetType)
            => GetArgumentComparer(invocationTargetType, true);

        public virtual ArgumentHandler GetArgumentHandler(MethodInfo methodInfo, ParameterInfo parameterInfo)
            => GetArgumentComparer(parameterInfo.ParameterType);

        public virtual ArgumentHandler GetArgumentComparer(Type type, bool isInvocationTarget = false)
        {
            var handlerType = MatchingTypeFinder.TryFind(type, typeof(ArgumentHandlerProvider));
            if (handlerType != null)
                return CreateHandler(handlerType);

            if (isInvocationTarget)
                return ByRefArgumentHandler.Instance;
            var equatableType = typeof(IEquatable<>).MakeGenericType(type);
            if (equatableType.IsAssignableFrom(type)) {
                var eacType = typeof(EquatableArgumentHandler<>).MakeGenericType(type);
                var eac = (EquatableArgumentHandler) CreateHandler(eacType);
                if (eac.IsAvailable)
                    return eac;
            }
            return ArgumentHandler.Default;
        }

        protected virtual ArgumentHandler CreateHandler(Type comparerType)
        {
            var pInstance = comparerType.GetProperty(
                nameof(ByRefArgumentHandler.Instance),
                BindingFlags.Static | BindingFlags.Public);
            if (pInstance != null)
                return (ArgumentHandler) pInstance.GetValue(null);

            var fInstance = comparerType.GetField(
                nameof(ByRefArgumentHandler.Instance),
                BindingFlags.Static | BindingFlags.Public);
            if (fInstance != null)
                return (ArgumentHandler) fInstance.GetValue(null);

            if (Services != null)
                return (ArgumentHandler) Services.Activate(comparerType);
            return (ArgumentHandler) comparerType.CreateInstance();
        }
    }
}
