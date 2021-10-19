using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR;

public static class ServiceProviderExt
{
    public static ICommander Commander(this IServiceProvider services)
        => services.GetRequiredService<ICommander>();
}
