namespace Stl.Rpc.Infrastructure;

public class RpcCallFactoryProvider : RpcServiceBase
{
    public RpcCallFactoryProvider(IServiceProvider services) : base(services) { }

    public RpcCallFactory Create(RpcMethodDef methodDef)
    {
        if (!methodDef.IsValid)
            throw new ArgumentOutOfRangeException(nameof(methodDef));

        var (inboundCallType, outboundCallType) = GetCallTypes(methodDef);
        return RpcCallFactory.New(methodDef, inboundCallType, outboundCallType);
    }

    // Protected methods

    protected virtual (Type InboundCallType, Type OutboundCallType) GetCallTypes(RpcMethodDef methodDef)
        => (typeof(RpcInboundCall<>), typeof(RpcOutboundCall<>));
}
