namespace Stl.CommandR.Configuration;

public sealed record CommandHandlerSet
{
    public Type CommandType { get; }
    public bool IsSingleChain { get; }
    public ImmutableArray<CommandHandler> SingleChain { get; }
    public ImmutableDictionary<Symbol, ImmutableArray<CommandHandler>> MultipleChains { get; }

    public CommandHandlerSet(Type commandType, ImmutableArray<CommandHandler> singleChain)
    {
        CommandType = commandType;
        IsSingleChain = false;
        SingleChain = singleChain;
        MultipleChains = ImmutableDictionary<Symbol, ImmutableArray<CommandHandler>>.Empty;
    }

    public CommandHandlerSet(Type commandType, ImmutableDictionary<Symbol, ImmutableArray<CommandHandler>> multipleChains)
    {
        CommandType = commandType;
        IsSingleChain = true;
        SingleChain = ImmutableArray<CommandHandler>.Empty;
        MultipleChains = multipleChains;
    }

    public ImmutableArray<CommandHandler> GetHandlerChain(ICommand command)
    {
        if (command is not IMultiChainCommand multiChainCommand)
            return SingleChain;

        var chainId = multiChainCommand.ChainId;
        if (chainId.IsEmpty)
            return ImmutableArray<CommandHandler>.Empty;

        return MultipleChains.TryGetValue(chainId, out var result)
            ? result
            : ImmutableArray<CommandHandler>.Empty;
    }

    // This record relies on reference-based equality
    public bool Equals(CommandHandlerSet? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
