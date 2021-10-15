using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion;

public static class ServiceProviderExt
{
    public static IStateFactory StateFactory(this IServiceProvider services)
        => services.GetRequiredService<IStateFactory>();
}
