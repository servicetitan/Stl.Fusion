using Stl.CommandR.Internal;
using Stl.Interception.Interceptors;

namespace Stl.CommandR.Interception;

public sealed record CommandHandlerMethodDef : MethodDef
{
    public CommandHandlerMethodDef(Type type, MethodInfo method)
        : base(type, method)
    {
        var commandHandler = MethodCommandHandler.TryNew(method.ReflectedType!, method);
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

    // All XxxMethodDef records should rely on reference-based equality
    public bool Equals(CommandHandlerMethodDef? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
