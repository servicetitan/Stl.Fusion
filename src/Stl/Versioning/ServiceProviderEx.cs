using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Versioning
{
    public static class ServiceProviderEx
    {
        public static VersionGenerator<TVersion> VersionGenerator<TVersion>(this IServiceProvider services)
            where TVersion : notnull
            => services.GetRequiredService<VersionGenerator<TVersion>>();
    }
}
