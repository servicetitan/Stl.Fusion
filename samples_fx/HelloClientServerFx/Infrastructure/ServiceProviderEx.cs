using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace HelloClientServerFx
{
    public static class ServiceProviderEx
    {
        public static IServiceCollection AddControllersAsServices(this IServiceCollection services,
            IEnumerable<Type> controllerTypes)
        {
            foreach (var type in controllerTypes)
            {
                services.AddTransient(type);
            }

            return services;
        }
    }
}