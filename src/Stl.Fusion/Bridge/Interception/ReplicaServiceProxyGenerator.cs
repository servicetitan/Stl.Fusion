using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Generators.Emitters;
using Stl.CommandR.Interception;
using Stl.Concurrency;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Bridge.Interception;

public interface IReplicaServiceProxyGenerator
{
    Type GetProxyType(Type type, bool isCommandService);
}

public class ReplicaServiceProxyGenerator : ProxyGeneratorBase<ReplicaServiceProxyGenerator.Options>,
    IReplicaServiceProxyGenerator
{
    public class Options : ProxyGenerationOptions
    {
        public Type BaseType { get; set; } = typeof(object);
        public Type InterceptorType { get; set; } = typeof(ReplicaServiceInterceptor);
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

    protected ConcurrentDictionary<(Type, bool), Type> Cache { get; } = new();

    public ReplicaServiceProxyGenerator(
        Options options,
        ModuleScope? moduleScope = null)
        : base(options, moduleScope) { }

    public virtual Type GetProxyType(Type type, bool isCommandService)
        => Cache.GetOrAddChecked((type, isCommandService), (key, self) => {
            var (type1, isCommandService1) = key;
            var tInterfaces = isCommandService1
                ? new[] { typeof(IReplicaService), typeof(ICommandService) }
                : new[] { typeof(IReplicaService) };
            var generator = new Implementation(self.ModuleScope, type1, self.ProxyGeneratorOptions);
            return generator.GenerateCode(
                self.ProxyGeneratorOptions.BaseType,
                tInterfaces,
                self.ProxyGeneratorOptions);
        }, this);
}
