using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Stl.Async;
using Stl.Extensibility;
using Stl.Hosting;
using Stl.Plugins;
using Stl.Testing;
using Stl.Testing.Internal;
using Stl.Tests.Hosting.Plugins;
using Stl.Time;
using Stl.Time.Testing;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;
using IPluginHostBuilder = Stl.Plugins.IPluginHostBuilder;

namespace Stl.Tests.Hosting
{
    public class TestMiniHostProvider : AsyncDisposableBase
    {
        // TestPluginTypes should never be empty, b/c their presence
        // determines whether it's a test host or a real one.
        // Which is why the default here is to have a single plugin doing nothing.
        public Type[] TestPluginTypes { get; } = { typeof(NoopMiniHostPlugin) };
        public ITestOutputHelper? Out { get; }

        public IHost Host { get; set; } = null!;
        public Uri HostUrl { get; set; } = null!;
        public IServiceProvider Services => Host.Services;
        public ITestClock Clock => (ITestClock) Services.GetRequiredService<IMomentClock>();
        public TestOutputConsole Console => (TestOutputConsole) Services.GetRequiredService<IConsole>();

        public TestMiniHostProvider(ITestOutputHelper? @out, params Type[] testPluginTypes)
        {
            Out = @out;
            if (testPluginTypes.Length != 0)
                TestPluginTypes = testPluginTypes;
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Host == null) 
                return;
            IHost host; 
            (host, Host) = (Host, null!);
            await host.StopAsync();
            host.Dispose();
        }

        public virtual async Task CreateHostAsync(params string[] arguments)
        {
            var hostUrl = WebTestEx.GetRandomLocalUri();
            var host = new MiniHostBuilder() {
                    WebHostUrls = new[] {hostUrl.ToString()}
                }
                .ConfigureTestHost(TestPluginTypes, ConfigureTestHostBuilder)
                .Build(arguments);
            if (host != null)
                await host.StartAsync().ConfigureAwait(false);
            Host = host!;
            HostUrl = hostUrl;
        }

        protected virtual void ConfigureTestHostBuilder(ITestAppHostBuilder testAppHostBuilder)
        {
            var miniHostBuilder = (MiniHostBuilder) testAppHostBuilder;
            var hasOut = Out != null;
            var console = hasOut 
                ? (IConsole) new TestOutputConsole(Out)
                : new SystemConsole();
            var testOutputLoggerProvider = hasOut
                ? new XunitTestOutputLoggerProvider(new TestOutputHelperAccessor(Out!))
                : null;

            testAppHostBuilder.InjectPreBuilder<IPluginHostBuilder>(pluginHostBuilder => {
                pluginHostBuilder.ConfigureServices((_, services) => {
                    if (hasOut)
                        services.TryAddSingleton(Out);
                    services.TryAddSingleton(console);
                    services.AddLogging(logging => {
                        if (hasOut)
                            logging.AddProvider(testOutputLoggerProvider);
                        else
                            logging.AddProvider(new DebugLoggerProvider());
                    });
                });
            });
            testAppHostBuilder.InjectPreBuilder<IHostBuilder>(hostBuilder => {
                hostBuilder.ConfigureServices((ctx, services) => {
                    var plugins = miniHostBuilder.BuildState.PluginHost;
                    services.TryCopySingleton<ITestOutputHelper>(plugins);
                    services.TryCopySingleton<IConsole>(plugins);
                    services.AddLogging(logging => {
                        if (hasOut)
                            logging.AddProvider(testOutputLoggerProvider);
                        else
                            logging.AddProvider(new DebugLoggerProvider());
                    });
                });
            });
        }
    }
}
