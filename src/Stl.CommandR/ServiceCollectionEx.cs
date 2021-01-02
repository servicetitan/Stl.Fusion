using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR
{
    public static class ServiceCollectionEx
    {
        public static CommandRBuilder AddCommandR(this IServiceCollection services)
            => new(services);

        public static IServiceCollection AddCommandR(this IServiceCollection services, Action<CommandRBuilder> configureCommandR)
        {
            var commandR = services.AddCommandR();
            configureCommandR.Invoke(commandR);
            return services;
        }
    }
}
