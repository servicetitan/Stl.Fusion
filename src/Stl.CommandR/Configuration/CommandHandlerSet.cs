namespace Stl.CommandR.Configuration;

public sealed record CommandHandlerSet
{
    public Type CommandType { get; }
    public ImmutableDictionary<Symbol, ImmutableArray<CommandHandler>> HandlerChains { get; }
    public ImmutableArray<CommandHandler> SingleHandlerChain { get; }

    public CommandHandlerSet(Type commandType, ImmutableArray<CommandHandler> singleHandlerChain)
    {
        CommandType = commandType;
        SingleHandlerChain = singleHandlerChain;
        HandlerChains = ImmutableDictionary<Symbol, ImmutableArray<CommandHandler>>.Empty;
    }

    public CommandHandlerSet(Type commandType, ImmutableDictionary<Symbol, ImmutableArray<CommandHandler>> handlerChains)
    {
        CommandType = commandType;
        HandlerChains = handlerChains;
        SingleHandlerChain = ImmutableArray<CommandHandler>.Empty;
    }

    public ImmutableArray<CommandHandler> GetHandlerChain(ICommand command)
    {
        if (command is not IEventCommand eventCommand)
            return SingleHandlerChain;

        var chainId = eventCommand.ChainId;
        if (chainId.IsEmpty)
            return ImmutableArray<CommandHandler>.Empty;

        return HandlerChains.TryGetValue(chainId, out var result)
            ? result
            : ImmutableArray<CommandHandler>.Empty;
    }

    // This record relies on reference-based equality
    public bool Equals(CommandHandlerSet? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
