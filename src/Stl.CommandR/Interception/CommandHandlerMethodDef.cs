using Castle.DynamicProxy;
using Stl.CommandR.Internal;
using Stl.Interception.Interceptors;

namespace Stl.CommandR.Interception;

public class CommandHandlerMethodDef : MethodDef
{
    public CommandHandlerMethodDef(IInterceptor interceptor, MethodInfo methodInfo)
        : base(interceptor, methodInfo)
    {
        var commandHandler = MethodCommandHandler.TryNew(methodInfo.ReflectedType!, methodInfo);
        if (commandHandler == null)
            return; // Can be only when attr.IsEnabled == false

        if (!methodInfo.IsVirtual || methodInfo.IsFinal)
            throw Errors.WrongInterceptedCommandHandlerMethodSignature(methodInfo);

        var parameters = methodInfo.GetParameters();
        if (parameters.Length != 2)
            throw Errors.WrongInterceptedCommandHandlerMethodSignature(methodInfo);

        IsValid = true;
    }
}
