using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Stl.Purifier.Autofac
{
    public interface IArgumentComparerProvider
    {
        ArgumentComparer GetComparer(MethodInfo methodInfo, ParameterInfo parameterInfo);
    }

    public class ArgumentComparerProvider : IArgumentComparerProvider
    {
        protected static readonly IReadOnlyDictionary<Type, ArgumentComparer> DefaultComparers = 
            new Dictionary<Type, ArgumentComparer>() {
                {typeof(CancellationToken), ArgumentComparer.Ignore},
            };

        public static readonly ArgumentComparerProvider Default = new ArgumentComparerProvider();

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

        public virtual ArgumentComparer GetComparer(MethodInfo methodInfo, ParameterInfo parameterInfo)
            => _comparers.TryGetValue(parameterInfo.ParameterType, out var comparer)
                ? comparer
                : ArgumentComparer.Default;
    }
}
