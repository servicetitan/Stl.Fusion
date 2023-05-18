using Stl.Interception;
using Stl.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public sealed class RpcHub : ProcessorBase, IHasServices
{
    private readonly ConcurrentDictionary<Type, object> _clients = new();
    private ILogger? _log;
    private RpcServiceRegistry? _serviceRegistry;
    private RpcCallFactoryProvider? _callFactoryProvider;
    private RpcConnector? _connector;
    private RpcPeerResolver? _peerResolver;

    private ILogger Log => _log ??= Services.LogFor(GetType());

    internal RpcCallFactoryProvider CallFactoryProvider => _callFactoryProvider ??= Services.GetRequiredService<RpcCallFactoryProvider>();
    internal RpcConnector Connector => _connector ??= Services.GetRequiredService<RpcConnector>();
    internal RpcPeerResolver PeerResolver => _peerResolver ??= Services.GetRequiredService<RpcPeerResolver>();
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

    public object GetClient(Type serviceOrClientType)
    {
        var serviceDef = ServiceRegistry[serviceOrClientType];
        return _clients.GetOrAdd(serviceDef.ClientType, static (type, self) => {
            var interceptor = self.Services.GetRequiredService<RpcClientInterceptor>();
            var proxy = Proxies.New(type, interceptor);
            return proxy;
        }, this);
    }

    // Private methods

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
