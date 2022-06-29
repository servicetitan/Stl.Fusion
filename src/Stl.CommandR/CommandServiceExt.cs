using Stl.CommandR.Interception;
using Stl.Interception;

namespace Stl.CommandR;

public static class CommandServiceExt
{
    public static IServiceProvider GetServices(this ICommandService service)
        => ProxyExt.GetServices(service);

    public static ICommander GetCommander(this ICommandService service)
        => service.GetServices().Commander();
}
