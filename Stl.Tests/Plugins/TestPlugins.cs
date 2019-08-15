using System;
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
    
    public class TestPlugin2 : TestPlugin1, ITestPluginEx
    {
        public virtual string GetVersion() => "1.0";

        public TestPlugin2(IServiceProvider services)
        {
            services.Should().NotBeNull();
        }
    }
}
