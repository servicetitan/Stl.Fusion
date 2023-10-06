using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public static class RpcInboundContextExt
{
    public static LTag GetResultVersion(this RpcInboundContext? context)
    {
        var versionHeader = context?.Message.Headers.GetOrDefault(FusionRpcHeaders.Version.Name);
        return versionHeader is { } vVersionHeader
            ? LTag.TryParse(vVersionHeader.Value, out var v) ? v : default
            : default;
    }
}
