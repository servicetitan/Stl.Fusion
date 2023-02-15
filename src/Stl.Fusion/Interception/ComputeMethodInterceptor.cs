using System.Globalization;
using Stl.Fusion.Internal;
using Stl.Versioning;

namespace Stl.Fusion.Interception;

public class ComputeMethodInterceptor : ComputeMethodInterceptorBase
{
    public new record Options : ComputeMethodInterceptorBase.Options
    {
        public VersionGenerator<LTag>? VersionGenerator { get; init; }
    }

    protected readonly VersionGenerator<LTag> VersionGenerator;

    public ComputeMethodInterceptor(Options options, IServiceProvider services)
        : base(options, services)
        => VersionGenerator = options.VersionGenerator ?? services.VersionGenerator<LTag>();

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => method.ComputedOptions.IsAsyncComputed
            ? new AsyncComputeMethodFunction<T>(method, Services, VersionGenerator)
            : new ComputeMethodFunction<T>(method, Services, VersionGenerator);

    protected override void ValidateTypeInternal(Type type)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.FlattenHierarchy;
        foreach (var method in type.GetMethods(bindingFlags)) {
            var attr = ComputedOptionsProvider.GetComputeMethodAttribute(method, type);
            var options = ComputedOptionsProvider.GetComputedOptions(method, type);
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

            var attributeName = attr.GetType().GetName()
#if NETSTANDARD2_0
                .Replace(nameof(Attribute), "");
#else
                .Replace(nameof(Attribute), "", StringComparison.Ordinal);
#endif
            var properties = new List<string>();
            if (!double.IsNaN(attr.MinCacheDuration))
                properties.Add($"{nameof(attr.MinCacheDuration)} = {Format(attr.MinCacheDuration)}");
            if (!double.IsNaN(attr.AutoInvalidationDelay))
                properties.Add($"{nameof(attr.AutoInvalidationDelay)} = {Format(attr.AutoInvalidationDelay)}");
            if (!double.IsNaN(attr.TransientErrorInvalidationDelay))
                properties.Add($"{nameof(attr.TransientErrorInvalidationDelay)} = {Format(attr.TransientErrorInvalidationDelay)}");
            Log.Log(ValidationLogLevel,
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                $"+ {{Method}}: [{attributeName}({properties.ToDelimitedString(", ")})]", method.ToString());

            static string Format(double value)
                => value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
