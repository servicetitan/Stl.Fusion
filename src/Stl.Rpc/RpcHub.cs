using Stl.Interception;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Rpc;

public sealed class RpcHub : ProcessorBase, IHasServices
{
    private ILogger? _log;
    private RpcServiceRegistry? _serviceRegistry;
    private RpcPeerFactory? _peerFactory;
    private IEnumerable<RpcPeerTracker>? _peerTrackers;
    private RpcCallRouter? _peerResolver;
    private RpcInboundContextFactory? _inboundContextFactory;
    private RpcClientChannelProvider? _clientChannelProvider;
    private RpcClientIdGenerator? _clientIdGenerator;
    private RpcSystemCallSender? _systemCallSender;
    private RpcErrorClassifier? _errorClassifier;

    private ILogger Log => _log ??= Services.LogFor(GetType());

    internal RpcPeerFactory PeerFactory => _peerFactory ??= Services.GetRequiredService<RpcPeerFactory>();
    internal IEnumerable<RpcPeerTracker> PeerTrackers => _peerTrackers ??= Services.GetRequiredService<IEnumerable<RpcPeerTracker>>();
    internal RpcCallRouter CallRouter => _peerResolver ??= Services.GetRequiredService<RpcCallRouter>();
    internal RpcInboundContextFactory InboundContextFactory => _inboundContextFactory ??= Services.GetRequiredService<RpcInboundContextFactory>();
    internal RpcClientChannelProvider ClientChannelProvider => _clientChannelProvider ??= Services.GetRequiredService<RpcClientChannelProvider>();
    internal RpcClientIdGenerator ClientIdGenerator => _clientIdGenerator ??= Services.GetRequiredService<RpcClientIdGenerator>();
    internal RpcSystemCallSender SystemCallSender => _systemCallSender ??= Services.GetRequiredService<RpcSystemCallSender>();
    internal RpcErrorClassifier ErrorClassifier => _errorClassifier ??= Services.GetRequiredService<RpcErrorClassifier>();

    public IServiceProvider Services { get; }
    public RpcConfiguration Configuration { get; }
    public RpcServiceRegistry ServiceRegistry => _serviceRegistry ??= Services.GetRequiredService<RpcServiceRegistry>();
    public RpcHubInternals Internals => new(this);

    public ConcurrentDictionary<Symbol, RpcPeer> Peers { get; } = new();

    public RpcHub(IServiceProvider services)
    {
        Services = services;
        Configuration = services.GetRequiredService<RpcConfiguration>();
        Configuration.Freeze();
    }

    protected override Task DisposeAsyncCore()
    {
        var disposeTasks = new List<Task>();
        foreach (var (_, peer) in Peers)
            disposeTasks.Add(peer.DisposeAsync().AsTask());
        return Task.WhenAll(disposeTasks);
    }

    public TClient CreateClient<TService, TClient>()
        where TClient : TService
        => (TClient)CreateClient(typeof(TService), typeof(TClient));

    public TService CreateClient<TService>(Type? clientType = null)
        => (TService)CreateClient(typeof(TService), clientType);

    public object CreateClient(Type serviceType, Type? clientType = null)
    {
        if (clientType == null)
            clientType = serviceType;
        else if (!serviceType.IsAssignableFrom(clientType))
            throw Errors.MustBeAssignableTo(clientType, serviceType, nameof(clientType));

        var interceptor = Services.GetRequiredService<RpcClientInterceptor>();
        interceptor.Setup(ServiceRegistry[serviceType]);
        var proxy = Proxies.New(clientType, interceptor);
        return proxy;
    }

    public RpcPeer GetPeer(Symbol id)
    {
        while (true) {
            var peer = Peers.GetOrAdd(id, CreatePeer);
            if (peer.WhenRunning?.IsCompleted == false)
                return peer;
        }
    }

    // Private methods

    private RpcPeer CreatePeer(Symbol id)
    {
        if (WhenDisposed != null)
            throw Errors.AlreadyDisposed();

        var peer = PeerFactory.Invoke(id);
        _ = peer.Run();
        return peer;
    }
}
