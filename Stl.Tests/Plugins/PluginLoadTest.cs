using System;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Serilog;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Plugins
{
    public class PluginLoadTest : ConsoleInterceptingTestBase
    {
        public PluginLoadTest(ITestOutputHelper @out) : base(@out) { }
        
        [Fact]
        public void BasicTest()
        {
            var (log, writer) = Test.CreateLog();
            using var host = new TestPluginHost(log);
            Out.WriteLine("BachHost created.");
            var messages = writer.ToString();
            Assert.True(
                messages.IndexOf("TestPlugin2: Initializing.") <
                messages.IndexOf("TestPlugin1: Initializing."));
            Assert.True(
                messages.IndexOf("TestPlugin1: Injecting.") <
                messages.IndexOf("TestPlugin2: Injecting."));
        }
    }
}
