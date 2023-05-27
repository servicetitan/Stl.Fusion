using Stl.Interception;
using Stl.Interception.Internal;

namespace Stl.Fusion;

public static class ComputeServiceExt
{
    public static bool IsReplicaService(this IComputeService service)
        => service is InterfaceProxy;

    public static IServiceProvider GetServices(this IComputeService service)
        => ProxyExt.GetServices(service);

    public static ICommander GetCommander(this IComputeService service)
        => service.GetServices().Commander();
}
