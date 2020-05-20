using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Stl.Hosting.Internal;
using Stl.Hosting.Plugins;
using Stl.Plugins;
using Stl.Plugins.Extensions.Cli;
using CliOption = System.CommandLine.Option;

[assembly: Plugin(typeof(PrimaryCliPlugin))]

namespace Stl.Hosting.Plugins
{
    public interface IPrimaryCliPlugin : ISingletonPlugin
    {
        int ProcessCommandLine();
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

        public virtual int ProcessCommandLine()
        {
            var buildState = AppHostBuilder.BuildState;
            var arguments = buildState.ParsableArguments();
            var console = Plugins.GetRequiredService<IConsole>();

            // Creating & configuring CLI builder
            CliBuilder = new CommandLineBuilder().UseDefaults();
            ConfigureCliBuilder();
            // Letting other CLI plugins to kick in
            CliBuilder.UsePlugins<ICliPlugin>(Plugins);

            // Building parser & parsing the arguments
            var cliParser = CliBuilder.Build();
            var cliParseResult = cliParser.Parse(arguments);

            // Invoking commands
            var exitCode = cliParseResult.Invoke(console);
            var cliException = buildState.CliException;
            if (cliException != null)
                ExceptionDispatchInfo.Throw(cliException);
            return exitCode;
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

        protected virtual void AddOverridesOption()
        {
            var option = AppHostBuilder.Implementation.GetArgumentConfigurationOverridesOption();
            if (CliBuilder.Command.Children.GetByAlias(option.Name) != null)
                return;
            CliBuilder.AddOption(option);
        }

        protected virtual void AddBindOption()
        {
            var option = new CliOption(
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
            var middlewareListField = CliBuilder.GetType().GetField(
                "_middlewareList", BindingFlags.Instance | BindingFlags.NonPublic);
            var fieldValue = middlewareListField!.GetValue(CliBuilder);
            var middlewareList = (List<(InvocationMiddleware, int)>) fieldValue!;
            middlewareList.Add((middleware, order));
        }
    }
}
