using Stl.CommandR.Internal;

namespace Stl.CommandR.Configuration;

public interface ICommandHandlerResolver
{
    IReadOnlyList<CommandHandler> GetCommandHandlers(Type commandType);
}

public class CommandHandlerResolver : ICommandHandlerResolver
{
    protected ILogger Log { get; init; }
    protected ICommandHandlerRegistry Registry { get; }
    protected Func<CommandHandler, Type, bool> Filter { get; }
    protected ConcurrentDictionary<Type, IReadOnlyList<CommandHandler>> Cache { get; } = new();

    public CommandHandlerResolver(
        ICommandHandlerRegistry registry,
        IEnumerable<CommandHandlerFilter>? filters = null,
        ILogger<CommandHandlerResolver>? log = null)
    {
        Log = log ?? new NullLogger<CommandHandlerResolver>();
        Registry = registry;
        var aFilters = filters?.ToArray() ?? Array.Empty<CommandHandlerFilter>();
        Filter = (commandHandler, type) => aFilters.All(f => f.IsCommandHandlerUsed(commandHandler, type));
    }

    public IReadOnlyList<CommandHandler> GetCommandHandlers(Type commandType)
        => Cache.GetOrAdd(commandType, static (commandType1, self) => {
            var baseTypes = commandType1.GetAllBaseTypes(true, true)
                .Select((type, index) => (Type: type, Index: index))
                .ToArray();
            var handlers = (
                from typeEntry in baseTypes
                from handler in self.Registry.Handlers
                where handler.CommandType == typeEntry.Type && self.Filter(handler, commandType1)
                orderby handler.Priority descending, typeEntry.Index descending
                select handler
            ).Distinct().ToArray();
            var nonFilterHandlers = handlers.Where(h => !h.IsFilter);
            if (nonFilterHandlers.Count() > 1) {
                var e = Errors.MultipleNonFilterHandlers(commandType1);
                self.Log.LogCritical(e,
                    "Multiple non-filter handlers are found for '{CommandType}': {Handlers}",
                    commandType1, handlers.ToDelimitedString());
                throw e;
            }
            return handlers;
        }, this);
}
