using Stl.Generators;
using Stl.Interception;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public delegate Symbol RpcServiceNameBuilder(Type serviceType);
public delegate Symbol RpcMethodNameBuilder(RpcMethodDef methodDef);
public delegate void RpcPeerTracker(RpcPeer peer);
public delegate RpcInboundContext RpcInboundContextFactory(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken);
public delegate RpcPeer? RpcCallRouter(RpcMethodDef methodDef, ArgumentList arguments);
public delegate Task<Channel<RpcMessage>> RpcClientChannelFactory(RpcClientPeer peer, CancellationToken cancellationToken);
public delegate string RpcClientIdGenerator();
public delegate bool RpcBackendServiceDetector(Type serviceType, Symbol serviceName);
public delegate bool RpcUnrecoverableErrorDetector(Exception error, CancellationToken cancellationToken);

public static class RpcDefaultDelegates
{
    public static RpcServiceNameBuilder ServiceNameBuilder { get; set; } =
        static serviceType => serviceType.GetName();

    public static RpcMethodNameBuilder MethodNameBuilder { get; set; } =
        static methodDef => $"{methodDef.Method.Name}:{methodDef.RemoteParameterTypes.Length}";

    public static RpcCallRouter CallRouter { get; set; } =
        static (methodDef, arguments) => methodDef.Hub.GetPeer(RpcPeerRef.Default);

    public static RpcInboundContextFactory InboundContextFactory { get; set; } =
        static (peer, message, cancellationToken) => new RpcInboundContext(peer, message, cancellationToken);

    public static RpcClientChannelFactory ClientChannelFactory { get; set; } =
        static (peer, cancellationToken) => peer.Hub.Client.CreateChannel(peer, cancellationToken);

    public static RpcClientIdGenerator ClientIdGenerator { get; set; } =
        static () => RandomStringGenerator.Default.Next(32);

    public static RpcBackendServiceDetector BackendServiceDetector { get; set; }=
        static (serviceType, serviceName) =>
            serviceType.Name.EndsWith("Backend", StringComparison.Ordinal)
            || serviceName.Value.StartsWith("backend.", StringComparison.Ordinal);

    public static RpcUnrecoverableErrorDetector UnrecoverableErrorDetector { get; set; } =
        static (error, cancellationToken)
            => cancellationToken.IsCancellationRequested
            || error is ConnectionUnrecoverableException or TimeoutException;
}
