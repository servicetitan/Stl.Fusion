using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Stl.Plugins;

namespace Stl.Tests.Plugins 
{
    public class TestPluginHost : PluginHostBase<TestPlugin>
    {
        public IServiceProvider Services { get; }

        public TestPluginHost(ILogger? log = null) : base(log)
        {
            Log.Information("Initializing plugins.");
            InitializePlugins(PluginLoader.Load(extraAssemblies: new [] {
                Assembly.GetExecutingAssembly(),
            }));

            Log.Information("Creating services.");
            // ReSharper disable once VirtualMemberCallInConstructor
            Services = CreateServices();

            Log.Information("Starting plugins.");
            this.Inject<StartupInjectionPoint>(p => p.Inject());

            Log.Information("Host is ready.");
        }

        protected virtual IServiceProvider CreateServices()
        {
            var services = (IServiceCollection) new ServiceCollection();
            ConfigureServices(ref services);
            var factory = new DefaultServiceProviderFactory();
            return factory.CreateServiceProvider(services);
        }
        
        protected virtual void ConfigureServices(ref IServiceCollection services)
        {
            services.AddSingleton(this);
            services = this.Inject<ConfigureServicesInjectionPoint, IServiceCollection>(
                services, (p, s) => p.Inject(s));
        }
    }
}
