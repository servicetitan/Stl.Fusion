using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Stl.DependencyInjection;
using Stl.Fusion.Internal;
using Stl.Generators;

namespace Stl.Fusion.Interception
{
    public class ComputeServiceInterceptor : InterceptorBase
    {
        public new class Options : InterceptorBase.Options
        {
            public Generator<LTag> VersionGenerator { get; set; } = ConcurrentLTagGenerator.Default;
        }

        protected readonly Generator<LTag> VersionGenerator;

        public ComputeServiceInterceptor(
            Options? options,
            IServiceProvider serviceProvider,
            ILoggerFactory? loggerFactory = null)
            : base(options = options.OrDefault(serviceProvider), serviceProvider, loggerFactory)
            => VersionGenerator = options.VersionGenerator;

        protected override InterceptedFunctionBase<T> CreateFunction<T>(InterceptedMethodDescriptor method)
        {
            var log = LoggerFactory.CreateLogger<ComputeServiceFunction<T>>();
            if (method.Options.IsAsyncComputed)
                return new AsyncComputeServiceFunction<T>(method, VersionGenerator, ServiceProvider, log);
            return new ComputeServiceFunction<T>(method, VersionGenerator, ServiceProvider, log);
        }

        protected override void ValidateTypeInternal(Type type)
        {
            Log.Log(ValidationLogLevel, $"Validating: '{type}':");
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.FlattenHierarchy;
            foreach (var method in type.GetMethods(bindingFlags)) {
                if (!(GetInterceptedMethodAttribute(method) is ComputeMethodAttribute attr))
                    continue;
                if (method.IsStatic)
                    throw Errors.ComputeServiceMethodAttributeOnStaticMethod(method);
                if (!method.IsVirtual)
                    throw Errors.ComputeServiceMethodAttributeOnNonVirtualMethod(method);
                if (method.IsFinal)
                    // All implemented interface members are marked as "virtual final"
                    // unless they are truly virtual
                    throw Errors.ComputeServiceMethodAttributeOnNonVirtualMethod(method);
                if (!attr.IsEnabled) {
                    Log.Log(ValidationLogLevel,
                        $"- {method}: has {nameof(ComputeMethodAttribute)}(false)");
                    continue;
                }
                Log.Log(ValidationLogLevel,
                    $"+ {method}: {nameof(ComputeMethodAttribute)} {{ " +
                    $"{nameof(ComputeMethodAttribute.IsEnabled)} = {attr.IsEnabled}, " +
                    $"{nameof(ComputeMethodAttribute.KeepAliveTime)} = {attr.KeepAliveTime}" +
                    $" }}");
            }
        }
    }
}
