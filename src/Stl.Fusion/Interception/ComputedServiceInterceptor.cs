using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stl.Concurrency;
using Stl.Fusion.Interception.Internal;
using Stl.Fusion.Internal;
using Stl.Serialization;

namespace Stl.Fusion.Interception
{
    public class ComputedServiceInterceptor : InterceptorBase
    {
        public new class Options : InterceptorBase.Options
        {
            public ConcurrentIdGenerator<LTag> LTagGenerator { get; set; } = ConcurrentIdGenerator.DefaultLTag; 
        }

        protected ConcurrentIdGenerator<LTag> LTagGenerator { get; }

        public ComputedServiceInterceptor(
            Options options, 
            IComputedRegistry? registry = null, 
            ILoggerFactory? loggerFactory = null) 
            : base(options, registry, loggerFactory)
        {
            LTagGenerator = options.LTagGenerator;
        }

        protected override InterceptedFunctionBase<T> CreateFunction<T>(InterceptedMethod method)
        {
            var log = LoggerFactory.CreateLogger<ComputedServiceFunction<T>>();
            return new ComputedServiceFunction<T>(method, LTagGenerator, Registry, log);
        }

        protected override void ValidateType(Type type)
        {
            Log.LogDebug($"Validating: '{type.FullName}':");
            if (!typeof(IComputedService).IsAssignableFrom(type))
                throw Errors.MustImplement<IComputedService>(type);
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static;
            foreach (var method in type.GetMethods(bindingFlags)) {
                var attrs = method
                    .GetCustomAttributes(typeof(ComputedServiceMethodAttribute), true)
                    .Cast<ComputedServiceMethodAttribute>()
                    .ToArray();
                if (!attrs.Any())
                    continue; // No attributes
                if (method.IsStatic)
                    throw Errors.ComputedServiceMethodAttributeOnStaticMethod(method);
                if (!method.IsVirtual)
                    throw Errors.ComputedServiceMethodAttributeOnStaticMethod(method);
                if (attrs.Any(a => !a.IsEnabled)) {
                    Log.LogDebug($"- {method}: has {nameof(ComputedServiceMethodAttribute)}(false)");
                    continue;
                }
                var attr = attrs.FirstOrDefault();
                Log.LogDebug($"+ {method}: {nameof(ComputedServiceMethodAttribute)} {{ " +
                    $"{nameof(ComputedServiceMethodAttribute.IsEnabled)} = {attr.IsEnabled}, " +
                    $"{nameof(ComputedServiceMethodAttribute.KeepAliveTime)} = {attr.KeepAliveTime}" +
                    $" }}");
            }
        }
    }
}
