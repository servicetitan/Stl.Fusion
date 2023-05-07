using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public sealed class RpcHub : ProcessorBase, IHasServices
{
    private ILogger? _log;

    private ILogger Log => _log ??= Services.LogFor(GetType());
    private Func<Symbol, RpcPeer> ConnectionFactory { get; }

    public IServiceProvider Services { get; }
    public RpcConfiguration Configuration { get; }
    public RpcServiceRegistry ServiceRegistry { get; }
    public RpcRequestBinder RequestBinder { get; }
    public RpcRequestHandler RequestHandler { get; }
    public RpcOutboundCallTracker OutboundCalls { get; private set; }
    public ConcurrentDictionary<Symbol, RpcPeer> Connections { get; } = new();

    public RpcPeer this[Symbol name] => Connections.GetOrAdd(name, CreateConnection);

    public RpcHub(IServiceProvider services)
    {
        Services = services;
        Configuration = services.GetRequiredService<RpcConfiguration>();
        ServiceRegistry = services.GetRequiredService<RpcServiceRegistry>();
        RequestBinder = services.GetRequiredService<RpcRequestBinder>();
        RequestHandler = services.GetRequiredService<RpcRequestHandler>();
        OutboundCalls = services.GetRequiredService<RpcOutboundCallTracker>();

        ConnectionFactory = services.GetRequiredService<Func<Symbol, RpcPeer>>();
    }

    protected override Task DisposeAsyncCore()
    {
        var disposeTasks = new List<Task>();
        foreach (var (_, connection) in Connections)
            disposeTasks.Add(connection.DisposeAsync().AsTask());
        return Task.WhenAll(disposeTasks);
    }

    private RpcPeer CreateConnection(Symbol name)
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
