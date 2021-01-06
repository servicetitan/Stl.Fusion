using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR
{
    public static class ServiceProviderEx
    {
        public static ICommander Commander(this IServiceProvider services)
            => services.GetRequiredService<ICommander>();
    }
}
