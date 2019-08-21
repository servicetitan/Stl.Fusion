using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Extensibility;
using Stl.Plugins;

namespace Stl.Bootstrap.Cli
{
    public interface ICliHost
    {
        IServiceProvider CoreServices { get; } 
        IServiceProvider Plugins { get; }
        Task<int> RunAsync(string[] args);
    }

    public abstract class CliHostBase : ICliHost
    {
        public IServiceProvider CoreServices { get; } 
        public IServiceProvider Plugins { get; }
        public Type PluginType { get; }

        protected CliHostBase(
            Type? pluginType = null,
            IServiceProvider? coreServices = null, 
            IServiceProvider? plugins = null)
        {
            PluginType = pluginType ?? typeof(ICliHostPlugin);
            CoreServices = coreServices ?? CreateCoreServices();
            Plugins = plugins ?? CreatePlugins();
        }

        public async Task<int> RunAsync(string[] args)
        {
            var invocation =  new CliHostPluginConfigureInvocation() {
                Plugins = Plugins.GetPlugins<ICliHostPlugin>().ToImmutableArray(),
            };
            invocation.Invoke((plugin, chain) => plugin.Configure(chain));
            var result = await invocation.RootCommand.InvokeAsync(args, CoreServices.GetService<IConsole>());
            return result;
        }

        protected virtual IServiceProvider CreateCoreServices()
        {
            var services = (IServiceCollection) new ServiceCollection();
            ConfigureCoreServices(services);
            var factory = new DefaultServiceProviderFactory();
            return factory.CreateServiceProvider(services);
        }

        protected virtual IServiceProvider CreatePlugins()
        {
            var plugins = (IServiceCollection) new ServiceCollection();
            ConfigurePlugins(plugins);
            var factory = new DefaultServiceProviderFactory();
            return factory.CreateServiceProvider(plugins);
        }

        protected virtual void ConfigureCoreServices(IServiceCollection services)
        {
            services.AddSingleton<ICliHost>(this);
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton<IPluginFinder, PluginFinder>();
            services.AddSingleton<IConsole>(new SystemConsole());
        }

        protected virtual void ConfigurePlugins(IServiceCollection plugins)
        {
            var pluginFinder = CoreServices.GetService<IPluginFinder>();
            var pluginSetInfo = pluginFinder.FindPlugins();
            
            plugins.CopySingleton<ICliHost>(CoreServices);
            plugins.CopySingleton<ILoggerFactory>(CoreServices);
            plugins.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            plugins.CopySingleton<IConsole>(CoreServices);
            
            var containerBuilder = new PluginContainerBuilder() {
                Configuration = new PluginContainerConfiguration(pluginSetInfo, PluginType),
            };
            containerBuilder.ConfigureServices(plugins);
        }
    }
}
