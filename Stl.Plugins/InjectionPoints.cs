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
            Log.Debug($"{this}: Injecting.");
            try {
                _action.Invoke();
            }
            finally {
                Log.Debug($"{this}: Injected.");
            }
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
            Log.Debug($"{this}: Injecting.");
            try {
                return _configureServices.Invoke(services);
            }
            finally {
                Log.Debug($"{this}: Injected.");
            }
        }
    }
}
