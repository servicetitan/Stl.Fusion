using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Internal;
using Stl.Testing;
using Stl.Testing.Internal;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;

namespace Stl.Fusion.Tests
{
    public abstract class SimpleFusionTestBase : TestBase
    {
        protected SimpleFusionTestBase(ITestOutputHelper @out) : base(@out) { }

        protected IServiceProvider CreateServiceProvider(Action<IServiceCollection>? configureServices = null)
        {
            ComputedRegistry.Instance = new ComputedRegistry(new ComputedRegistry.Options() {
                InitialCapacity = 16,
            });
            var services = new ServiceCollection();
            services.AddLogging(logging => {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddDebug();
                logging.AddProvider(
                    new XunitTestOutputLoggerProvider(
                        new TestOutputHelperAccessor(Out)));
            });
            services.AddFusion();
            ConfigureCommonServices(services);
            configureServices?.Invoke(services);
            return services.BuildServiceProvider();
        }

        protected IServiceProvider CreateServiceProviderFor<TService>()
            where TService : class
            => CreateServiceProvider(services => services.AddFusion().AddComputeService<TService>());

        protected abstract void ConfigureCommonServices(ServiceCollection services);

        protected virtual Task Delay(double seconds)
            => Timeouts.Clock.Delay(TimeSpan.FromSeconds(seconds));

        protected void GCCollect()
        {
            for (var i = 0; i < 3; i++) {
                GC.Collect();
                Thread.Sleep(10);
            }
        }
    }
}
