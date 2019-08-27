using System.CommandLine;
using System.CommandLine.Invocation;
using Stl.Plugins;
using Stl.Plugins.Extensions.Cli;
using Stl.Plugins.Extensions.Hosting;
using Stl.Tests.Plugins.Extensions.AppHost;

[assembly: Plugin(typeof(Plugin1))]
[assembly: Plugin(typeof(Plugin2))]

namespace Stl.Tests.Plugins.Extensions.AppHost 
{
    public sealed class TestAppHost : AppHostBase<TestAppHost, ITestAppPlugin>
    { }

    public interface ITestAppPlugin
    { }

    public class Plugin1 : ITestAppPlugin, IHasAutoStart
    {
        public IConsole Console { get; }

        public Plugin1(IConsole console) => Console = console;

        public void AutoStart()
        {
            Console.Out.WriteLine("AutoStarted.");
        }
    }

    public class Plugin2 : ITestAppPlugin, ICliPlugin
    {
        public IAppHost AppHost { get; }
        public IConsole Console { get; }

        public Plugin2(IAppHost appHost, IConsole console)
        {
            AppHost = appHost;
            Console = console;
        }

        public void Use(CliPluginInvoker invoker)
        {
            var serveCommand = new Command("serve") {
                Handler = CommandHandler.Create(() => {
                    Console.Out.WriteLine("Serving...");
                    var webHost = AppHost.WebHost;
                }),
            };
            invoker.Builder.Command.AddCommand(serveCommand);
        }
    }
}
