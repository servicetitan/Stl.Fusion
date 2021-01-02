using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR.Internal;
using Stl.Reflection;

namespace Stl.CommandR.Configuration
{
    public record MethodCommandHandler<TCommand> : CommandHandler<TCommand>
        where TCommand : class, ICommand
    {
        public MethodInfo HandlerMethod { get; }
        public bool HasContextParameter { get; }

        public MethodCommandHandler(MethodInfo handlerMethod, double priority = 0)
            : base(handlerMethod.ReflectedType!, priority)
        {
            HandlerMethod = handlerMethod;
            HasContextParameter = handlerMethod.GetParameters().Length == 3;
        }

        public override Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var services = context.ServiceProvider;
            var handlerService = services.GetRequiredService(HandlerServiceType);
            var parameters = HasContextParameter
                // ReSharper disable once HeapView.BoxingAllocation
                ? new object[] {command, context, cancellationToken}
                // ReSharper disable once HeapView.BoxingAllocation
                : new object[] {command, cancellationToken};
            try {
                return (Task) HandlerMethod.Invoke(handlerService, parameters)!;
            }
            catch (TargetInvocationException tie) {
                if (tie.InnerException != null)
                    throw tie.InnerException;
                throw;
            }
        }
    }

    public static class MethodCommandHandler
    {
        private static readonly MethodInfo CreateMethod =
            typeof(MethodCommandHandler)
                .GetMethod(nameof(Create), BindingFlags.Static | BindingFlags.NonPublic)!;

        public static CommandHandler New(MethodInfo handlerMethod, double? priorityOverride = null)
            => TryNew(handlerMethod, priorityOverride) ?? throw Errors.InvalidCommandHandlerMethod(handlerMethod);

        public static CommandHandler? TryNew(MethodInfo handlerMethod, double? priorityOverride = null)
        {
            var attr = handlerMethod.GetAttribute<CommandHandlerAttribute>(true, true);
            var isEnabled = attr?.IsEnabled ?? false;
            if (!isEnabled)
                return null;
            var priority = priorityOverride ?? attr?.Priority ?? 0;

            var tHandlerResult = handlerMethod.ReturnType;
            if (!typeof(Task).IsAssignableFrom(tHandlerResult))
                throw Errors.CommandHandlerMethodMustReturnTask(handlerMethod);

            var parameters = handlerMethod.GetParameters();
            if (parameters.Length is < 2 or > 3)
                throw Errors.WrongCommandHandlerMethodArgumentCount(handlerMethod);
            var pCommand = parameters[0];
            var pContext = parameters.Length > 2 ? parameters[1] : null;
            var pCancellationToken = parameters[^1];

            if (!typeof(ICommand).IsAssignableFrom(pCommand.ParameterType))
                throw Errors.WrongCommandHandlerMethodArguments(handlerMethod);
            if (tHandlerResult.IsGenericType && tHandlerResult.GetGenericTypeDefinition() == typeof(Task<>)) {
                var tHandlerResultTaskArgument = tHandlerResult.GetGenericArguments().Single();
                var tGenericCommandType = typeof(ICommand<>).MakeGenericType(tHandlerResultTaskArgument);
                if (!tGenericCommandType.IsAssignableFrom(pCommand.ParameterType))
                    throw Errors.WrongCommandHandlerMethodArguments(handlerMethod);
            }

            if (typeof(CancellationToken) != pCancellationToken.ParameterType)
                throw Errors.WrongCommandHandlerMethodArguments(handlerMethod);
            if (pContext != null && !typeof(CommandContext).IsAssignableFrom(pContext.ParameterType))
                throw Errors.WrongCommandHandlerMethodArguments(handlerMethod);

            return (CommandHandler) CreateMethod
                .MakeGenericMethod(pCommand.ParameterType)
                .Invoke(null, new object[] {handlerMethod, priority})!;
        }

        private static MethodCommandHandler<TCommand> Create<TCommand>(
            MethodInfo handlerMethod, double priority = 0)
            where TCommand : class, ICommand
            => new(handlerMethod, priority);
    }
}
