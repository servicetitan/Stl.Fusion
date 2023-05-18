using Stl.Interception.Interceptors;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcCallFactory
{
    private static readonly ConcurrentDictionary<Type, Func<RpcMethodDef, Type, Type, RpcCallFactory>> FactoryCache = new();

    public RpcMethodDef MethodDef { get; }
    public Type InboundCallType { get; }
    public Type OutboundCallType { get; }
    public Func<RpcInboundContext, IRpcInboundCall> CreateInbound { get; protected init; } = null!;
    public Func<RpcOutboundContext, IRpcOutboundCall> CreateOutbound { get; protected init; } = null!;

    public static RpcCallFactory New(RpcMethodDef methodDef, Type inboundCallType, Type outboundCallType)
    {
        var factory = FactoryCache.GetOrAdd(
            methodDef.UnwrappedReturnType,
            tResult => (Func<RpcMethodDef, Type, Type, RpcCallFactory>)typeof(RpcCallFactory<>)
                .MakeGenericType(tResult)
                .GetConstructorDelegate(typeof(RpcMethodDef), typeof(Type), typeof(Type))!);
        return factory.Invoke(methodDef, inboundCallType, outboundCallType);
    }

    protected RpcCallFactory(RpcMethodDef methodDef, Type inboundCallType, Type outboundCallType)
    {
        if (!inboundCallType.IsGenericTypeDefinition || !typeof(IRpcInboundCall).IsAssignableFrom(inboundCallType))
            throw new ArgumentOutOfRangeException(nameof(inboundCallType));
        if (!outboundCallType.IsGenericTypeDefinition || !typeof(IRpcOutboundCall).IsAssignableFrom(outboundCallType))
            throw new ArgumentOutOfRangeException(nameof(inboundCallType));

        MethodDef = methodDef;
        InboundCallType = inboundCallType;
        OutboundCallType = outboundCallType;
    }
}

public sealed class RpcCallFactory<TResult> : RpcCallFactory
{
    public RpcCallFactory(RpcMethodDef methodDef, Type inboundCallType, Type outboundCallType)
        : base(methodDef, inboundCallType, outboundCallType)
    {
        var tInbound = inboundCallType.MakeGenericType(typeof(TResult));
        var tOutbound = outboundCallType.MakeGenericType(typeof(TResult));
        CreateInbound = (Func<RpcInboundContext, IRpcInboundCall>)tInbound.GetConstructorDelegate(typeof(RpcInboundContext))!;
        CreateOutbound = (Func<RpcOutboundContext, IRpcOutboundCall>)tOutbound.GetConstructorDelegate(typeof(RpcOutboundContext))!;
    }
}
