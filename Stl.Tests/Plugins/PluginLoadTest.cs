using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Extensions.Logging;
using Serilog;
using Stl.Plugins;
using Stl.Testing;
using Stl.Tests.Plugins;
using Xunit;
using Xunit.Abstractions;

namespace Bach.Tests
{
    public class PluginLoadTest : ConsoleInterceptingTestBase
    {
        public class BachHost : PluginHostBase<TestPlugin>
        {
            public IServiceProvider Services { get; }

            public BachHost(ILoggerFactory? loggerFactory = null)
            {
                LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            
                Logger.LogInformation("Initializing plugins.");
                InitializePlugins(PluginLoader.Load(extraAssemblies: new [] {
                    Assembly.GetExecutingAssembly(),
                }));

                Logger.LogInformation("Creating services.");
                // ReSharper disable once VirtualMemberCallInConstructor
                Services = CreateServices();

                Logger.LogInformation("Starting plugins.");
                this.Inject<StartupInjectionPoint>(p => p.Inject());

                Logger.LogInformation("Host is ready.");
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

        public PluginLoadTest(ITestOutputHelper @out) : base(@out) { }
        
        [Fact]
        public void BasicTest()
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "{Timestamp:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            var loggerFactory = new LoggerFactory(new [] {
                new SerilogLoggerProvider(logger), 
            });
            using (var _ = new BachHost(loggerFactory)) {
                Out.WriteLine("BachHost created.");
            }
        }
    }
}
