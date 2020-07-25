using System;
using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Stl.Concurrency;
using Stl.Fusion.Bridge.Interception.Internal;

namespace Stl.Fusion.Bridge.Interception
{
    public interface IReplicaServiceProxyGenerator
    {
        Type GetProxyType(Type type);
    }

    public class ReplicaServiceProxyGenerator : IReplicaServiceProxyGenerator
    {
        public static readonly IReplicaServiceProxyGenerator Default = new ReplicaServiceProxyGenerator();

        protected ConcurrentDictionary<Type, Type> Cache { get; }
        protected ProxyGenerationOptions Options { get; }
        protected ModuleScope ModuleScope { get; }

        public ReplicaServiceProxyGenerator(
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
                var generator = new ReplicaServiceProxyGeneratorImpl(self.ModuleScope, type1);
                var baseType = typeof(object);
                return generator.GenerateCode(baseType, Array.Empty<Type>(), self.Options);
            }, this);
    }
}
