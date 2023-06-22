using Stl.Fusion.Internal;

namespace Stl.Fusion.Interception;

public class ComputeServiceInterceptor : ComputeServiceInterceptorBase
{
    public new record Options : ComputeServiceInterceptorBase.Options;

    public ComputeServiceInterceptor(Options options, IServiceProvider services)
        : base(options, services)
    { }

    protected override ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method)
        => new ComputeMethodFunction<T>(method, Services, Hub.LTagVersionGenerator);

    protected override void ValidateTypeInternal(Type type)
    {
        base.ValidateTypeInternal(type);
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var methods = (type.IsInterface
            ? type.GetAllInterfaceMethods(bindingFlags)
            : type.GetMethods(bindingFlags)
            ).ToList();
        foreach (var method in methods) {
            if (method.DeclaringType == typeof(object))
                continue;
            var options = Hub.ComputedOptionsProvider.GetComputedOptions(type, method);
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
