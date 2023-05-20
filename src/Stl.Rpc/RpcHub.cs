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
    private RpcPeerFactory? _peerFactory;
    private RpcPeerConnector? _peerConnector;
    private RpcPeerResolver? _peerResolver;
    private RpcSystemCallSender? _systemCallSender;

    private ILogger Log => _log ??= Services.LogFor(GetType());

    internal RpcCallFactoryProvider CallFactoryProvider => _callFactoryProvider ??= Services.GetRequiredService<RpcCallFactoryProvider>();
    internal RpcPeerFactory PeerFactory => _peerFactory ??= Services.GetRequiredService<RpcPeerFactory>();
    internal RpcPeerConnector PeerConnector => _peerConnector ??= Services.GetRequiredService<RpcPeerConnector>();
    internal RpcPeerResolver PeerResolver => _peerResolver ??= Services.GetRequiredService<RpcPeerResolver>();
    internal RpcSystemCallSender SystemCallSender => _systemCallSender ??= Services.GetRequiredService<RpcSystemCallSender>();

    public IServiceProvider Services { get; }
    public RpcConfiguration Configuration { get; }
    public RpcServiceRegistry ServiceRegistry => _serviceRegistry ??= Services.GetRequiredService<RpcServiceRegistry>();

    public ConcurrentDictionary<Symbol, RpcPeer> Peers { get; } = new();

    public RpcHub(IServiceProvider services)
    {
        Services = services;
        Configuration = services.GetRequiredService<RpcConfiguration>();
    }

    protected override Task DisposeAsyncCore()
    {
        var disposeTasks = new List<Task>();
        foreach (var (_, peer) in Peers)
            disposeTasks.Add(peer.DisposeAsync().AsTask());
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

    public RpcPeer GetPeer(Symbol name)
        => Peers.GetOrAdd(name, CreatePeer);

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
