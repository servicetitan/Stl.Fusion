using System.Diagnostics.CodeAnalysis;
using Stl.CommandR.Internal;

namespace Stl.CommandR.Configuration;

public class CommandHandlerResolver
{
    protected ILogger Log { get; init; }
    protected CommandHandlerRegistry Registry { get; }
    protected Func<CommandHandler, Type, bool> Filter { get; }
    protected ConcurrentDictionary<Type, CommandHandlerSet> Cache { get; } = new();

    public CommandHandlerResolver(IServiceProvider services)
    {
        Log = services.LogFor(GetType());
        Registry = services.GetRequiredService<CommandHandlerRegistry>();
        var filters = services.GetRequiredService<IEnumerable<CommandHandlerFilter>>().ToArray();
        Filter = (commandHandler, type) => filters.All(f => f.IsCommandHandlerUsed(commandHandler, type));
    }

    public virtual CommandHandlerSet GetCommandHandlers(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType)
        => Cache.GetOrAdd(commandType, static (commandType1, self) => {
            if (!typeof(ICommand).IsAssignableFrom(commandType1))
                throw new ArgumentOutOfRangeException(nameof(commandType1));

#pragma warning disable IL2067
            var baseTypes = commandType1.GetAllBaseTypes(true, true)
#pragma warning restore IL2067
                .Select((type, index) => (Type: type, Index: index))
                .ToArray();
            var handlers = (
                from typeEntry in baseTypes
                from handler in self.Registry.Handlers
                where handler.CommandType == typeEntry.Type && self.Filter(handler, commandType1)
                orderby handler.Priority descending, typeEntry.Index descending
                select handler
            ).Distinct().ToList();

            var nonFilterHandlers = handlers.Where(h => !h.IsFilter);

            if (!typeof(IEventCommand).IsAssignableFrom(commandType1)) {
                // Regular ICommand
                if (nonFilterHandlers.Count() > 1) {
                    var e = Errors.MultipleNonFilterHandlers(commandType1);
                    self.Log.LogCritical(e,
                        "Multiple non-filter handlers are found for '{CommandType}': {Handlers}",
                        commandType1, handlers.ToDelimitedString());
                    throw e;
                }
                return new CommandHandlerSet(commandType1, handlers.ToImmutableArray());
            }
            else {
                // IEventCommand
                var handlerChains = (
                    from nonFilterHandler in nonFilterHandlers
                    let handlerSubset = handlers.Where(h => h.IsFilter || h == nonFilterHandler).ToImmutableArray()
                    select KeyValuePair.Create(nonFilterHandler.Id, handlerSubset)
                    ).ToImmutableDictionary();
                return new CommandHandlerSet(commandType1, handlerChains);
            }
        }, this);
}
