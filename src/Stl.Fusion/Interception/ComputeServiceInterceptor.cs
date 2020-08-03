using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Interception.Internal;
using Stl.Fusion.Internal;
using Stl.Generators;

namespace Stl.Fusion.Interception
{
    public class ComputeServiceInterceptor : InterceptorBase
    {
        public new class Options : InterceptorBase.Options
        {
            public Generator<LTag> LTagGenerator { get; set; } = ConcurrentLTagGenerator.Default;
        }

        protected Generator<LTag> LTagGenerator { get; }

        public ComputeServiceInterceptor(
            Options options,
            IComputedRegistry? registry = null,
            ILoggerFactory? loggerFactory = null)
            : base(options, registry, loggerFactory)
        {
            LTagGenerator = options.LTagGenerator;
        }

        protected override InterceptedFunctionBase<T> CreateFunction<T>(InterceptedMethod method)
        {
            var log = LoggerFactory.CreateLogger<ComputeServiceFunction<T>>();
            return new ComputeServiceFunction<T>(method, LTagGenerator, Registry, log);
        }

        protected override void ValidateTypeInternal(Type type)
        {
            Log.Log(ValidationLogLevel, $"Validating: '{type}':");
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.FlattenHierarchy;
            foreach (var method in type.GetMethods(bindingFlags)) {
                var attrs = method
                    .GetCustomAttributes(typeof(ComputeMethodAttribute), true)
                    .Cast<ComputeMethodAttribute>()
                    .ToArray();
                if (!attrs.Any())
                    continue; // No attributes
                if (method.IsStatic)
                    throw Errors.ComputeServiceMethodAttributeOnStaticMethod(method);
                if (!method.IsVirtual)
                    throw Errors.ComputeServiceMethodAttributeOnNonVirtualMethod(method);
                if (method.IsFinal)
                    // All implemented interface members are marked as "virtual final"
                    // unless they are truly virtual
                    throw Errors.ComputeServiceMethodAttributeOnNonVirtualMethod(method);
                if (attrs.Any(a => !a.IsEnabled)) {
                    Log.Log(ValidationLogLevel,
                        $"- {method}: has {nameof(ComputeMethodAttribute)}(false)");
                    continue;
                }
                var attr = attrs.FirstOrDefault();
                Log.Log(ValidationLogLevel,
                    $"+ {method}: {nameof(ComputeMethodAttribute)} {{ " +
                    $"{nameof(ComputeMethodAttribute.IsEnabled)} = {attr.IsEnabled}, " +
                    $"{nameof(ComputeMethodAttribute.KeepAliveTime)} = {attr.KeepAliveTime}" +
                    $" }}");
            }
        }
    }
}
