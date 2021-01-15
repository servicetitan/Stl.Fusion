using System;
using System.Reflection;
using System.Threading;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.CommandR.Configuration;
using Stl.CommandR.Internal;
using Stl.Interception.Interceptors;

namespace Stl.CommandR.Interception
{
    public class CommandServiceInterceptor : InterceptorBase
    {
        public new class Options : InterceptorBase.Options
        { }

        protected ICommander Commander { get; }

        public CommandServiceInterceptor(
            Options options,
            IServiceProvider services,
            ILoggerFactory? loggerFactory = null)
            : base(options, services, loggerFactory)
            => Commander = services.GetRequiredService<ICommander>();

        protected override Action<IInvocation> CreateHandler<T>(
            IInvocation initialInvocation, MethodDef methodDef)
            => invocation => {
                if (!(invocation.Proxy is ICommandService)) {
                    invocation.Proceed();
                    return;
                }
                var command = (ICommand) invocation.Arguments[0];
                var cancellationToken1 = (CancellationToken) invocation.Arguments[^1];
                var context = CommandContext.Current;
                if (ReferenceEquals(command, context?.UntypedCommand))
                    invocation.Proceed();
                else {
                    var newContext = Commander.Start(command, false, cancellationToken1);
                    invocation.ReturnValue = newContext.UntypedResultTask;
                }
            };

        protected override MethodDef? CreateMethodDef(MethodInfo methodInfo, IInvocation initialInvocation)
        {
            var methodDef = new CommandHandlerMethodDef(this, methodInfo);
            return methodDef.IsValid ? methodDef : null;
        }

        protected override void ValidateTypeInternal(Type type)
        {
            Log.Log(ValidationLogLevel, $"Validating: '{type}':");
            if (typeof(ICommandHandler).IsAssignableFrom(type))
                throw Errors.OnlyInterceptedCommandHandlersAllowed(type);
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.FlattenHierarchy;
            foreach (var method in type.GetMethods(bindingFlags)) {
                var attr = MethodCommandHandler.GetAttribute(method);
                if (attr == null)
                    continue;

                var methodDef = new CommandHandlerMethodDef(this, method);
                if (!methodDef.IsValid) // attr.IsEnabled == false
                    Log.Log(ValidationLogLevel,
                        $"- {method}: has {nameof(CommandHandlerAttribute)}(false)");
                else
                    Log.Log(ValidationLogLevel,
                        $"+ {method}: {nameof(CommandHandlerAttribute)} {{ " +
                        $"{nameof(CommandHandlerAttribute.Priority)} = {attr.Priority}" +
                        $" }}");
            }
        }
    }
}
