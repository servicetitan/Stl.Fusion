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
        private ParameterInfo[]? _cachedParameters;
        public Type ServiceType { get; }
        public MethodInfo MethodInfo { get; }

        public MethodCommandHandler(Type serviceType, MethodInfo methodInfo, bool isFilter = false, double order = 0)
            : base(isFilter, order)
        {
            ServiceType = serviceType;
            MethodInfo = methodInfo;
        }

        public override object GetHandlerService(ICommand command, CommandContext context)
            => context.Services.GetRequiredService(ServiceType);

        public override Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var services = context.Services;
            var service = services.GetRequiredService(ServiceType);
            var parameters = _cachedParameters ??= MethodInfo.GetParameters();
            var arguments = new object[parameters.Length];
            arguments[0] = command;
            // ReSharper disable once HeapView.BoxingAllocation
            arguments[^1] = cancellationToken;
            for (var i = 1; i < parameters.Length - 1; i++) {
                var p = parameters[i];
                var value = p.HasDefaultValue
                    ? (services.GetService(p.ParameterType) ?? p.DefaultValue!)
                    : services.GetRequiredService(p.ParameterType);
                arguments[i] = value;
            }
            try {
                return (Task) MethodInfo.Invoke(service, arguments)!;
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

        public static CommandHandler New(Type serviceType, MethodInfo methodInfo, double? priorityOverride = null)
            => TryNew(serviceType, methodInfo, priorityOverride)
                ?? throw Errors.InvalidCommandHandlerMethod(methodInfo);

        public static CommandHandler? TryNew(Type serviceType, MethodInfo methodInfo, double? priorityOverride = null)
        {
            var attr = GetAttribute(methodInfo);
            var isEnabled = attr?.IsEnabled ?? false;
            if (!isEnabled)
                return null;
            var isFilter = attr?.IsFilter ?? false;
            var order = priorityOverride ?? attr?.Order ?? 0;

            if (methodInfo.IsStatic)
                throw Errors.CommandHandlerMethodMustBeInstanceMethod(methodInfo);

            var tHandlerResult = methodInfo.ReturnType;
            if (!typeof(Task).IsAssignableFrom(tHandlerResult))
                throw Errors.CommandHandlerMethodMustReturnTask(methodInfo);

            var parameters = methodInfo.GetParameters();
            if (parameters.Length < 2)
                throw Errors.WrongCommandHandlerMethodArgumentCount(methodInfo);

            // Checking command parameter
            var pCommand = parameters[0];
            if (!typeof(ICommand).IsAssignableFrom(pCommand.ParameterType))
                throw Errors.WrongCommandHandlerMethodArguments(methodInfo);
            if (tHandlerResult.IsGenericType && tHandlerResult.GetGenericTypeDefinition() == typeof(Task<>)) {
                var tHandlerResultTaskArgument = tHandlerResult.GetGenericArguments().Single();
                var tGenericCommandType = typeof(ICommand<>).MakeGenericType(tHandlerResultTaskArgument);
                if (!tGenericCommandType.IsAssignableFrom(pCommand.ParameterType))
                    throw Errors.WrongCommandHandlerMethodArguments(methodInfo);
            }

            // Checking CancellationToken parameter
            var pCancellationToken = parameters[^1];
            if (typeof(CancellationToken) != pCancellationToken.ParameterType)
                throw Errors.WrongCommandHandlerMethodArguments(methodInfo);

            return (CommandHandler) CreateMethod
                .MakeGenericMethod(pCommand.ParameterType)
                .Invoke(null, new object[] {serviceType, methodInfo, isFilter, order})!;
        }

        public static CommandHandlerAttribute? GetAttribute(MethodInfo methodInfo)
            => methodInfo.GetAttribute<CommandHandlerAttribute>(true, true);

        private static MethodCommandHandler<TCommand> Create<TCommand>(
            Type serviceType, MethodInfo methodInfo, bool isFilter, double order)
            where TCommand : class, ICommand
            => new(serviceType, methodInfo, isFilter, order);
    }
}
