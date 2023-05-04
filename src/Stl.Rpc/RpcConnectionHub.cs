using Stl.Internal;

namespace Stl.Rpc;

public class RpcConnectionHub : ProcessorBase, IHasServices
{
    private Func<Symbol, RpcConnection> ConnectionFactory { get; }

    public IServiceProvider Services { get; }
    public ConcurrentDictionary<Symbol, RpcConnection> Connections { get; } = new();

    public RpcConnection this[Symbol name] => Connections.GetOrAdd(name, CreateConnection);

    public RpcConnectionHub(IServiceProvider services)
    {
        Services = services;
        ConnectionFactory = services.GetRequiredService<Func<Symbol, RpcConnection>>();
    }

    protected override Task DisposeAsyncCore()
    {
        var disposeTasks = new List<Task>();
        foreach (var (_, connection) in Connections)
            disposeTasks.Add(connection.DisposeAsync().AsTask());
        return Task.WhenAll(disposeTasks);
    }

    protected virtual RpcConnection CreateConnection(Symbol name)
    {
        if (WhenDisposed != null)
            throw Errors.AlreadyDisposed();

        var channel = ConnectionFactory.Invoke(name);
        _ = channel.Run().ContinueWith(
            _ => Connections.TryRemove(name, channel),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return channel;
    }
}
