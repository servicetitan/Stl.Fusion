using System;
using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Generators.Emitters;
using Stl.Concurrency;
using Stl.DependencyInjection;
using Stl.DependencyInjection.Internal;

namespace Stl.Fusion.Bridge.Interception
{
    public interface IReplicaClientProxyGenerator
    {
        Type GetProxyType(Type type);
    }

    public class ReplicaClientProxyGenerator : ProxyGeneratorBase<ReplicaClientProxyGenerator.Options>,
        IReplicaClientProxyGenerator
    {
        public class Options : ProxyGenerationOptions, IOptions
        {
            public Type BaseType { get; set; } = typeof(object);
            public Type InterceptorType { get; set; } = typeof(ReplicaClientInterceptor);
        }

        protected class Implementation : InterfaceProxyWithTargetInterfaceGenerator
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

        public static readonly IReplicaClientProxyGenerator Default = new ReplicaClientProxyGenerator();

        protected ConcurrentDictionary<Type, Type> Cache { get; } = new ConcurrentDictionary<Type, Type>();

        public ReplicaClientProxyGenerator(
            Options? options = null,
            ModuleScope? moduleScope = null)
            : base(options, moduleScope) { }

        public virtual Type GetProxyType(Type type)
            => Cache.GetOrAddChecked(type, (type1, self) => {
                var generator = new Implementation(self.ModuleScope, type1, self.ProxyGeneratorOptions);
                return generator.GenerateCode(
                    self.ProxyGeneratorOptions.BaseType,
                    Array.Empty<Type>(),
                    self.ProxyGeneratorOptions);
            }, this);
    }
}
