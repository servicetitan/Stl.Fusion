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

    protected virtual RpcConnection CreateConnection(Symbol name)
    {
        var channel = ConnectionFactory.Invoke(name);
        _ = channel.Run().ContinueWith(
            _ => Connections.TryRemove(name, channel),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return channel;
    }
}
