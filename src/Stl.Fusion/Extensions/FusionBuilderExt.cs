using Stl.Fusion.Extensions.Internal;
using Stl.Rpc;

namespace Stl.Fusion.Extensions;

public static class FusionBuilderExt
{
    public static FusionBuilder AddFusionTime(this FusionBuilder fusion,
        Func<IServiceProvider, FusionTime.Options>? optionsFactory = null)
    {
        var services = fusion.Services;
        services.AddSingleton(optionsFactory, _ => FusionTime.Options.Default);
        if (!services.HasService<IFusionTime>())
            fusion.AddService<IFusionTime, FusionTime>();
        return fusion;
    }

    public static FusionBuilder AddRpcPeerStateMonitor(this FusionBuilder fusion,
        Func<IServiceProvider, RpcPeerRef>? peerRefResolver = null)
    {
        var services = fusion.Services;
        services.AddSingleton(c => {
            var monitor = new RpcPeerStateMonitor(c);
            if (peerRefResolver != null)
                monitor.PeerRef = peerRefResolver.Invoke(c);
            return monitor;
        });
        return fusion;
    }
}
