using System.Diagnostics.CodeAnalysis;
using Stl.CommandR.Internal;

namespace Stl.CommandR.Configuration;

public sealed record MethodCommandHandler<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand>
    ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type ServiceType,
    MethodInfo Method, bool IsFilter = false, double Priority = 0)
    : CommandHandler<TCommand>($"{ServiceType.GetName(true)}.{Method.Name}", IsFilter, Priority)
    where TCommand : class, ICommand
{
    private ParameterInfo[]? _cachedParameters;

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

    [RequiresUnreferencedCode(UnreferencedCode.Commander)]
    public static CommandHandler New(Type serviceType, MethodInfo method, double? priorityOverride = null)
        => TryNew(serviceType, method, priorityOverride)
            ?? throw Errors.InvalidCommandHandlerMethod(method);

    [RequiresUnreferencedCode(UnreferencedCode.Commander)]
    public static CommandHandler? TryNew(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type serviceType,
        MethodInfo method,
        double? priorityOverride = null)
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

        return (CommandHandler)CreateMethod
            .MakeGenericMethod(pCommand.ParameterType)
            .Invoke(null, new object[] { serviceType, method, isFilter, order })!;
    }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Reflection)]
    public static CommandHandlerAttribute? GetAttribute(MethodInfo method)
#pragma warning disable IL2026
        => method.GetAttribute<CommandHandlerAttribute>(true, true);
#pragma warning restore IL2026

    private static MethodCommandHandler<TCommand> Create<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type serviceType,
        MethodInfo method, bool isFilter, double priority)
        where TCommand : class, ICommand
        => new(serviceType, method, isFilter, priority);
}
