using System.Web.Http;

namespace Stl.Rpc.Server;

public static class HttpConfigurationExt
{
    public static HttpConfiguration AddDependencyResolver(
        this HttpConfiguration httpConfiguration,
        Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();
        configureServices.Invoke(services);
        httpConfiguration.DependencyResolver = services.BuildServiceProvider().ToDependencyResolver();
        return httpConfiguration;
    }
}
