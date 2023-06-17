using MemoryPack;
using Stl.Rpc;

namespace Stl.Tests.Rpc;

[DataContract, MemoryPackable]
public partial record SimpleRpcServiceDummyCommand(
    [property: DataMember] string Input
) : ICommand<Unit>;

public partial interface ISimpleRpcService : ICommandService
{
    Task<int?> Div(int? a, int b);
    Task Delay(TimeSpan duration, CancellationToken cancellationToken = default);
    Task<ITuple> Polymorph(ITuple argument, CancellationToken cancellationToken = default);

    [CommandHandler]
    Task OnDummyCommand(SimpleRpcServiceDummyCommand command, CancellationToken cancellationToken = default);
}

public interface ISimpleRpcServiceClient : ISimpleRpcService, IRpcService
{ }

public class SimpleRpcService : ISimpleRpcService
{
    public Task<int?> Div(int? a, int b)
        => Task.FromResult(a / b);

    public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default)
        => Task.Delay(duration, cancellationToken);

    public Task<ITuple> Polymorph(ITuple argument, CancellationToken cancellationToken = default)
        => Task.FromResult(argument);

    public virtual Task OnDummyCommand(SimpleRpcServiceDummyCommand command, CancellationToken cancellationToken = default)
    {
        if (Equals(command.Input, "error"))
            throw new ArgumentOutOfRangeException(nameof(command));
        return Task.CompletedTask;
    }
}
