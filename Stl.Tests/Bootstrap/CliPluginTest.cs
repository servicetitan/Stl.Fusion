using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Stl.Bootstrap.Cli;
using Stl.Plugins;
using Stl.Testing;
using Stl.Tests.Bootstrap;
using Xunit;
using Xunit.Abstractions;

[assembly: Plugin(typeof(CliPluginTest.TestCliPlugin))]

namespace Stl.Tests.Bootstrap
{
    public class CliPluginTest : TestBase
    {
        public class TestCliPlugin : ICliPlugin
        {
            public IConsole Console { get; }

            public TestCliPlugin(IServiceProvider host)
            {
                Console = host.GetService<IConsole>();
            }

            public void Use(CliPluginInvocation invocation)
            {
                var builder = invocation.Builder;

                var testCommand = new Command("test") {
                    Handler = CommandHandler.Create((int a, int b) => {
                        Console.Out.WriteLine((a + b).ToString());
                    }),
                };
                testCommand.AddOption(new Option("--a") {
                    Argument = new Argument<int>(),
                });
                testCommand.AddOption(new Option("--b") {
                    Argument = new Argument<int>(),
                });
                builder.AddCommand(testCommand);
            }
        }

        public CliPluginTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public void CombinedTestAsync()
        {
            var writer = new StringWriter();
            var log = TestLogger.New(writer);
            var loggerFactory = new LoggerFactory().AddSerilog(log);
            var testConsole = new TestConsole();

            var host = new PluginHostBuilder()
                .ConfigureServices(services => services
                    .AddSingleton(loggerFactory)
                    .AddSingleton<IConsole>(testConsole))
                .AddPluginTypes(typeof(TestCliPlugin))
                .Build();

            var parser = new CommandLineBuilder()
                .UsePlugins<TestCliPlugin>(host)
                .Build();

            parser.Invoke("test --a 1 --b 2").Equals(0);
            var output = testConsole.Out!.ToString()!.Trim();
            output.Should().Be("3");
        }
    }
}
