using Stl.CommandR.Internal;

namespace Stl.CommandR.Configuration;

public sealed record MethodCommandHandler<TCommand> : CommandHandler<TCommand>
    where TCommand : class, ICommand
{
    private ParameterInfo[]? _cachedParameters;
    public Type ServiceType { get; }
    public MethodInfo Method { get; }

    public MethodCommandHandler(Type serviceType, MethodInfo method, bool isFilter = false, double priority = 0)
        : base($"{serviceType.GetName(true)}.{method.Name}", isFilter, priority)
    {
        ServiceType = serviceType;
        Method = method;
    }

    public override object GetHandlerService(ICommand command, CommandContext context)
        => context.Services.GetRequiredService(ServiceType);

    public override Task Invoke(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken)
    {
        var services = context.Services;
        var service = GetHandlerService(command, context);
        var parameters = _cachedParameters ??= Method.GetParameters();
        var arguments = new object[parameters.Length];
        arguments[0] = command;
        // ReSharper disable once HeapView.BoxingAllocation
        arguments[^1] = cancellationToken;
        for (var i = 1; i < parameters.Length - 1; i++) {
            var p = parameters[i];
            var value = GetParameterValue(p, context, services);
            arguments[i] = value;
        }
        try {
            return (Task) Method.Invoke(service, arguments)!;
        }
        catch (TargetInvocationException tie) {
            if (tie.InnerException != null)
                throw tie.InnerException;
            throw;
        }
    }

    public override string ToString() => base.ToString();

    // This record relies on reference-based equality
    public bool Equals(MethodCommandHandler<TCommand>? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    private static object GetParameterValue(ParameterInfo parameter, ICommandContext context, IServiceProvider services)
    {
        if (parameter.ParameterType == typeof(CommandContext))
            return context;
        if (parameter.HasDefaultValue)
            return services.GetService(parameter.ParameterType) ?? parameter.DefaultValue!;
        return services.GetRequiredService(parameter.ParameterType);
    }
}

public static class MethodCommandHandler
{
    private static readonly MethodInfo CreateMethod =
        typeof(MethodCommandHandler)
            .GetMethod(nameof(Create), BindingFlags.Static | BindingFlags.NonPublic)!;

    public static CommandHandler New(Type serviceType, MethodInfo method, double? priorityOverride = null)
        => TryNew(serviceType, method, priorityOverride)
            ?? throw Errors.InvalidCommandHandlerMethod(method);

    public static CommandHandler? TryNew(Type serviceType, MethodInfo method, double? priorityOverride = null)
    {
        var attr = GetAttribute(method);
        if (attr == null)
            return null;

        var isFilter = attr.IsFilter;
        var order = priorityOverride ?? attr.Priority;

        if (method.IsStatic)
            throw Errors.CommandHandlerMethodMustBeInstanceMethod(method);

        var tHandlerResult = method.ReturnType;
        if (!typeof(Task).IsAssignableFrom(tHandlerResult))
            throw Errors.CommandHandlerMethodMustReturnTask(method);

        var parameters = method.GetParameters();
        if (parameters.Length < 2)
            throw Errors.WrongCommandHandlerMethodArgumentCount(method);

        // Checking command parameter
        var pCommand = parameters[0];
        if (!typeof(ICommand).IsAssignableFrom(pCommand.ParameterType))
            throw Errors.WrongCommandHandlerMethodArguments(method);
        if (tHandlerResult.IsGenericType && tHandlerResult.GetGenericTypeDefinition() == typeof(Task<>)) {
            var tHandlerResultTaskArgument = tHandlerResult.GetGenericArguments().Single();
            var tGenericCommandType = typeof(ICommand<>).MakeGenericType(tHandlerResultTaskArgument);
            if (!tGenericCommandType.IsAssignableFrom(pCommand.ParameterType))
                throw Errors.WrongCommandHandlerMethodArguments(method);
        }

        // Checking CancellationToken parameter
        var pCancellationToken = parameters[^1];
        if (typeof(CancellationToken) != pCancellationToken.ParameterType)
            throw Errors.WrongCommandHandlerMethodArguments(method);

        return (CommandHandler) CreateMethod
            .MakeGenericMethod(pCommand.ParameterType)
            .Invoke(null, new object[] {serviceType, method, isFilter, order})!;
    }

    public static CommandHandlerAttribute? GetAttribute(MethodInfo method)
        => method.GetAttribute<CommandHandlerAttribute>(true, true);

    private static MethodCommandHandler<TCommand> Create<TCommand>(
        Type serviceType, MethodInfo method, bool isFilter, double priority)
        where TCommand : class, ICommand
        => new(serviceType, method, isFilter, priority);
}
