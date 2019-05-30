using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Plugins 
{
    public class StartupInjectionPoint : InjectionPoint
    {
        private readonly Action _action;

        public StartupInjectionPoint(Action action) => _action = action;

        public void Inject()
        {
            using var scope = Logger.BeginScope($"{this}: injection");
            _action.Invoke();
        }
    }

    public class ConfigureServicesInjectionPoint : InjectionPoint
    {
        private readonly Func<IServiceCollection, IServiceCollection> _configureServices;

        public ConfigureServicesInjectionPoint(Func<IServiceCollection, IServiceCollection> configureServices) => 
            _configureServices = configureServices;
        public ConfigureServicesInjectionPoint(Action<IServiceCollection> configureServices) => 
            _configureServices = services => {
                configureServices.Invoke(services);
                return services;
            };

        public IServiceCollection Inject(IServiceCollection services)
        {
            using var scope = Logger.BeginScope($"{this}: injection");
            return _configureServices.Invoke(services);
        }
    }
}
