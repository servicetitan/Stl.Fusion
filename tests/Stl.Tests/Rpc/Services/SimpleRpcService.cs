using System.Collections.Concurrent;
using Stl.Rpc;

namespace Stl.Tests.Rpc;

[DataContract, MemoryPackable]
public partial record SimpleRpcServiceDummyCommand(
    [property: DataMember] string Input
) : ICommand<Unit>;

public partial interface ITestRpcService : ICommandService
{
    Task<int?> Div(int? a, int b);
    Task<int?> Add(int? a, int b);
    Task<TimeSpan> Delay(TimeSpan duration, CancellationToken cancellationToken = default);
    Task<int> GetCancellationCount();

    Task<ITuple> Polymorph(ITuple argument, CancellationToken cancellationToken = default);

    ValueTask<RpcNoWait> MaybeSet(string key, string? value);
    ValueTask<string?> Get(string key);

    [CommandHandler]
    Task OnDummyCommand(SimpleRpcServiceDummyCommand command, CancellationToken cancellationToken = default);
}

public interface ITestRpcServiceClient : ITestRpcService, IRpcService
{ }

public class TestRpcService : ITestRpcService
{
    private volatile int _cancellationCount;
    private readonly ConcurrentDictionary<string, string> _values = new();

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

    public ValueTask<RpcNoWait> MaybeSet(string key, string? value)
    {
        if (value == null)
            _values.Remove(key, out _);
        else
            _values[key] = value;
        return default;
    }

    public ValueTask<string?> Get(string key)
        => new(_values.GetValueOrDefault(key));

    public virtual Task OnDummyCommand(SimpleRpcServiceDummyCommand command, CancellationToken cancellationToken = default)
    {
        if (Equals(command.Input, "error"))
            throw new ArgumentOutOfRangeException(nameof(command));
        return Task.CompletedTask;
    }
}
