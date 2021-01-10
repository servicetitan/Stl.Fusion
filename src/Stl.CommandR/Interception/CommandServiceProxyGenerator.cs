using System;
using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using Castle.DynamicProxy.Generators.Emitters;
using Stl.Concurrency;
using Stl.Interception.Interceptors;

namespace Stl.CommandR.Interception
{
    public interface ICommandServiceProxyGenerator
    {
        Type GetProxyType(Type type);
    }

    public class CommandServiceProxyGenerator : ProxyGeneratorBase<CommandServiceProxyGenerator.Options>,
        ICommandServiceProxyGenerator
    {
        public class Options : ProxyGenerationOptions
        {
            public Type InterceptorType { get; set; } = typeof(CommandServiceInterceptor);
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

        public static readonly ICommandServiceProxyGenerator Default = new CommandServiceProxyGenerator();

        protected ConcurrentDictionary<Type, Type> Cache { get; } = new();

        public CommandServiceProxyGenerator(
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
