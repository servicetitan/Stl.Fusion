using Stl.Interception.Internal;

namespace Stl.Fusion;

public static class ComputeServiceExt
{
    public static bool IsClient(this IComputeService service)
        => service is InterfaceProxy;
}
