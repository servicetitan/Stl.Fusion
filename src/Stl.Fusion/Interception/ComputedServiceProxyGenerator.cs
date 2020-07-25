using System;
using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Stl.Concurrency;
using Stl.Fusion.Interception.Internal;

namespace Stl.Fusion.Interception
{
    public interface IComputedServiceProxyGenerator
    {
        Type GetProxyType(Type type);
    }

    public class ComputedServiceProxyGenerator : IComputedServiceProxyGenerator
    {
        public static readonly IComputedServiceProxyGenerator Default = new ComputedServiceProxyGenerator();

        protected ConcurrentDictionary<Type, Type> Cache { get; }
        protected ProxyGenerationOptions Options { get; }
        protected ModuleScope ModuleScope { get; }

        public ComputedServiceProxyGenerator(
            ProxyGenerationOptions? options = null,
            ModuleScope? moduleScope = null)
        {
            options ??= new ProxyGenerationOptions();
            moduleScope ??= new ModuleScope();
            Options = options;
            ModuleScope = moduleScope;
            Cache = new ConcurrentDictionary<Type, Type>();
        }

        public virtual Type GetProxyType(Type type)
            => Cache.GetOrAddChecked(type, (type1, self) => {
                var generator = new ComputedServiceProxyGeneratorImpl(self.ModuleScope, type1);
                return generator.GenerateCode(Array.Empty<Type>(), self.Options);
            }, this);
    }
}
