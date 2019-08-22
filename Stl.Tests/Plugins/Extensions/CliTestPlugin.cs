using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins;
using Stl.Plugins.Extensions.Cli;
using Stl.Tests.Plugins.Extensions;

[assembly: Plugin(typeof(TestCliPluginAdd))]
[assembly: Plugin(typeof(TestCliPluginMul))]

namespace Stl.Tests.Plugins.Extensions
{
    public abstract class TestCliPlugin : ICliPlugin
    {
        public IConsole Console { get; }

        public TestCliPlugin(IServiceProvider host)
        {
            Console = host.GetService<IConsole>();
        }

        public abstract void Use(CliPluginInvocation invocation);
    }

    public class TestCliPluginAdd : TestCliPlugin
    {
        public TestCliPluginAdd(IServiceProvider host) : base(host) { }

        public override void Use(CliPluginInvocation invocation)
        {
            var builder = invocation.Builder;

            var testCommand = new Command("add") {
                Handler = CommandHandler.Create((int a, int b) => {
                    Console.Out.WriteLine($"Add: {a + b}");
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

    public class TestCliPluginMul : TestCliPlugin
    {
        public TestCliPluginMul(IServiceProvider host) : base(host) { }

        public override void Use(CliPluginInvocation invocation)
        {
            var builder = invocation.Builder;

            var testCommand = new Command("mul") {
                Handler = CommandHandler.Create((int a, int b) => {
                    Console.Out.WriteLine($"Mul: {a * b}");
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
}
