using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Extensions.Internal;
using Stl.Rpc;

namespace Stl.Fusion.Extensions;

public static class FusionBuilderExt
{
    public static FusionBuilder AddFusionTime(this FusionBuilder fusion,
        Func<IServiceProvider, FusionTime.Options>? optionsFactory = null)
    {
        var services = fusion.Services;
        services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        fusion.AddService<IFusionTime, FusionTime>();
        return fusion;
    }

    public static FusionBuilder AddRpcPeerConnectionMonitor(this FusionBuilder fusion,
        Func<IServiceProvider, RpcPeerRef>? peerRefResolver = null)
    {
        var services = fusion.Services;
        services.AddSingleton(c => {
            var monitor = new RpcPeerConnectionMonitor(c);
            if (peerRefResolver != null)
                monitor.PeerRef = peerRefResolver.Invoke(c);
            return monitor;
        });
        return fusion;
    }
}
