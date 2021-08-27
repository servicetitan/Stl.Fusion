using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Internal;
using Stl.Generators;
using Stl.Reflection;
using Stl.Versioning;
using Stl.Versioning.Providers;

namespace Stl.Fusion.Interception
{
    public class ComputeMethodInterceptor : ComputeMethodInterceptorBase
    {
        public new class Options : ComputeMethodInterceptorBase.Options
        {
            public VersionGenerator<LTag>? VersionGenerator { get; set; }
        }

        protected readonly VersionGenerator<LTag> VersionGenerator;

        public ComputeMethodInterceptor(
            Options? options,
            IServiceProvider services,
            ILoggerFactory? loggerFactory = null)
            : base(options ??= new(), services, loggerFactory)
            => VersionGenerator = options.VersionGenerator ?? services.VersionGenerator<LTag>();

        protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        {
            var log = LoggerFactory.CreateLogger<ComputeMethodFunction<T>>();
            if (method.Options.IsAsyncComputed)
                return new AsyncComputeMethodFunction<T>(method, VersionGenerator, Services, log);
            return new ComputeMethodFunction<T>(method, VersionGenerator, Services, log);
        }

        protected override void ValidateTypeInternal(Type type)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.FlattenHierarchy;
            foreach (var method in type.GetMethods(bindingFlags)) {
                var attr = ComputedOptionsProvider.GetComputeMethodAttribute(method);
                var options = ComputedOptionsProvider.GetComputedOptions(method);
                if (attr == null || options == null)
                    continue;
                if (method.IsStatic)
                    throw Errors.ComputeServiceMethodAttributeOnStaticMethod(method);
                if (!method.IsVirtual)
                    throw Errors.ComputeServiceMethodAttributeOnNonVirtualMethod(method);
                if (method.IsFinal)
                    // All implemented interface members are marked as "virtual final"
                    // unless they are truly virtual
                    throw Errors.ComputeServiceMethodAttributeOnNonVirtualMethod(method);
                var returnType = method.ReturnType;
                if (!returnType.IsTaskOrValueTask())
                    throw Errors.ComputeServiceMethodAttributeOnNonAsyncMethod(method);
                if (returnType.GetTaskOrValueTaskArgument() == null)
                    throw Errors.ComputeServiceMethodAttributeOnAsyncMethodReturningNonGenericTask(method);

                var attributeName = nameof(ComputeMethodAttribute).Replace(nameof(Attribute), "");
                if (!attr.IsEnabled)
                    Log.Log(ValidationLogLevel,
                        "- {Method}: has [{Attribute}(false)]", method.ToString(), attributeName);
                else
                    Log.Log(ValidationLogLevel,
                        "+ {Method}: [{Attribute}(" +
                        "KeepAliveTime = {KeepAliveTime}, " +
                        "AutoInvalidateTime = {AutoInvalidateTime}" +
                        ")]", method.ToString(), attributeName, attr.KeepAliveTime, attr.AutoInvalidateTime);
            }
        }
    }
}
