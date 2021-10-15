using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Operations;

public class InvalidationInfoProvider
{
    protected ICommander Commander { get; }
    protected ICommandHandlerResolver CommandHandlerResolver { get; }
    protected ConcurrentDictionary<Type, bool> IsReplicaServiceCommandCache { get; } = new();
    protected ConcurrentDictionary<Type, bool> IsComputeServiceCommandCache { get; } = new();
    protected ConcurrentDictionary<Type, Type?> FinalHandlerServiceTypeCache { get; } = new();

    public InvalidationInfoProvider(ICommander commander, ICommandHandlerResolver commandHandlerResolver)
    {
        Commander = commander;
        CommandHandlerResolver = commandHandlerResolver;
    }

    public virtual bool RequiresInvalidation(ICommand? command)
        => IsComputeServiceCommand(command) && !IsReplicaServiceCommand(command);

    public virtual bool IsComputeServiceCommand(ICommand? command)
    {
        if (command == null)
            return false;
        return IsComputeServiceCommandCache.GetOrAdd(command.GetType(), (_, command1) => {
            var finalHandlerServiceType = GetFinalHandlerServiceType(command1);
            return typeof(IComputeService).IsAssignableFrom(finalHandlerServiceType);
        }, command);
    }

    public virtual bool IsReplicaServiceCommand(ICommand? command)
    {
        if (command == null)
            return false;
        return IsReplicaServiceCommandCache.GetOrAdd(command.GetType(), (_, command1) => {
            var finalHandlerServiceType = GetFinalHandlerServiceType(command1);
            return typeof(IReplicaService).IsAssignableFrom(finalHandlerServiceType);
        }, command);
    }

    public virtual Type? GetFinalHandlerServiceType(ICommand? command)
    {
        if (command == null)
            return null;
        return FinalHandlerServiceTypeCache.GetOrAdd(command.GetType(), (type, arg) => {
            var (self, command1) = arg;
            using var context = CommandContext.New(self.Commander, command1!);
            var handlers = self.CommandHandlerResolver.GetCommandHandlers(command1.GetType());
            var finalHandler = handlers.FirstOrDefault(h => !h.IsFilter);
            return finalHandler?.GetHandlerService(command1, context)?.GetType();
        }, (this, command));
    }
}
