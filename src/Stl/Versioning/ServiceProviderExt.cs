using Microsoft.Extensions.DependencyInjection;

namespace Stl.Versioning;

public static class ServiceProviderExt
{
    public static VersionGenerator<TVersion> VersionGenerator<TVersion>(this IServiceProvider services)
        where TVersion : notnull
        => services.GetRequiredService<VersionGenerator<TVersion>>();
}
