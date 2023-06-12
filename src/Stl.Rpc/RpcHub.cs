using Stl.Interception;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Rpc;

public sealed class RpcHub : ProcessorBase, IHasServices
{
    private ILogger? _log;
    private RpcServiceRegistry? _serviceRegistry;
    private IEnumerable<RpcPeerTracker>? _peerTrackers;
    private RpcClient? _client;
    private RpcSystemCallSender? _systemCallSender;

    private ILogger Log => _log ??= Services.LogFor(GetType());

    internal RpcServiceNameBuilder ServiceNameBuilder { get; }
    internal RpcMethodNameBuilder MethodNameBuilder { get; }
    internal RpcPeerFactory PeerFactory { get; }
    internal RpcCallRouter CallRouter { get; }
    internal RpcArgumentSerializer ArgumentSerializer { get; }
    internal RpcInboundContextFactory InboundContextFactory { get; }
    internal RpcClientChannelFactory ClientChannelFactory { get; }
    internal RpcClientIdGenerator ClientIdGenerator { get; }
    internal RpcBackendServiceDetector BackendServiceDetector { get; }
    internal RpcUnrecoverableErrorDetector UnrecoverableErrorDetector { get; }
    internal IEnumerable<RpcPeerTracker> PeerTrackers => _peerTrackers ??= Services.GetRequiredService<IEnumerable<RpcPeerTracker>>();
    internal RpcSystemCallSender SystemCallSender => _systemCallSender ??= Services.GetRequiredService<RpcSystemCallSender>();
    internal RpcClient Client => _client ??= Services.GetRequiredService<RpcClient>();

    public IServiceProvider Services { get; }
    public RpcConfiguration Configuration { get; }
    public RpcServiceRegistry ServiceRegistry => _serviceRegistry ??= Services.GetRequiredService<RpcServiceRegistry>();
    public RpcInternalServices InternalServices => new(this);

    public ConcurrentDictionary<Symbol, RpcPeer> Peers { get; } = new();

    public RpcHub(IServiceProvider services)
    {
        Services = services;
        Configuration = services.GetRequiredService<RpcConfiguration>();
        Configuration.Freeze();

        // Delegates
        ServiceNameBuilder = services.GetRequiredService<RpcServiceNameBuilder>();
        MethodNameBuilder = services.GetRequiredService<RpcMethodNameBuilder>();
        PeerFactory = services.GetRequiredService<RpcPeerFactory>();
        CallRouter = services.GetRequiredService<RpcCallRouter>();
        ArgumentSerializer = services.GetRequiredService<RpcArgumentSerializer>();
        InboundContextFactory = services.GetRequiredService<RpcInboundContextFactory>();
        ClientChannelFactory = services.GetRequiredService<RpcClientChannelFactory>();
        ClientIdGenerator = services.GetRequiredService<RpcClientIdGenerator>();
        BackendServiceDetector = services.GetRequiredService<RpcBackendServiceDetector>();
        UnrecoverableErrorDetector = services.GetRequiredService<RpcUnrecoverableErrorDetector>();
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

        var peer = PeerFactory.Invoke(this, id);
        _ = peer.Run();
        return peer;
    }
}
