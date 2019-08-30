using System;
using System.Collections.Generic;
using System.CommandLine;
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
        string BaseDirectory { get; set; }
        string EnvironmentVarPrefix { get; set; }
        string PluginHostLoggingSectionName { get; set; }
        string LoggingSectionName { get; set; }
        ReadOnlyMemory<string> WebHostUrls { get; set; }
        IAppHostBuildState BuildState { get; }

        IHost? Build(string[]? arguments = null);
    }

    public interface IAppHostBuildState 
    { 
        ReadOnlyMemory<string> Arguments { get; set; }
        string EnvironmentName { get; set; }
        IConfiguration PluginHostConfiguration { get; set; }
        IPluginHostBuilder PluginHostBuilder { get; set; }
        IPluginHost PluginHost { get; set; }
        ReadOnlyMemory<string> HostArguments { get; set; }
        IHost? Host { get; set; }

        void BuildHost();
    }

    public abstract class AppHostBuilderBase : HasOptionsBase, IAppHostBuilder, IAppHostBuildState
    {
        public abstract string AppName { get; }
        public abstract Type[] PluginTypes { get; }

        public string BaseDirectory {
            get => GetOption<string>(nameof(BaseDirectory)) ?? "";
            set => SetOption(nameof(BaseDirectory), value);
        }
        public string EnvironmentVarPrefix {
            get => GetOption<string>(nameof(EnvironmentVarPrefix)) ?? "";
            set => SetOption(nameof(EnvironmentVarPrefix), value);
        }
        public string PluginHostLoggingSectionName {
            get => GetOption<string>(nameof(PluginHostLoggingSectionName)) ?? "";
            set => SetOption(nameof(PluginHostLoggingSectionName), value);
        }
        public string LoggingSectionName {
            get => GetOption<string>(nameof(LoggingSectionName)) ?? "";
            set => SetOption(nameof(LoggingSectionName), value);
        }
        public ReadOnlyMemory<string> WebHostUrls {
            get => GetOption<ReadOnlyMemory<string>>(nameof(WebHostUrls));
            set => SetOption(nameof(WebHostUrls), value);
        }

        public IAppHostBuildState BuildState => this;
        void IAppHostBuildState.BuildHost() => BuildHost();

        ReadOnlyMemory<string> IAppHostBuildState.Arguments {
            get => GetOption<ReadOnlyMemory<string>>(nameof(IAppHostBuildState.Arguments));
            set => SetOption(nameof(IAppHostBuildState.Arguments), value);
        }
        string IAppHostBuildState.EnvironmentName {
            get => GetOption<string>(nameof(IAppHostBuildState.EnvironmentName)) ?? Environments.Production;
            set => SetOption(nameof(IAppHostBuildState.EnvironmentName), value);
        }
        IConfiguration IAppHostBuildState.PluginHostConfiguration {
            get => GetOption<IConfiguration?>(nameof(IAppHostBuildState.PluginHostConfiguration))!;
            set => SetOption(nameof(IAppHostBuildState.PluginHostConfiguration), value);
        }
        IPluginHostBuilder IAppHostBuildState.PluginHostBuilder {
            get => GetOption<IPluginHostBuilder?>(nameof(IAppHostBuildState.PluginHostBuilder))!;
            set => SetOption(nameof(IAppHostBuildState.PluginHostBuilder), value);
        }
        IPluginHost IAppHostBuildState.PluginHost {
            get => GetOption<IPluginHost?>(nameof(IAppHostBuildState.PluginHost))!;
            set => SetOption(nameof(IAppHostBuildState.PluginHost), value);
        }
        ReadOnlyMemory<string> IAppHostBuildState.HostArguments {
            get => GetOption<ReadOnlyMemory<string>>(nameof(IAppHostBuildState.HostArguments));
            set => SetOption(nameof(IAppHostBuildState.HostArguments), value);
        }
        IHost? IAppHostBuildState.Host {
            get => GetOption<IHost?>(nameof(IAppHostBuildState.Host));
            set => SetOption(nameof(IAppHostBuildState.Host), value);
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
            state.PluginHostConfiguration = BuildPluginHostConfiguration();
            state.PluginHostBuilder = CreatePluginHostBuilder();
            ConfigurePluginHostServiceProviderFactory();
            ConfigurePluginHostUsePlugins();
            ConfigurePluginHostServices();
            state.PluginHost = state.PluginHostBuilder.Build();
            ProcessCommandLine();
            LockOptions();
            return state.Host;
        }

        protected virtual IConfiguration BuildPluginHostConfiguration()
        {
            var baseCfg = new ConfigurationBuilder()
                .SetBasePath(BaseDirectory)
                .AddEnvironmentVariables(EnvironmentVarPrefix)
                .Build();
            
            BuildState.EnvironmentName = baseCfg[HostDefaults.EnvironmentKey] ?? Environments.Production;

            var cfg = new ConfigurationBuilder()
                .AddConfiguration(baseCfg);
            foreach (var fileName in GetPluginHostConfigurationFileNames())
                cfg.AddFile(fileName);

            return cfg.Build();
        }

        protected virtual IPluginHostBuilder CreatePluginHostBuilder() 
            => new PluginHostBuilder();

        protected virtual void ConfigurePluginHostServiceProviderFactory()
            => BuildState.PluginHostBuilder.UseServiceProviderFactory(services => {
                var f = new AutofacServiceProviderFactory();
                var builder = f.CreateBuilder(services);
                return f.CreateServiceProvider(builder);
            });

        protected virtual void ConfigurePluginHostUsePlugins()
            => BuildState.PluginHostBuilder.UsePluginTypes(PluginTypes);

        protected virtual void ConfigurePluginHostServices()
        {
            var cfg = BuildState.PluginHostConfiguration;
            BuildState.PluginHostBuilder.ConfigureServices(services => {
                services.TryAddSingleton(cfg);
                services.TryAddSingleton<IAppHostBuilder>(this);
                services.AddLogging(logging => {
                    var loggingConfiguration = cfg.GetSection(PluginHostLoggingSectionName);
                    logging.AddConfiguration(loggingConfiguration);
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                });
                services.TryAddSingleton<IConsole, SystemConsole>();
                return services;
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
