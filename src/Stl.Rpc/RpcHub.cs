using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public sealed class RpcHub : ProcessorBase, IHasServices
{
    private ILogger? _log;
    private RpcServiceRegistry? _serviceRegistry;
    private RpcHandlerFactory? _handlerFactory;
    private RpcOutboundCallTracker? _outboundCalls;
    private RpcConnector? _connector;

    private ILogger Log => _log ??= Services.LogFor(GetType());

    internal RpcHandlerFactory HandlerFactory => _handlerFactory ??= Services.GetRequiredService<RpcHandlerFactory>();
    internal RpcOutboundCallTracker OutboundCalls => _outboundCalls ??= Services.GetRequiredService<RpcOutboundCallTracker>();
    internal RpcConnector Connector => _connector ??= Services.GetRequiredService<RpcConnector>();
    internal Func<Symbol, RpcPeer> PeerFactory { get; }

    public IServiceProvider Services { get; }
    public RpcConfiguration Configuration { get; }
    public RpcServiceRegistry ServiceRegistry => _serviceRegistry ??= Services.GetRequiredService<RpcServiceRegistry>();

    public ConcurrentDictionary<Symbol, RpcPeer> Peers { get; } = new();
    public RpcPeer this[Symbol name] => Peers.GetOrAdd(name, CreatePeer);

    public RpcHub(IServiceProvider services)
    {
        Services = services;
        Configuration = services.GetRequiredService<RpcConfiguration>();
        PeerFactory = services.GetRequiredService<Func<Symbol, RpcPeer>>();
    }

    protected override Task DisposeAsyncCore()
    {
        var disposeTasks = new List<Task>();
        foreach (var (_, connection) in Peers)
            disposeTasks.Add(connection.DisposeAsync().AsTask());
        return Task.WhenAll(disposeTasks);
    }

    private RpcPeer CreatePeer(Symbol name)
    {
        if (WhenDisposed != null)
            throw Errors.AlreadyDisposed();

        var peer = PeerFactory.Invoke(name);
        _ = peer.Run().ContinueWith(
            _ => Peers.TryRemove(name, peer),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return peer;
    }
}
