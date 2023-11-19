using System.Diagnostics.CodeAnalysis;
using Stl.CommandR.Internal;
using Stl.Interception.Interceptors;

namespace Stl.CommandR.Interception;

public sealed class CommandHandlerMethodDef : MethodDef
{
    public CommandHandlerMethodDef(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        MethodInfo method)
        : base(type, method)
    {
#pragma warning disable IL2026, IL2072
        var commandHandler = MethodCommandHandler.TryNew(method.ReflectedType!, method);
#pragma warning restore IL2026, IL2072
        if (commandHandler == null) {
            IsValid = false;
            return; // Can be only when attr.IsEnabled == false
        }

        if (!method.IsVirtual || method.IsFinal)
            throw Errors.WrongInterceptedCommandHandlerMethodSignature(method);

        var parameters = method.GetParameters();
        if (parameters.Length != 2)
            throw Errors.WrongInterceptedCommandHandlerMethodSignature(method);
    }
}
