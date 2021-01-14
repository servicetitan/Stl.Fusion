using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion
{
    public static class ServiceProviderEx
    {
        public static IStateFactory StateFactory(this IServiceProvider services)
            => services.GetRequiredService<IStateFactory>();
    }
}
