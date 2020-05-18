using System;
using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Stl.Fusion.Interception.Internal;
using Stl.Reflection;

namespace Stl.Fusion.Interception
{
    public interface IComputedProxyGenerator
    {
        Type GetProxyType(Type type);
    }

    public class ComputedProxyGenerator : IComputedProxyGenerator
    {
        public static readonly IComputedProxyGenerator Default = new ComputedProxyGenerator();

        protected ConcurrentDictionary<Type, Type> Cache { get; }
        protected ProxyGenerationOptions Options { get;  }
        protected ModuleScope ModuleScope { get; }

        public ComputedProxyGenerator(
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
                var generator = new ComputedClassProxyGenerator(ModuleScope, type1);
                return generator.GenerateCode(Array.Empty<Type>(), Options);
            }, this);
    }
}
