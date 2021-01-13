using System;
using System.Collections.Concurrent;
using System.Linq;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;
using Stl.Reflection;

namespace Stl.Fusion.CommandR
{
    public interface IInvalidationInfoProvider
    {
        bool RequiresInvalidation(ICommand? command);
    }

    public class InvalidationInfoProvider : IInvalidationInfoProvider
    {
        protected ICommander Commander { get; }
        protected ICommandHandlerResolver CommandHandlerResolver { get; }
        protected ConcurrentDictionary<Type, bool> RequiresInvalidationCache { get; } = new();

        public InvalidationInfoProvider(ICommander commander, ICommandHandlerResolver commandHandlerResolver)
        {
            Commander = commander;
            CommandHandlerResolver = commandHandlerResolver;
        }

        public bool RequiresInvalidation(ICommand? command)
        {
            if (command == null)
                return false;
            return RequiresInvalidationCache.GetOrAdd(command.GetType(), (type, arg) => {
                var (self, command1) = arg;
                using var _ = ExecutionContextEx.SuppressFlow();
                var tContext = typeof(CommandContext<>).MakeGenericType(command1!.ResultType);
                using var context = (CommandContext) tContext.CreateInstance(self.Commander, command1);

                var handlers = self.CommandHandlerResolver.GetCommandHandlers(command1.GetType());
                var finalHandler = handlers.FirstOrDefault(h => !h.IsFilter);
                var finalHandlerService = finalHandler?.GetHandlerService(command1, context);
                return finalHandlerService is IComputeService && !(finalHandlerService is IReplicaService);
            }, (this, command));
        }
    }
}
