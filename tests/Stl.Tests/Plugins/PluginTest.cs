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
using Stl.Plugins.Services;
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
            var host = new PluginHostBuilder()
                .UsePluginTypes(typeof(ITestPlugin))
                .Build();

            RunPluginHostTests(host);
        }

        [Fact]
        public void PluginFilterTest()
        {
            var host = new PluginHostBuilder()
                .UsePluginTypes(typeof(ITestPlugin))
                .Build();
            host.GetPlugins<ITestPlugin>().Count().Should().Be(2);

            host = new PluginHostBuilder()
                .UsePluginTypes(typeof(ITestPlugin))
                .AddPluginFilter(p => p.Type != typeof(TestPlugin2))
                .Build();
            host.GetPlugins<ITestPlugin>().Count().Should().Be(1);
        }

        [Fact]
        public void SingletonPluginTest()
        {
            var host = new PluginHostBuilder()
                .UsePluginTypes(typeof(ITestPlugin))
                .Build();

            host.GetPlugins<ITestPlugin>().Count().Should().Be(2);
            host.GetPlugins<ITestSingletonPlugin>().Count().Should().Be(1);
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
                var fsc = (FileSystemCache<string, string>)pluginFinder.Cache;
                fsc.Clear();
            }

            var plugins = pluginFinder.FindPlugins();
            plugins.InfoByType.Count.Should().BeGreaterOrEqualTo(2);

            // Capabilities extraction
            var testPlugin1Caps = plugins.InfoByType[typeof(TestPlugin1)].Capabilities;
            testPlugin1Caps.Count.Should().Be(0);
            var testPlugin2Caps = plugins.InfoByType[typeof(TestPlugin2)].Capabilities;
            testPlugin2Caps.Count.Should().Be(2);
            testPlugin2Caps.GetValueOrDefault("Client").Should().Be(true);
            testPlugin2Caps.GetValueOrDefault("Server").Should().Be(false);

            // Dependencies extraction
            var testPlugin1Deps = plugins.InfoByType[typeof(TestPlugin1)].Dependencies;
            testPlugin1Deps.Should().BeEquivalentTo((TypeRef)typeof(TestPlugin2));
            var testPlugin1AllDeps = plugins.InfoByType[typeof(TestPlugin1)].AllDependencies;
            testPlugin1AllDeps.Should().Contain((TypeRef)typeof(TestPlugin2));

            var testPlugin2Deps = plugins.InfoByType[typeof(TestPlugin2)].Dependencies;
            testPlugin2Deps.Count.Should().Be(0);

            var host = new PluginHostBuilder()
                .UsePlugins(plugins)
                .UsePluginTypes(typeof(ITestPlugin))
                .Build();

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
