using System;
using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Generators.Emitters;
using Stl.Concurrency;
using Stl.DependencyInjection;
using Stl.DependencyInjection.Internal;

namespace Stl.Fusion.Interception
{
    public interface IComputeServiceProxyGenerator
    {
        Type GetProxyType(Type type);
    }

    public class ComputeServiceProxyGenerator : ProxyGeneratorBase<ComputeServiceProxyGenerator.Options>,
        IComputeServiceProxyGenerator
    {
        public class Options : ProxyGenerationOptions
        {
            public Type InterceptorType { get; set; } = typeof(ComputeServiceInterceptor);
        }

        protected class Implementation : ClassProxyGenerator
        {
            protected Options Options { get; }

            public Implementation(ModuleScope scope, Type @interface, Options options)
                : base(scope, @interface)
                => Options = options;

            protected override void CreateFields(ClassEmitter emitter)
            {
                CreateOptionsField(emitter);
                CreateSelectorField(emitter);
                CreateInterceptorsField(emitter);
            }

            protected new void CreateInterceptorsField(ClassEmitter emitter)
                => emitter.CreateField("__interceptors", Options.InterceptorType.MakeArrayType());
        }

        public static readonly IComputeServiceProxyGenerator Default = new ComputeServiceProxyGenerator();

        protected ConcurrentDictionary<Type, Type> Cache { get; } = new();

        public ComputeServiceProxyGenerator(
            Options? options = null,
            ModuleScope? moduleScope = null)
            : base(options ??= new(), moduleScope) { }

        public virtual Type GetProxyType(Type type)
            => Cache.GetOrAddChecked(type, (type1, self) => {
                var generator = new Implementation(self.ModuleScope, type1, self.ProxyGeneratorOptions);
                return generator.GenerateCode(Array.Empty<Type>(), self.ProxyGeneratorOptions);
            }, this);
    }
}
