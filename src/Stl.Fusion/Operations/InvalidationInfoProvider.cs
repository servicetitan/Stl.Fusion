using Stl.Interception.Internal;

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

        return IsComputeServiceCommandCache.GetOrAdd(command.GetType(), static (_, arg) => {
            var (self, command1) = arg;
            var finalHandlerServiceType = self.GetFinalHandlerServiceType(command1);
            return typeof(IComputeService).IsAssignableFrom(finalHandlerServiceType);
        }, (this, command));
    }

    public virtual bool IsReplicaServiceCommand(ICommand? command)
    {
        if (command == null)
            return false;

        return IsReplicaServiceCommandCache.GetOrAdd(command.GetType(), static (_, arg) => {
            var (self, command1) = arg;
            var finalHandlerServiceType = self.GetFinalHandlerServiceType(command1);
            return typeof(IComputeService).IsAssignableFrom(finalHandlerServiceType)
                && typeof(InterfaceProxy).IsAssignableFrom(finalHandlerServiceType);
        }, (this, command));
    }

    public virtual Type? GetFinalHandlerServiceType(ICommand? command)
    {
        if (command == null)
            return null;

        return FinalHandlerServiceTypeCache.GetOrAdd(command.GetType(), static (type, arg) => {
            var (self, command1) = arg;
            var context = CommandContext.New(self.Commander, command1, isOutermost: true);
            try {
                var handlers = self.CommandHandlerResolver.GetHandlerChain(command1);
                var finalHandler = handlers.FirstOrDefault(h => !h.IsFilter);
                var finalHandlerServiceType = finalHandler?
                    .GetHandlerService(command1, context)
                    .GetType();
                return finalHandlerServiceType;
            }
            finally {
                _ = Task.Run(() => context.DisposeAsync(), default);
            }
        }, (this, command));
    }
}
