namespace Stl.Rpc.Infrastructure;

public class RpcPeerResolver : RpcServiceBase
{
    public RpcPeerResolver(IServiceProvider services) : base(services) { }

    public virtual Symbol Resolve(RpcOutboundContext context)
        => Symbol.Empty;
}
