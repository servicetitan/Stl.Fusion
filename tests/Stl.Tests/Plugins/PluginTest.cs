using Microsoft.Extensions.DependencyInjection;
using Stl.Caching;
using Stl.Plugins;
using Stl.Reflection;
using Stl.Testing.Output;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.Plugins;

public class PluginTest : TestBase
{
    public PluginTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void PluginHostBuilderTest()
    {
        var host = new PluginHostBuilder()
            .UsePluginFilter(typeof(ITestPlugin))
            .Build();

        RunPluginHostTests(host);
    }

    [Fact]
    public async Task AbstractPluginTest()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await new PluginHostBuilder()
                .UsePlugins(typeof(ITestPlugin))
                .BuildAsync();
        });
    }

    [Fact]
    public void PluginFilterTest()
    {
        var host = new PluginHostBuilder()
            .UsePluginFilter(typeof(ITestPlugin))
            .Build();
        host.GetPlugins<ITestPlugin>().Count().Should().Be(2);

        host = new PluginHostBuilder()
            .UsePluginFilter(typeof(ITestPlugin))
            .UsePluginFilter(p => p.Type != typeof(TestPlugin2))
            .Build();
        host.GetPlugins<ITestPlugin>().Count().Should().Be(1);
    }

    [Fact]
    public void SingletonPluginTest()
    {
        var host = new PluginHostBuilder()
            .UsePluginFilter(typeof(ITestPlugin))
            .Build();

        host.GetPlugins<ITestPlugin>().Count().Should().Be(2);
        host.GetPlugins<ITestSingletonPlugin>().Count().Should().Be(1);
    }

    [Fact]
    public async Task CombinedTest()
    {
        using (var capture = CaptureOutput()) {
            await RunCombinedTest(true);
            capture.Resource.Should().ContainAll("populating");
        }

        using (var capture = CaptureOutput()) {
            await RunCombinedTest();
            capture.Resource.Should().ContainAll("Cached plugin set info found");
        }
    }

    private PluginHostBuilder CreateHostBuilder(bool mustClearCache = false)
    {
        var hostBuilder = new PluginHostBuilder()
            .UsePluginFilter(typeof(ITestPlugin))
            .ConfigureServices(services => {
                services.AddLogging(logging => {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddDebug();
                    logging.AddProvider(new XunitTestOutputLoggerProvider(
                        new TestOutputHelperAccessor(Out),
                        (category, level) => level >= LogLevel.Debug));
                });
            });
        if (mustClearCache) {
            var serviceProvider = hostBuilder.ServiceProviderFactory(hostBuilder.Services);
            var pluginFinder = serviceProvider.GetService<IPluginFinder>();
            if (pluginFinder is FileSystemPluginFinder fileSystemPluginFinder) {
                var cache = (FileSystemCache<string, string>) fileSystemPluginFinder.Cache;
                cache.Clear();
            }
        }
        return hostBuilder;
    }

    private async Task RunCombinedTest(bool mustClearCache = false)
    {
        var hostBuilder = CreateHostBuilder(mustClearCache);
        var host = await hostBuilder.BuildAsync();
        var plugins = host.FoundPlugins;
        plugins!.InfoByType.Count.Should().Be(3);

        // Capabilities extraction
        var testPlugin1Caps = plugins.InfoByType[typeof(TestPlugin1)].Capabilities;
        testPlugin1Caps.Items.Count.Should().Be(0);
        var testPlugin2Caps = plugins.InfoByType[typeof(TestPlugin2)].Capabilities;
        testPlugin2Caps.Items.Count.Should().Be(2);
        testPlugin2Caps.Items.GetValueOrDefault("Client").Should().Be(true);
        testPlugin2Caps.Items.GetValueOrDefault("Server").Should().Be(false);

        // Dependencies extraction
        var testPlugin1Deps = plugins.InfoByType[typeof(TestPlugin1)].Dependencies;
        testPlugin1Deps.Should().BeEquivalentTo(new [] {(TypeRef)typeof(TestPlugin2)});
        var testPlugin1AllDeps = plugins.InfoByType[typeof(TestPlugin1)].AllDependencies;
        testPlugin1AllDeps.Should().Contain((TypeRef)typeof(TestPlugin2));

        var testPlugin2Deps = plugins.InfoByType[typeof(TestPlugin2)].Dependencies;
        testPlugin2Deps.Count.Should().Be(0);

        hostBuilder = CreateHostBuilder()
            .UsePlugins(plugins.InfoByType.Keys.Select(t => t.Resolve()));
        host = await hostBuilder.BuildAsync();

        RunPluginHostTests(host);
    }

    private static void RunPluginHostTests(IPluginHost host)
    {
        // GetPlugins -- simple form (all plugins)
        var testPlugins = host.GetPlugins<ITestPlugin>().ToArray();
        testPlugins.Length.Should().Be(2);
        testPlugins.Select(p => p.GetName())
            .Should().BeEquivalentTo("TestPlugin1", "TestPlugin2");

        var testPluginsEx = host.GetPlugins<ITestPluginEx>().ToArray();
        testPluginsEx.Length.Should().Be(1);
        testPluginsEx.Select(p => p.GetVersion())
            .Should().BeEquivalentTo("1.0");

        // GetPlugins -- filtering based on capabilities
        host.GetPlugins<ITestPlugin>(
                p => Equals(null, p.Capabilities["Server"]))
            .Select(p => p.GetName())
            .Should().BeEquivalentTo("TestPlugin1");
        host.GetPlugins<ITestPlugin>(
                p => Equals(true, p.Capabilities["Client"]))
            .Select(p => p.GetName())
            .Should().BeEquivalentTo("TestPlugin2");
        host.GetPlugins<ITestPlugin>(_ => false).Count().Should().Be(0);
        host.GetPlugins<ITestPlugin>(_ => true).Count().Should().Be(2);
    }
}
