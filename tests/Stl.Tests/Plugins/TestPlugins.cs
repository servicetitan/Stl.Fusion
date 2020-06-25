using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Stl.Plugins;
using Stl.Tests.Plugins;

[assembly: Plugin(typeof(TestPlugin1))]
[assembly: Plugin(typeof(TestPlugin2))]

namespace Stl.Tests.Plugins
{

    public interface ITestPlugin
    {
        string GetName();
    }

    public interface ITestSingletonPlugin : ITestPlugin, ISingletonPlugin
    {
    }

    public interface ITestPluginEx : ITestPlugin
    {
        string GetVersion();
    }

    public abstract class TestPlugin : ITestPlugin
    {
        public virtual string GetName() => GetType().Name;
    }

    public class TestPlugin1 : TestPlugin, IHasDependencies, ITestSingletonPlugin
    {
        public IEnumerable<Type> Dependencies { get; } = new [] { typeof(TestPlugin2) };
        public TestPlugin1(IPluginInfoQuery query) { }
        public TestPlugin1() { }
    }

    public class TestPlugin2 : TestPlugin, ITestPluginEx, IHasCapabilities, ITestSingletonPlugin
    {
        public virtual string GetVersion() => "1.0";
        public ImmutableDictionary<string, object> Capabilities { get; } =
            new Dictionary<string, object>() {
                {"Client", true},
                {"Server", false}
            }.ToImmutableDictionary();

        public TestPlugin2(IPluginInfoQuery query) { }
        public TestPlugin2(IServiceProvider services)
        {
            services.Should().NotBeNull();
        }
    }
}
