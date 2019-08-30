using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;
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
            CliBuilder.Build().Invoke(arguments);
        }

        protected virtual void ConfigureCliBuilder()
        {
            CliBuilder.Command.Handler = CommandHandler.Create<ParseResult>(BuildHostCommandHandler);
            CliBuilder.Command.AddOption(CreateOverridesOption());
            CliBuilder.Command.AddOption(CreateBindOption());
        }

        protected virtual void BuildHostCommandHandler(ParseResult parseResult)
        {
            var commandResult = parseResult.CommandResult;
            var overrides = commandResult.ValueForOption<string[]>("-o") ?? Array.Empty<string>();
            var binds = commandResult.ValueForOption<string[]>("-b") ?? Array.Empty<string>();
            if (binds.Length > 0)
                AppHostBuilder.WebHostUrls = binds;
            var hostArguments = GetHostArguments(overrides);
            AppHostBuilder.BuildState.BuildHost(hostArguments);
        }

        protected virtual string[] GetHostArguments(string[] overrides) 
            => overrides.Select(s => "--" + s).ToArray();

        protected virtual Option CreateOverridesOption() 
            => new Option(new[] {"-o", "--overrides"}) {
                Argument = new Argument<string[]>(),
            };
        protected virtual Option CreateBindOption() 
            => new Option(new[] {"-b", "--bind"}) {
                Argument = new Argument<string[]>(),
            };
    }
}
