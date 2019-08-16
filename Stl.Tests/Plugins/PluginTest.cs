using System.Collections.Generic;
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

            // PluginFinder
            var pluginFinderLogger = loggerFactory.CreateLogger<PluginFinder>();
            var pluginFinder = new PluginFinder(pluginFinderLogger);
            var pluginSetInfo = pluginFinder.FindPlugins();
            pluginSetInfo.Plugins.Count.Should().Be(2);
            
            // Capabilities extraction
            pluginSetInfo.Plugins[typeof(TestPlugin1)].Capabilities.Count.Should().Be(0);
            var testPlugin2Caps = pluginSetInfo.Plugins[typeof(TestPlugin2)].Capabilities;
            testPlugin2Caps.Count.Should().Be(2);
            testPlugin2Caps.GetValueOrDefault("Client").Should().Be(true);
            testPlugin2Caps.GetValueOrDefault("Server").Should().Be(false);

            var containerBuilder = new PluginContainerBuilder() {
                Configuration = new PluginContainerConfiguration(
                    pluginSetInfo, 
                    typeof(ITestPlugin), typeof(ITestPluginEx)), 
            };
            var services = containerBuilder.BuildContainer();
            
            // GetPlugins -- simple form (all plugins)
            var testPlugins = services.GetPlugins<ITestPlugin>().ToArray();
            testPlugins.Length.Should().Be(2);
            testPlugins.Select(p => p.GetName())
                .Should().BeEquivalentTo("TestPlugin1", "TestPlugin2");

            var testPluginsEx = services.GetPlugins<ITestPluginEx>().ToArray();
            testPluginsEx.Length.Should().Be(1);
            testPluginsEx.Select(p => p.GetVersion())
                .Should().BeEquivalentTo("1.0");

            // GetPlugins -- filtering based on capabilities
            services.GetPlugins<ITestPlugin>(
                    p => Equals(null, p.Capabilities.GetValueOrDefault("Server")))
                .Select(p => p.GetName())
                .Should().BeEquivalentTo("TestPlugin1");
            services.GetPlugins<ITestPlugin>(
                    p => Equals(true, p.Capabilities.GetValueOrDefault("Client")))
                .Select(p => p.GetName())
                .Should().BeEquivalentTo("TestPlugin2");
            services.GetPlugins<ITestPlugin>(_ => false).Count().Should().Be(0);
            services.GetPlugins<ITestPlugin>(_ => true).Count().Should().Be(2);

            // Caching of PluginSetInfo
            writer.Clear();
            var pluginFinder2 = new PluginFinder(pluginFinderLogger);
            var pluginSetInfo2 = pluginFinder2.FindPlugins();
            pluginSetInfo2.Plugins.Count.Should().Be(2);
            writer.GetContentAndClear().Should().ContainAll("Cached plugin set info found");
        }
    }
}
