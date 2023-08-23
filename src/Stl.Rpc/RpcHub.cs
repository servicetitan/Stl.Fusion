using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Rpc;

public sealed class RpcHub : ProcessorBase, IHasServices
{
    private RpcServiceRegistry? _serviceRegistry;
    private IEnumerable<RpcPeerTracker>? _peerTrackers;
    private RpcSystemCallSender? _systemCallSender;
    private RpcClient? _client;
    private IMomentClock? _clock;

    internal RpcServiceNameBuilder ServiceNameBuilder { get; }
    internal RpcMethodNameBuilder MethodNameBuilder { get; }
    internal RpcCallRouter CallRouter { get; }
    internal RpcArgumentSerializer ArgumentSerializer { get; }
    internal RpcInboundContextFactory InboundContextFactory { get; }
    internal RpcInboundMiddlewares InboundMiddlewares { get; }
    internal RpcOutboundMiddlewares OutboundMiddlewares { get; }
    internal RpcPeerFactory PeerFactory { get; }
    internal RpcClientConnectionFactory ClientConnectionFactory { get; }
    internal RpcClientIdGenerator ClientIdGenerator { get; }
    internal RpcClientPeerReconnectDelayer ClientPeerReconnectDelayer { get; }
    internal RpcBackendServiceDetector BackendServiceDetector { get; }
    internal RpcUnrecoverableErrorDetector UnrecoverableErrorDetector { get; }
    internal IEnumerable<RpcPeerTracker> PeerTrackers => _peerTrackers ??= Services.GetRequiredService<IEnumerable<RpcPeerTracker>>();
    internal RpcSystemCallSender SystemCallSender => _systemCallSender ??= Services.GetRequiredService<RpcSystemCallSender>();
    internal RpcClient Client => _client ??= Services.GetRequiredService<RpcClient>();
    internal IMomentClock Clock => _clock ??= Services.Clocks().CpuClock;

    internal ConcurrentDictionary<RpcPeerRef, RpcPeer> Peers { get; } = new();

    public IServiceProvider Services { get; }
    public RpcConfiguration Configuration { get; }
    public RpcServiceRegistry ServiceRegistry => _serviceRegistry ??= Services.GetRequiredService<RpcServiceRegistry>();
    public RpcInternalServices InternalServices => new(this);

    public RpcHub(IServiceProvider services)
    {
        Services = services;
        Configuration = services.GetRequiredService<RpcConfiguration>();
        Configuration.Freeze();

        // Delegates
        ServiceNameBuilder = services.GetRequiredService<RpcServiceNameBuilder>();
        MethodNameBuilder = services.GetRequiredService<RpcMethodNameBuilder>();
        CallRouter = services.GetRequiredService<RpcCallRouter>();
        ArgumentSerializer = services.GetRequiredService<RpcArgumentSerializer>();
        InboundContextFactory = services.GetRequiredService<RpcInboundContextFactory>();
        InboundMiddlewares = services.GetRequiredService<RpcInboundMiddlewares>();
        OutboundMiddlewares = services.GetRequiredService<RpcOutboundMiddlewares>();
        PeerFactory = services.GetRequiredService<RpcPeerFactory>();
        ClientConnectionFactory = services.GetRequiredService<RpcClientConnectionFactory>();
        ClientIdGenerator = services.GetRequiredService<RpcClientIdGenerator>();
        ClientPeerReconnectDelayer = services.GetRequiredService<RpcClientPeerReconnectDelayer>();
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

    public RpcPeer GetPeer(RpcPeerRef peerRef)
    {
        if (Peers.TryGetValue(peerRef, out var peer))
            return peer;

        lock (Lock) {
            if (Peers.TryGetValue(peerRef, out peer))
                return peer;
            if (WhenDisposed != null)
                throw Errors.AlreadyDisposed(GetType());

            peer = PeerFactory.Invoke(this, peerRef);
            Peers[peerRef] = peer;
            peer.Start();
            return peer;
        }
    }

    public RpcClientPeer GetClientPeer(RpcPeerRef peerRef)
        => (RpcClientPeer)GetPeer(peerRef.RequireClient());

    public RpcServerPeer GetServerPeer(RpcPeerRef peerRef)
        => (RpcServerPeer)GetPeer(peerRef.RequireServer());
}
