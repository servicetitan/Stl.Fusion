using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Stl.Caching;
using Stl.Plugins;
using Stl.Reflection;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Plugins
{
    public class PluginTest : ConsoleInterceptingTestBase
    {
        public PluginTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void PluginHostBuilderTest()
        {
            var writer = new StringWriter();
            var log = TestLogger.New(writer);
            var loggerFactory = new LoggerFactory().AddSerilog(log);

            var host = new PluginHostBuilder()
                .ConfigureServices(s => s.AddSingleton(loggerFactory))
                .AddPluginTypes(typeof(ITestPlugin), typeof(ITestPluginEx))
                .Build();

            RunPluginHostTests(host);
        }
        
        [Fact]
        public void CombinedTest()
        {
            var writer = new StringWriter();
            var log = TestLogger.New(writer);
            var loggerFactory = new LoggerFactory().AddSerilog(log);

            RunCombinedTest(loggerFactory, true);
            writer.GetContentAndClear().Should().ContainAll("populating");
            RunCombinedTest(loggerFactory);
            writer.GetContentAndClear().Should().ContainAll("Cached plugin set info found");
        }

        private void RunCombinedTest(ILoggerFactory loggerFactory, bool mustClearCache = false)
        { 
            var logger = loggerFactory.CreateLogger<PluginFinder>();
            var pluginFinder = new PluginFinder(logger);
            if (mustClearCache) {
                var fsc = (FileSystemCache<string, string>) pluginFinder.Cache;
                fsc.Clear();
            }

            var pluginSetInfo = pluginFinder.FindPlugins();
            pluginSetInfo.Plugins.Count.Should().BeGreaterOrEqualTo(2);

            // Capabilities extraction
            var testPlugin1Caps = pluginSetInfo.Plugins[typeof(TestPlugin1)].Capabilities;
            testPlugin1Caps.Count.Should().Be(0);
            var testPlugin2Caps = pluginSetInfo.Plugins[typeof(TestPlugin2)].Capabilities;
            testPlugin2Caps.Count.Should().Be(2);
            testPlugin2Caps.GetValueOrDefault("Client").Should().Be(true);
            testPlugin2Caps.GetValueOrDefault("Server").Should().Be(false);

            // Dependencies extraction
            var testPlugin1Deps = pluginSetInfo.Plugins[typeof(TestPlugin1)].AllDependencies;
            testPlugin1Deps.Should().BeEquivalentTo((TypeRef) typeof(TestPlugin2));
            var testPlugin2Deps = pluginSetInfo.Plugins[typeof(TestPlugin2)].AllDependencies;
            testPlugin2Deps.Count.Should().Be(0);

            var host = new PluginHostBuilder()
                .UsePluginConfiguration(new PluginConfiguration(pluginSetInfo))
                .Build();

            RunPluginHostTests(host);
        }

        private static void RunPluginHostTests(IServiceProvider host)
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
                    p => Equals(null, p.Capabilities.GetValueOrDefault("Server")))
                .Select(p => p.GetName())
                .Should().BeEquivalentTo("TestPlugin1");
            host.GetPlugins<ITestPlugin>(
                    p => Equals(true, p.Capabilities.GetValueOrDefault("Client")))
                .Select(p => p.GetName())
                .Should().BeEquivalentTo("TestPlugin2");
            host.GetPlugins<ITestPlugin>(_ => false).Count().Should().Be(0);
            host.GetPlugins<ITestPlugin>(_ => true).Count().Should().Be(2);
        }
    }
}
