using System.Web.Http.Dependencies;
using Stl.Rpc.Server.Internal;

namespace Stl.Rpc.Server;

public static class ServiceProviderExt
{
    public static IDependencyResolver ToDependencyResolver(this IServiceProvider services)
        => new DependencyResolver(services);
}
