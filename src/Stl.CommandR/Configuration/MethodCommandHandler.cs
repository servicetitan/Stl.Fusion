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
        public Type HandlerServiceType { get; }
        public MethodInfo HandlerMethod { get; }

        public MethodCommandHandler(MethodInfo handlerMethod, double order = 0)
            : base(order)
        {
            HandlerServiceType = handlerMethod.ReflectedType!;
            HandlerMethod = handlerMethod;
        }

        public override Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var handlerService = context.GetRequiredService(HandlerServiceType);
            var parameters = HandlerMethod.GetParameters();
            var arguments = new object[parameters.Length];
            arguments[0] = command;
            // ReSharper disable once HeapView.BoxingAllocation
            arguments[^1] = cancellationToken;
            for (var i = 1; i < parameters.Length - 1; i++) {
                var p = parameters[i];
                var value = p.HasDefaultValue
                    ? (context.GetService(p.ParameterType) ?? p.DefaultValue!)
                    : context.GetRequiredService(p.ParameterType);
                arguments[i] = value;
            }
            try {
                return (Task) HandlerMethod.Invoke(handlerService, arguments)!;
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
            var order = priorityOverride ?? attr?.Order ?? 0;

            var tHandlerResult = handlerMethod.ReturnType;
            if (!typeof(Task).IsAssignableFrom(tHandlerResult))
                throw Errors.CommandHandlerMethodMustReturnTask(handlerMethod);

            var parameters = handlerMethod.GetParameters();
            if (parameters.Length < 2)
                throw Errors.WrongCommandHandlerMethodArgumentCount(handlerMethod);
            var pCommand = parameters[0];
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

            return (CommandHandler) CreateMethod
                .MakeGenericMethod(pCommand.ParameterType)
                .Invoke(null, new object[] {handlerMethod, order})!;
        }

        private static MethodCommandHandler<TCommand> Create<TCommand>(
            MethodInfo handlerMethod, double order = 0)
            where TCommand : class, ICommand
            => new(handlerMethod, order);
    }
}
