using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text.RegularExpressions;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.Extensibility;
using Stl.Hosting.Plugins;
using Stl.Plugins;

namespace Stl.Hosting
{
    public interface IAppHostBuilder
    {
        string AppName { get; }
        string BaseDirectory { get; set; }
        string EnvironmentVarPrefix { get; set; }
        string PluginHostLoggingSectionName { get; set; }
        string LoggingSectionName { get; set; }
        ReadOnlyMemory<string> WebHostUrls { get; set; }

        bool IsTestHost { get; }
        IAppHostBuildState BuildState { get; }

        IHost? Build(string[]? arguments = null);
    }

    public interface ITestAppHostBuilder
    {
        ReadOnlyMemory<Type> TestPluginTypes { get; set; }
    }

    public interface IAppHostBuildState 
    { 
        ReadOnlyMemory<string> Arguments { get; set; }
        string EnvironmentName { get; set; }
        IPluginHostBuilder PluginHostBuilder { get; set; }
        IPluginHost PluginHost { get; set; }
        ReadOnlyMemory<string> HostArguments { get; set; }
        IHost? Host { get; set; }

        void BuildHost();
    }

    public abstract class AppHostBuilderBase : HasOptionsBase, 
        IAppHostBuilder, ITestAppHostBuilder, IAppHostBuildState
    {
        public abstract string AppName { get; }
        public abstract Type[] CorePluginTypes { get; }
        public abstract Type[] NonTestPluginTypes { get; }
        public bool IsTestHost => TestPluginTypes.Length != 0;

        public string BaseDirectory {
            get => this.GetOption<string>(nameof(BaseDirectory)) ?? "";
            set => SetOption(nameof(BaseDirectory), value);
        }
        public string EnvironmentVarPrefix {
            get => this.GetOption<string>(nameof(EnvironmentVarPrefix)) ?? "";
            set => SetOption(nameof(EnvironmentVarPrefix), value);
        }
        public string PluginHostLoggingSectionName {
            get => this.GetOption<string>(nameof(PluginHostLoggingSectionName)) ?? "";
            set => SetOption(nameof(PluginHostLoggingSectionName), value);
        }
        public string LoggingSectionName {
            get => this.GetOption<string>(nameof(LoggingSectionName)) ?? "";
            set => SetOption(nameof(LoggingSectionName), value);
        }
        public ReadOnlyMemory<string> WebHostUrls {
            get => this.GetOption<string[]>(nameof(WebHostUrls));
            set => SetOption(nameof(WebHostUrls), value.ToArray());
        }

        public IAppHostBuildState BuildState => this;
        void IAppHostBuildState.BuildHost() => BuildHost();

        ReadOnlyMemory<string> IAppHostBuildState.Arguments {
            get => this.GetOption<string[]>(nameof(IAppHostBuildState.Arguments));
            set => SetOption(nameof(IAppHostBuildState.Arguments), value.ToArray());
        }
        string IAppHostBuildState.EnvironmentName {
            get => this.GetOption<string>(nameof(IAppHostBuildState.EnvironmentName)) ?? Environments.Production;
            set => SetOption(nameof(IAppHostBuildState.EnvironmentName), value);
        }
        IPluginHostBuilder IAppHostBuildState.PluginHostBuilder {
            get => this.GetOption<IPluginHostBuilder?>(nameof(IAppHostBuildState.PluginHostBuilder))!;
            set => SetOption(nameof(IAppHostBuildState.PluginHostBuilder), value);
        }
        IPluginHost IAppHostBuildState.PluginHost {
            get => this.GetOption<IPluginHost?>(nameof(IAppHostBuildState.PluginHost))!;
            set => SetOption(nameof(IAppHostBuildState.PluginHost), value);
        }
        ReadOnlyMemory<string> IAppHostBuildState.HostArguments {
            get => this.GetOption<string[]>(nameof(IAppHostBuildState.HostArguments));
            set => SetOption(nameof(IAppHostBuildState.HostArguments), value.ToArray());
        }
        IHost? IAppHostBuildState.Host {
            get => this.GetOption<IHost?>(nameof(IAppHostBuildState.Host));
            set => SetOption(nameof(IAppHostBuildState.Host), value);
        }

        // ITestAppHostBuilder properties (protected + explicit)

        ReadOnlyMemory<Type> ITestAppHostBuilder.TestPluginTypes {
            get => TestPluginTypes;
            set => TestPluginTypes = value;
        }
        protected ReadOnlyMemory<Type> TestPluginTypes {
            get => this.GetOption<Type[]>(nameof(TestPluginTypes));
            set => SetOption(nameof(TestPluginTypes), value.ToArray());
        }

        protected AppHostBuilderBase()
        {
            BaseDirectory = AppContext.BaseDirectory;
            EnvironmentVarPrefix = GetDefaultEnvironmentVarPrefix();
            PluginHostLoggingSectionName = "Logging";
            LoggingSectionName = "Logging";
            WebHostUrls = new [] {"http://localhost:32100"};
            
            var state = BuildState;
            state.Arguments = Array.Empty<string>();
            state.EnvironmentName = Environments.Production;
        }

        protected string GetDefaultEnvironmentVarPrefix()
        {
            var nonAlphaRegex = new Regex("[^A-Z]+", 
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return $"{nonAlphaRegex.Replace(AppName.ToUpperInvariant(), "")}_";
        }

        public virtual IHost? Build(string[]? arguments = null)
        {
            var state = BuildState;
            if (arguments != null)
                state.Arguments = arguments;
            state.PluginHostBuilder = CreatePluginHostBuilder();
            ConfigurePluginHostConfiguration();
            ConfigurePluginHostServiceProviderFactory();
            ConfigurePluginHostPluginTypes();
            ConfigurePluginHostLogging();
            ConfigurePluginHostServices();
            state.PluginHost = state.PluginHostBuilder.Build();
            ProcessCommandLine();
            LockOptions();
            return state.Host;
        }

        protected virtual void ConfigurePluginHostConfiguration()
        {
            BuildState.PluginHostBuilder.ConfigureHostConfiguration(cfg => {
                var baseCfg = new ConfigurationBuilder()
                    .SetBasePath(BaseDirectory)
                    .AddEnvironmentVariables(EnvironmentVarPrefix)
                    .Build();
            
                BuildState.EnvironmentName = baseCfg[HostDefaults.EnvironmentKey] ?? Environments.Production;

                cfg.AddConfiguration(baseCfg);
                foreach (var fileName in GetPluginHostConfigurationFileNames())
                    cfg.AddFile(fileName);
            });
        }

        protected virtual IPluginHostBuilder CreatePluginHostBuilder() 
            => new PluginHostBuilder();

        protected virtual void ConfigurePluginHostServiceProviderFactory()
            => BuildState.PluginHostBuilder.UseServiceProviderFactory((builder, services) => {
                var f = new AutofacServiceProviderFactory();
                var containerBuilder = f.CreateBuilder(services);
                return f.CreateServiceProvider(containerBuilder);
            });

        protected virtual void ConfigurePluginHostPluginTypes()
        {
            var pluginTypes = CorePluginTypes
                .Concat(IsTestHost ? TestPluginTypes.ToArray() : NonTestPluginTypes)
                .ToArray();
            BuildState.PluginHostBuilder.UsePluginTypes(pluginTypes);
        }

        protected virtual void ConfigurePluginHostLogging()
        {
            BuildState.PluginHostBuilder.ConfigureServices((builder, services) => {
                services.AddLogging(logging => {
                    if (IsTestHost) {
                        logging.AddDebug();
                        return;
                    }
                    var cfg = builder.Configuration;
                    var loggingCfg = cfg.GetSection(PluginHostLoggingSectionName);
                    logging.AddConfiguration(loggingCfg);
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                });
            });
        }

        protected virtual void ConfigurePluginHostServices()
        {
            BuildState.PluginHostBuilder.ConfigureServices((builder, services) => {
                var cfg = builder.Configuration;
                services.TryAddSingleton(cfg);
                services.TryAddSingleton<IAppHostBuilder>(this);
                if (IsTestHost)
                    services.TryAddSingleton<IConsole, TestConsole>();
                else
                    services.TryAddSingleton<IConsole, SystemConsole>();
            });
        }

        protected virtual void ProcessCommandLine()
        {
            var plugins = BuildState.PluginHost;
            var cliProcessor = plugins.GetSingletonPlugin<IPrimaryCliPlugin>();
            cliProcessor.Run(BuildState.Arguments.ToArray());
        }

        protected virtual void BuildHost()
        {
            var plugins = BuildState.PluginHost;
            var hostPlugin = plugins.GetSingletonPlugin<IPrimaryHostPlugin>();
            BuildState.Host = hostPlugin.BuildHost();
        }

        protected virtual IEnumerable<string> GetPluginHostConfigurationFileNames()
        {
            yield return "pluginSettings.json";
            yield return $"pluginSettings.{BuildState.EnvironmentName}.json"; 
        }
    }
}
