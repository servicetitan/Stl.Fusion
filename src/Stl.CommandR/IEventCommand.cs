namespace Stl.CommandR;

public interface IEventCommand : ICommand<Unit>
{
    Symbol ChainId { get; init; }
}
