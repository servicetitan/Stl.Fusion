using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Stl.Plugins;
using Stl.Plugins.Metadata;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Plugins
{
    public class PluginTest : ConsoleInterceptingTestBase
    {
        public PluginTest(ITestOutputHelper @out) : base(@out) { }
        
        [Fact]
        public void BasicTest()
        {
            var writer = new StringWriter();
            var log = TestLogger.New(writer);

            var pluginEnumerator = new PluginEnumerator() {
                PluginTypes = { typeof(ITestPlugin), typeof(ITestPluginEx) }
            };
            var pluginSetInfo = pluginEnumerator.GetPluginSetInfo();
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
        }
    }
}
