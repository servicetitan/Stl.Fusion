using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Stl.Fusion.Interception
{
    public interface IArgumentComparerProvider
    {
        ArgumentComparer GetInvocationTargetComparer(MethodInfo methodInfo, Type invocationTargetType);
        ArgumentComparer GetArgumentComparer(MethodInfo methodInfo, ParameterInfo parameterInfo);
    }

    public class ArgumentComparerProvider : IArgumentComparerProvider
    {
        protected static readonly IReadOnlyDictionary<Type, ArgumentComparer> DefaultComparers = 
            new Dictionary<Type, ArgumentComparer>() {
                {typeof(CancellationToken), ArgumentComparer.Ignore},
            };

        public static readonly IArgumentComparerProvider Default = new ArgumentComparerProvider();

        private readonly Dictionary<Type, ArgumentComparer> _comparers;

        public ArgumentComparerProvider()
        {
            _comparers = new Dictionary<Type, ArgumentComparer>(DefaultComparers);
        }

        public ArgumentComparerProvider(Dictionary<Type, ArgumentComparer> comparers)
        {
            foreach (var (key, value) in DefaultComparers)
                comparers[key] = value;
            _comparers = comparers;
        }

        public ArgumentComparer GetInvocationTargetComparer(MethodInfo methodInfo, Type invocationTargetType) 
            => GetArgumentComparer(invocationTargetType, true);

        public virtual ArgumentComparer GetArgumentComparer(MethodInfo methodInfo, ParameterInfo parameterInfo)
            => GetArgumentComparer(parameterInfo.ParameterType);

        public virtual ArgumentComparer GetArgumentComparer(Type type, bool isInvocationTarget = false)
        {
            if (_comparers.TryGetValue(type, out var comparer))
                return comparer;
            var bType = type.BaseType;
            while (bType != null) {
                if (_comparers.TryGetValue(bType, out comparer))
                    return comparer;
                bType = bType.BaseType;
            }
            var cType = (Type?) null;
            var cComparer = (ArgumentComparer?) null;
            foreach (var iType in type.GetInterfaces()) {
                if (_comparers.TryGetValue(iType, out comparer)) {
                    if (cType == null || iType.IsAssignableFrom(cType)) {
                        // We're looking for the most specific type here
                        cType = iType;
                        cComparer = comparer;
                    }
                }
            }

            if (comparer != null)
                return comparer;
            if (isInvocationTarget)
                return ArgumentComparer.ByRef;
            var equatableType = typeof(IEquatable<>).MakeGenericType(type);
            if (equatableType.IsAssignableFrom(type)) {
                var eacType = typeof(EquatableArgumentComparer<>).MakeGenericType(type);
                var eac = (EquatableArgumentComparer) eacType
                    .GetField(nameof(EquatableArgumentComparer<int>.Instance))
                    .GetValue(null);
                if (eac.IsAvailable)
                    return eac;
            }
            return ArgumentComparer.Default;
        }
    }
}
