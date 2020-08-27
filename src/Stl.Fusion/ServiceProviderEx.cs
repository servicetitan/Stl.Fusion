using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion
{
    public static class ServiceProviderEx
    {
        public static IStateFactory GetStateFactory(this IServiceProvider services)
            => services.GetRequiredService<IStateFactory>();
    }
}
