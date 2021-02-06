using System;
using System.Collections.Concurrent;
using System.Linq;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.CommandR.Configuration;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Operations
{
    public class InvalidationInfoProvider
    {
        protected ICommander Commander { get; }
        protected ICommandHandlerResolver CommandHandlerResolver { get; }
        protected ConcurrentDictionary<Type, bool> RequiresInvalidationCache { get; } = new();

        public InvalidationInfoProvider(ICommander commander, ICommandHandlerResolver commandHandlerResolver)
        {
            Commander = commander;
            CommandHandlerResolver = commandHandlerResolver;
        }

        public virtual bool RequiresInvalidation(ICommand? command)
        {
            if (command == null)
                return false;
            return RequiresInvalidationCache.GetOrAdd(command.GetType(), (type, arg) => {
                var (self, command1) = arg;
                if (typeof(IMetaCommand).IsAssignableFrom(type))
                    return false; // No invalidation for "second-order" commands

                using var context = CommandContext.New(self.Commander, command1!);
                var handlers = self.CommandHandlerResolver.GetCommandHandlers(command1.GetType());
                var finalHandler = handlers.FirstOrDefault(h => !h.IsFilter);
                var finalHandlerService = finalHandler?.GetHandlerService(command1, context);
                return finalHandlerService is IComputeService && !(finalHandlerService is IReplicaService);
            }, (this, command));
        }
    }
}
