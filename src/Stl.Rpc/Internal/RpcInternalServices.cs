using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Internal;

public readonly record struct RpcInternalServices(RpcHub Hub)
{
    public RpcServiceNameBuilder ServiceNameBuilder => Hub.ServiceNameBuilder;
    public RpcMethodNameBuilder MethodNameBuilder => Hub.MethodNameBuilder;
    public RpcCallRouter CallRouter => Hub.CallRouter;
    public RpcArgumentSerializer ArgumentSerializer => Hub.ArgumentSerializer;
    public RpcInboundContextFactory InboundContextFactory => Hub.InboundContextFactory;
    public RpcInboundMiddlewares InboundMiddlewares => Hub.InboundMiddlewares;
    public RpcOutboundMiddlewares OutboundMiddlewares => Hub.OutboundMiddlewares;
    public RpcPeerFactory PeerFactory => Hub.PeerFactory;
    public RpcClientConnectionFactory ClientConnectionFactory => Hub.ClientConnectionFactory;
    public RpcClientIdGenerator ClientIdGenerator => Hub.ClientIdGenerator;
    public RpcClientPeerReconnectDelayer ClientPeerReconnectDelayer => Hub.ClientPeerReconnectDelayer;
    public RpcUnrecoverableErrorDetector UnrecoverableErrorDetector => Hub.UnrecoverableErrorDetector;
    public IEnumerable<RpcPeerTracker> PeerTrackers => Hub.PeerTrackers;
    public RpcSystemCallSender SystemCallSender => Hub.SystemCallSender;
    public RpcClient Client => Hub.Client;

    public ConcurrentDictionary<RpcPeerRef, RpcPeer> Peers => Hub.Peers;
}
