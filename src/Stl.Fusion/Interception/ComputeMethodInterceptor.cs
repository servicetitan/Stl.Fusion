using Stl.Fusion.Internal;
using Stl.Versioning;

namespace Stl.Fusion.Interception;

public sealed class ComputeMethodInterceptor : ComputeMethodInterceptorBase
{
    public new record Options : ComputeMethodInterceptorBase.Options
    {
        public VersionGenerator<LTag>? VersionGenerator { get; init; }
    }

    private VersionGenerator<LTag> VersionGenerator { get; }

    public ComputeMethodInterceptor(Options options, IServiceProvider services)
        : base(options, services)
        => VersionGenerator = options.VersionGenerator ?? services.VersionGenerator<LTag>();

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => new ComputeMethodFunction<T>(method, Services, VersionGenerator);

    protected override void ValidateTypeInternal(Type type)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.FlattenHierarchy;
        foreach (var method in type.GetMethods(bindingFlags)) {
            var options = ComputedOptionsProvider.GetComputedOptions(type, method);
            if (options == null)
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

            Log.IfEnabled(ValidationLogLevel)?.Log(ValidationLogLevel,
                "+ {Method}: {Options}", method.ToString(), options);
        }
    }
}
