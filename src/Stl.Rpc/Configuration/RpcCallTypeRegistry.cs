using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public static class RpcCallTypeRegistry
{
    private static volatile (Type? InboundCallType, Type? OutboundCallType)[] _callTypes;
    private static readonly object Lock = new();

    static RpcCallTypeRegistry()
    {
        _callTypes = new (Type?, Type?)[256];
        _callTypes[RpcCallTypes.Regular] = (typeof(RpcInboundCall<>), typeof(RpcOutboundCall<>));
        _callTypes[RpcCallTypes.Stream] = (typeof(RpcInboundCall<>), typeof(RpcOutboundStreamCall<>));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Type InboundCallType, Type OutboundCallType) Resolve(byte callTypeId)
    {
        var item = _callTypes[callTypeId];
        if (item.InboundCallType == null)
            throw Errors.UnknownCallType(callTypeId);
        return item!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Type? InboundCallType, Type? OutboundCallType) Get(byte callTypeId)
        => _callTypes[callTypeId];

    public static void Register(byte callTypeId, Type inboundCallType, Type outboundCallType)
    {
        if (!typeof(RpcInboundCall).IsAssignableFrom(inboundCallType))
            throw Stl.Internal.Errors.MustBeAssignableTo<RpcInboundCall>(inboundCallType, nameof(inboundCallType));
        if (!typeof(RpcOutboundCall).IsAssignableFrom(outboundCallType))
            throw Stl.Internal.Errors.MustBeAssignableTo<RpcOutboundCall>(outboundCallType, nameof(outboundCallType));

        if (!inboundCallType.IsGenericType || inboundCallType.GetGenericArguments().Length != 1)
            throw new ArgumentOutOfRangeException(nameof(inboundCallType));
        if (!outboundCallType.IsGenericType || outboundCallType.GetGenericArguments().Length != 1)
            throw new ArgumentOutOfRangeException(nameof(outboundCallType));

        var item = (inboundCallType, outboundCallType);
        if (Get(callTypeId) == item)
            return;

        lock (Lock) {
            var existingItem = Get(callTypeId);
            if (existingItem == item)
                return;

            if (existingItem.InboundCallType != null)
                throw Stl.Internal.Errors.KeyAlreadyExists();

            _callTypes[callTypeId] = item;
        }
    }
}
