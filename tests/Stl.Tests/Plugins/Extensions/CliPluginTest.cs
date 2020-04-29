using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Stl.Plugins;
using Stl.Plugins.Extensions.Cli;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Plugins.Extensions
{
    public class CliPluginTest : TestBase
    {
        public CliPluginTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void CombinedTestAsync()
        {
            var writer = new StringWriter();
            var log = TestLogger.New(writer);
            var loggerFactory = new LoggerFactory().AddSerilog(log);
            var testConsole = new TestOutputConsole();

            var host = new PluginHostBuilder()
                .ConfigureServices((builder, services) => services
                    .AddSingleton(loggerFactory)
                    .AddSingleton<IConsole>(testConsole))
                .UsePluginTypes(typeof(TestCliPlugin))
                .Build();

            var parser = new CommandLineBuilder()
                .UsePlugins<TestCliPlugin>(host)
                .Build();

            testConsole.Clear();
            parser.Invoke("add --a:1 --b:2").Equals(0);
            testConsole.Should().Contain("Add: 3");

            testConsole.Clear();
            parser.Invoke("mul --a 1 --b 2").Equals(0);
            testConsole.Should().Contain("Mul: 2");
        }
    }
}
