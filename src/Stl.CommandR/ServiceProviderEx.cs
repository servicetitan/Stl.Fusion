using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR
{
    public static class ServiceProviderEx
    {
        public static ICommandDispatcher CommandDispatcher(this IServiceProvider services)
            => services.GetRequiredService<ICommandDispatcher>();
    }
}
