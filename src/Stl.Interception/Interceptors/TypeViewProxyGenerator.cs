using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Generators.Emitters;
using Stl.Concurrency;

namespace Stl.Interception.Interceptors;

public interface ITypeViewProxyGenerator
{
    Type GetProxyType(Type implementationType, Type viewType);
}

public class TypeViewProxyGenerator : ProxyGeneratorBase<TypeViewProxyGenerator.Options>,
    ITypeViewProxyGenerator
{
    public class Options : ProxyGenerationOptions
    {
        public Type GenericBaseType { get; set; } = typeof(TypeView<,>);
        public Type InterceptorType { get; set; } = typeof(TypeViewInterceptor);
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

    protected ConcurrentDictionary<(Type, Type), Type> Cache { get; } = new();

    public TypeViewProxyGenerator(
        Options options,
        ModuleScope? moduleScope = null)
        : base(options, moduleScope) { }

    public virtual Type GetProxyType(Type implementationType, Type viewType)
        => Cache.GetOrAddChecked((implementationType, viewType), (key, self) => {
            var (tImpl, tView) = key;
            var options = MemberwiseCloner.Invoke(self.ProxyGeneratorOptions);
            options.BaseTypeForInterfaceProxy = options.GenericBaseType
                .MakeGenericType(tImpl, tView);
            var generator = new Implementation(self.ModuleScope, tView, options);
            var proxyType = generator.GenerateCode(typeof(object), Array.Empty<Type>(), options);
            return proxyType;
        }, this);
}
