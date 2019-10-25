using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Stl.Hosting.Plugins;
using Stl.Plugins;
using Stl.Plugins.Extensions.Cli;

[assembly: Plugin(typeof(PrimaryCliPlugin))]

namespace Stl.Hosting.Plugins
{
    public interface IPrimaryCliPlugin : ISingletonPlugin
    {
        void Run(string[] arguments);
    }

    public class PrimaryCliPlugin : IPrimaryCliPlugin, IAppHostBuilderPlugin
    {
        protected IPluginHost Plugins { get; set; } = null!;
        protected IAppHostBuilder AppHostBuilder { get; set; } = null!;
        protected CommandLineBuilder CliBuilder { get; set; } = null!;

        public PrimaryCliPlugin() { }
        public PrimaryCliPlugin(IPluginHost plugins, IAppHostBuilder appHostBuilder)
        {
            Plugins = plugins;
            AppHostBuilder = appHostBuilder;
        }

        public virtual void Run(string[] arguments)
        {
            CliBuilder = new CommandLineBuilder().UseDefaults();
            ConfigureCliBuilder();
            // Now letting plugins to kick in
            CliBuilder.UsePlugins<ICliPlugin>(Plugins);
            var console = Plugins.GetService<IConsole>();
            var cliParser = CliBuilder.Build();
            var result = cliParser.Invoke(arguments, console);
            var cliException = AppHostBuilder.BuildState.CliException;
            if (cliException != null)
                ExceptionDispatchInfo.Throw(cliException);
        }

        protected virtual void OnCliException(Exception exception, InvocationContext context) 
            => AppHostBuilder.BuildState.CliException = exception;

        protected virtual void ConfigureCliBuilder()
        {
            AddOverridesOption();
            AddBindOption();
            CliBuilder.UseExceptionHandler(OnCliException);
            CliBuilder.Command.Handler = CommandHandler.Create(
                () => AppHostBuilder.BuildState.BuildHost());
        }

        protected virtual string[] GetHostArguments(string[] overrides) 
            => overrides.Select(s => "--" + s).ToArray();

        protected virtual void AddOverridesOption()
        {
            var option = new System.CommandLine.Option(
                new[] {"-o", "--override"}, 
                "Configuration property override; use '-o property1=value1 -o property2=value2' notation") {
                Argument = new Argument<string[]>(),
            };
            if (CliBuilder.Command.Children.GetByAlias(option.Name) != null)
                return;
            CliBuilder.AddOption(option);
            AddMiddleware(async (ctx, next) => {
                var value = ctx.ParseResult.RootCommandResult.ValueForOption<string[]>(option.Name);
                if (value != null && value.Length > 0)
                    AppHostBuilder.BuildState.HostArguments = GetHostArguments(value);
                await next(ctx);
            }, -10100);
        }

        protected virtual void AddBindOption()
        {
            var option = new System.CommandLine.Option(
                new[] {"-b", "--bind"}, 
                "Web server bind address; you can use multiple bind options") {
                Argument = new Argument<string[]>(),
            };
            if (CliBuilder.Command.Children.GetByAlias(option.Name) != null)
                return;
            CliBuilder.AddOption(option);
            AddMiddleware(async (ctx, next) => {
                var value = ctx.ParseResult.RootCommandResult.ValueForOption<string[]>(option.Name);
                if (value != null && value.Length > 0)
                    AppHostBuilder.WebHostUrls = value;
                await next(ctx);
            }, -10090);
        }

        protected void AddMiddleware(InvocationMiddleware middleware, int order)
        {
            var methodInfo = CliBuilder.GetType().GetMethod(
                nameof(AddMiddleware), 
                BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo!.Invoke(CliBuilder, new object[] { middleware, order });
        }
    }
}
