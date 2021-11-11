using System.Globalization;
using Stl.Fusion.Internal;
using Stl.Versioning;

namespace Stl.Fusion.Interception;

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

            var attributeName = nameof(ComputeMethodAttribute)
#if NETSTANDARD2_0
                .Replace(nameof(Attribute), "");
#else
                .Replace(nameof(Attribute), "", StringComparison.Ordinal);
#endif
            if (!attr.IsEnabled)
                Log.Log(ValidationLogLevel,
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    $"- {{Method}}: has [{attributeName}(false)]", method.ToString());
            else {
                var properties = new List<string>();
                if (!double.IsNaN(attr.KeepAliveTime))
                    properties.Add($"{nameof(attr.KeepAliveTime)} = {Format(attr.KeepAliveTime)}");
                if (!double.IsNaN(attr.AutoInvalidateTime))
                    properties.Add($"{nameof(attr.AutoInvalidateTime)} = {Format(attr.AutoInvalidateTime)}");
                if (!double.IsNaN(attr.ErrorAutoInvalidateTime))
                    properties.Add($"{nameof(attr.ErrorAutoInvalidateTime)} = {Format(attr.ErrorAutoInvalidateTime)}");
                Log.Log(ValidationLogLevel,
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    $"+ {{Method}}: [{attributeName}({properties.ToDelimitedString(", ")})]", method.ToString());
            }

            static string Format(double value)
                => value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
