using System;
using System.Reflection;
using Stl.Extensibility;
using Stl.Reflection;

namespace Stl.Fusion.Interception
{
    public interface IArgumentComparerProvider
    {
        ArgumentComparer GetInvocationTargetComparer(MethodInfo methodInfo, Type invocationTargetType);
        ArgumentComparer GetArgumentComparer(MethodInfo methodInfo, ParameterInfo parameterInfo);
    }

    public class ArgumentComparerProvider : IArgumentComparerProvider
    {
        public class Options
        {
            public IMatchingTypeFinder MatchingTypeFinder { get; set; } = null!;
        }

        public static readonly IArgumentComparerProvider Default = new ArgumentComparerProvider();
        protected IMatchingTypeFinder MatchingTypeFinder { get; }
        protected IServiceProvider? Services { get; }

        public ArgumentComparerProvider(
            Options? options = null,
            IServiceProvider? services = null)
        {
            options ??= new Options() {
                MatchingTypeFinder = new MatchingTypeFinder(GetType().Assembly),
            };
            MatchingTypeFinder = options.MatchingTypeFinder;
            Services = services;
        }

        public ArgumentComparer GetInvocationTargetComparer(MethodInfo methodInfo, Type invocationTargetType)
            => GetArgumentComparer(invocationTargetType, true);

        public virtual ArgumentComparer GetArgumentComparer(MethodInfo methodInfo, ParameterInfo parameterInfo)
            => GetArgumentComparer(parameterInfo.ParameterType);

        public virtual ArgumentComparer GetArgumentComparer(Type type, bool isInvocationTarget = false)
        {
            var comparerType = MatchingTypeFinder.TryFind(type, typeof(ArgumentComparerProvider));
            if (comparerType != null)
                return CreateComparer(comparerType);

            if (isInvocationTarget)
                return ByRefArgumentComparer.Instance;
            var equatableType = typeof(IEquatable<>).MakeGenericType(type);
            if (equatableType.IsAssignableFrom(type)) {
                var eacType = typeof(EquatableArgumentComparer<>).MakeGenericType(type);
                var eac = (EquatableArgumentComparer) CreateComparer(eacType);
                if (eac.IsAvailable)
                    return eac;
            }
            return ArgumentComparer.Default;
        }

        protected virtual ArgumentComparer CreateComparer(Type comparerType)
        {
            var pInstance = comparerType.GetProperty(
                nameof(ByRefArgumentComparer.Instance),
                BindingFlags.Static | BindingFlags.Public);
            if (pInstance != null)
                return (ArgumentComparer) pInstance.GetValue(null);

            var fInstance = comparerType.GetField(
                nameof(ByRefArgumentComparer.Instance),
                BindingFlags.Static | BindingFlags.Public);
            if (fInstance != null)
                return (ArgumentComparer) fInstance.GetValue(null);

            if (Services != null)
                return (ArgumentComparer) Services.Activate(comparerType);
            return (ArgumentComparer) comparerType.CreateInstance();
        }
    }
}
