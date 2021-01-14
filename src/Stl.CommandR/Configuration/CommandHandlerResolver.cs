using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Stl.CommandR.Internal;
using Stl.Reflection;

namespace Stl.CommandR.Configuration
{
    public interface ICommandHandlerResolver
    {
        IReadOnlyList<CommandHandler> GetCommandHandlers(Type commandType);
    }

    public class CommandHandlerResolver : ICommandHandlerResolver
    {
        protected ICommandHandlerRegistry Registry { get; }
        protected ConcurrentDictionary<Type, IReadOnlyList<CommandHandler>> Cache { get; } = new();

        public CommandHandlerResolver(ICommandHandlerRegistry registry)
            => Registry = registry;

        public IReadOnlyList<CommandHandler> GetCommandHandlers(Type commandType)
            => Cache.GetOrAdd(commandType, (commandType1, self) => {
                var baseTypes = commandType1.GetAllBaseTypes(true, true)
                    .Select((type, index) => (Type: type, Index: index))
                    .ToArray();
                var handlers = (
                    from typeEntry in baseTypes
                    from handler in self.Registry.Handlers.Where(h => h.CommandType == typeEntry.Type)
                    orderby handler.Order, -typeEntry.Index
                    select handler
                ).Distinct().ToArray();
                if (handlers.Count(h => !h.IsFilter) > 1)
                    throw Errors.MultipleNonFilterHandlers(commandType1);
                return handlers;
            }, this);
    }
}
