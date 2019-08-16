using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Stl.Plugins;
using Stl.Plugins.Extensions;
using Stl.Tests.Plugins;

[assembly: Plugin(typeof(TestPlugin1))]
[assembly: Plugin(typeof(TestPlugin2))]

namespace Stl.Tests.Plugins
{

    public interface ITestPlugin
    {
        string GetName();
    }

    public interface ITestPluginEx : ITestPlugin
    {
        string GetVersion();
    }

    public abstract class TestPlugin : ITestPlugin
    {
        public virtual string GetName() => GetType().Name;
    }

    public class TestPlugin1 : TestPlugin
    {
    }
    
    public class TestPlugin2 : TestPlugin1, ITestPluginEx, IHasCapabilities
    {
        public virtual string GetVersion() => "1.0";
        public ImmutableDictionary<string, object> Capabilities { get; } =
            new Dictionary<string, object>() {
                {"Client", true},
                {"Server", false}
            }.ToImmutableDictionary();

        public TestPlugin2(IServiceProvider services)
        {
            services.Should().NotBeNull();
        }
    }
}
