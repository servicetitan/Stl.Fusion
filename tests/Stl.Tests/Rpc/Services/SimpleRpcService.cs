using Stl.Rpc;

namespace Stl.Tests.Rpc;

[DataContract, MemoryPackable]
public partial record SimpleRpcServiceDummyCommand(
    [property: DataMember] string Input
) : ICommand<Unit>;

public partial interface ISimpleRpcService : ICommandService
{
    Task<int?> Div(int? a, int b);
    Task<int?> Add(int? a, int b);
    Task<TimeSpan> Delay(TimeSpan duration, CancellationToken cancellationToken = default);
    Task<int> GetCancellationCount();
    Task<ITuple> Polymorph(ITuple argument, CancellationToken cancellationToken = default);

    [CommandHandler]
    Task OnDummyCommand(SimpleRpcServiceDummyCommand command, CancellationToken cancellationToken = default);
}

public interface ISimpleRpcServiceClient : ISimpleRpcService, IRpcService
{ }

public class SimpleRpcService : ISimpleRpcService
{
    private volatile int _cancellationCount;

    public Task<int?> Div(int? a, int b)
        => Task.FromResult(a / b);

    public Task<int?> Add(int? a, int b)
        => Task.FromResult(a + b);

    public async Task<TimeSpan> Delay(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        try {
            await Task.Delay(duration, cancellationToken);
            return duration;
        }
        catch (OperationCanceledException) {
            Interlocked.Increment(ref _cancellationCount);
            throw;
        }
    }

    public Task<int> GetCancellationCount()
        => Task.FromResult(_cancellationCount);

    public Task<ITuple> Polymorph(ITuple argument, CancellationToken cancellationToken = default)
        => Task.FromResult(argument);

    public virtual Task OnDummyCommand(SimpleRpcServiceDummyCommand command, CancellationToken cancellationToken = default)
    {
        if (Equals(command.Input, "error"))
            throw new ArgumentOutOfRangeException(nameof(command));
        return Task.CompletedTask;
    }
}
