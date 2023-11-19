using System.Diagnostics.CodeAnalysis;

namespace Stl.CommandR.Configuration;

public sealed record InterfaceCommandHandler<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand>(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type ServiceType,
    bool IsFilter = false, double Priority = 0)
    : CommandHandler<TCommand>($"{ServiceType.GetName(true)} via interface", IsFilter, Priority)
    where TCommand : class, ICommand
{
    public override object GetHandlerService(ICommand command, CommandContext context)
        => context.Services.GetRequiredService(ServiceType);

    public override Task Invoke(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken)
    {
        var handler = (ICommandHandler<TCommand>) GetHandlerService(command, context);
        return handler.OnCommand((TCommand) command, context, cancellationToken);
    }

    public override string ToString() => base.ToString();

    // This record relies on reference-based equality
    public bool Equals(InterfaceCommandHandler<TCommand>? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}

public static class InterfaceCommandHandler
{
    public static InterfaceCommandHandler<TCommand> New<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand>
        ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type serviceType, bool isFilter, double priority = 0)
        where TCommand : class, ICommand
        => new(serviceType, isFilter, priority);

    public static InterfaceCommandHandler<TCommand> New<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TService,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand>
        (bool isFilter = false, double priority = 0)
        where TService : class
        where TCommand : class, ICommand
        => new(typeof(TService), isFilter, priority);

    public static CommandHandler New(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type serviceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType,
        bool isFilter = false, double priority = 0)
    {
        var ctor = typeof(InterfaceCommandHandler<>)
            .MakeGenericType(commandType)
            .GetConstructors()
            .Single();
        // ReSharper disable once HeapView.BoxingAllocation
        return (CommandHandler)ctor.Invoke(new object[] { serviceType, isFilter, priority });
    }
}
