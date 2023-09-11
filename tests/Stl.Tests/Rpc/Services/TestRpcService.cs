using System.Collections.Concurrent;
using Stl.Rpc;

namespace Stl.Tests.Rpc;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record HelloCommand(
    [property: DataMember, MemoryPackOrder(0)] string Name,
    [property: DataMember, MemoryPackOrder(1)] TimeSpan Delay = default
) : ICommand<string>;

public interface ITestRpcService : ICommandService
{
    Task<int?> Div(int? a, int b);
    Task<int?> Add(int? a, int b);
    Task<TimeSpan> Delay(TimeSpan duration, CancellationToken cancellationToken = default);
    Task<int> GetCancellationCount();

    Task<int> PolymorphArg(ITuple argument, CancellationToken cancellationToken = default);
    Task<ITuple> PolymorphResult(int argument, CancellationToken cancellationToken = default);

    ValueTask<RpcNoWait> MaybeSet(string key, string? value);
    ValueTask<string?> Get(string key);

    Task<RpcStream<int>> StreamInt32(int count);
    Task<RpcStream<ITuple>> StreamTuples(int count);

    [CommandHandler]
    Task<string> OnHello(HelloCommand command, CancellationToken cancellationToken = default);
}

public interface ITestRpcServiceClient : ITestRpcService, IRpcService
{
    Task<int> NoSuchMethod(int i1, int i2, int i3, int i4, CancellationToken cancellationToken = default);
}

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

    public Task<int> PolymorphArg(ITuple argument, CancellationToken cancellationToken = default)
        => Task.FromResult(argument.Length);

    public Task<ITuple> PolymorphResult(int argument, CancellationToken cancellationToken = default)
        => Task.FromResult((ITuple)Tuple.Create(argument));

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

    public Task<RpcStream<int>> StreamInt32(int count)
        => Task.FromResult(RpcStream.New(Enumerable.Range(0, count)));

    public Task<RpcStream<ITuple>> StreamTuples(int count)
        => Task.FromResult(RpcStream.New(Enumerable.Range(0, count).Select(x => (ITuple)new Tuple<int>(x))));

    public virtual async Task<string> OnHello(HelloCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Delay > TimeSpan.Zero)
            await Task.Delay(command.Delay, cancellationToken);

        if (Equals(command.Name, "error"))
            throw new ArgumentOutOfRangeException(nameof(command));

        return $"Hello, {command.Name}!";
    }
}
