using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Serilog;
using Stl.Plugins;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Plugins
{
    public class PluginTest : ConsoleInterceptingTestBase
    {
        public PluginTest(ITestOutputHelper @out) : base(@out) { }
        
        [Fact]
        public void CombinedTest()
        {
            var writer = new StringWriter();
            var log = TestLogger.New(writer);
            var loggerFactory = new LoggerFactory().AddSerilog(log);

            var pluginFinderLogger = loggerFactory.CreateLogger<PluginFinder>();
            var pluginFinder = new PluginFinder(pluginFinderLogger) {
                PluginTypes = { typeof(ITestPlugin), typeof(ITestPluginEx) }
            };
            var pluginSetInfo = pluginFinder.FindPlugins();
            pluginSetInfo.Exports.Count.Should().Be(2);

            var containerBuilder = new PluginContainerBuilder(pluginSetInfo);
            var services = containerBuilder.BuildContainer();
            
            var testPlugins = services.GetPlugins<ITestPlugin>().ToArray();
            testPlugins.Length.Should().Be(2);
            testPlugins.Select(p => p.GetName())
                .Should().BeEquivalentTo("TestPlugin1", "TestPlugin2");

            var testPluginsEx = services.GetPlugins<ITestPluginEx>().ToArray();
            testPluginsEx.Length.Should().Be(1);
            testPluginsEx.Select(p => p.GetVersion())
                .Should().BeEquivalentTo("1.0");

            // Checking whether caching works
            writer.Clear();
            var pluginFinder2 = new PluginFinder(pluginFinderLogger) {
                PluginTypes = { typeof(ITestPlugin), typeof(ITestPluginEx) }
            };
            var pluginSetInfo2 = pluginFinder2.FindPlugins();
            writer.GetContentAndClear().Should().ContainAll("Cached plugin set info found");
        }
    }
}
