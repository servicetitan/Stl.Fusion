using System.Reflection;
using Castle.DynamicProxy;
using Stl.CommandR.Configuration;
using Stl.CommandR.Internal;
using Stl.Interception.Internal;

namespace Stl.CommandR.Interception
{
    public class CommandHandlerMethodDef : MethodDef
    {
        public CommandHandlerMethodDef(IInterceptor interceptor, MethodInfo methodInfo)
            : base(interceptor, methodInfo)
        {
            if (!methodInfo.IsPublic)
                return;

            var commandHandler = MethodCommandHandler.TryNew(methodInfo.ReflectedType!, methodInfo);
            if (commandHandler == null)
                return;

            if (!methodInfo.IsVirtual)
                throw Errors.WrongInterceptedCommandHandlerMethodSignature(methodInfo);

            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 2)
                throw Errors.WrongInterceptedCommandHandlerMethodSignature(methodInfo);

            IsValid = true;
        }
    }
}
