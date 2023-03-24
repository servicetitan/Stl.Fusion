using Stl.CommandR.Internal;
using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.CommandR.Interception;

public record CommandHandlerMethodDef : MethodDef
{
    public CommandHandlerMethodDef(Interceptor interceptor, MethodInfo methodInfo)
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

    // All XxxMethodDef records should rely on reference-based equality
    public virtual bool Equals(CommandHandlerMethodDef? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
