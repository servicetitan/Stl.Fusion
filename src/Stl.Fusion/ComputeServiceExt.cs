using Stl.Interception.Internal;

namespace Stl.Fusion;

public static class ComputeServiceExt
{
    public static bool IsReplicaService(this IComputeService service)
        => service is InterfaceProxy;
}
