namespace Stl.CommandR;

public interface IMultiChainCommand : ICommand<Unit>
{
    Symbol ChainId { get; init; }
}
