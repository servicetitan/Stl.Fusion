using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.Linq;
using System.Text.RegularExpressions;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.Extensibility;
using Stl.Hosting.Plugins;
using Stl.Plugins;
using Stl.Time;
using Stl.Time.Testing;

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
        IAppHostBuilder, IAppHostBuildState,
        ITestAppHostBuilder, ITestAppHostBuilderImpl
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

        #region ITestAppHostBuilder & ITestAppHostBuilderImpl

        ReadOnlyMemory<Type> ITestAppHostBuilder.TestPluginTypes {
            get => TestPluginTypes;
            set => TestPluginTypes = value;
        }
        protected ReadOnlyMemory<Type> TestPluginTypes {
            get => this.GetOption<Type[]>(nameof(TestPluginTypes));
            set => SetOption(nameof(TestPluginTypes), value.ToArray());
        }

        ITestAppHostBuilderImpl ITestAppHostBuilder.Implementation => this;
        protected ImmutableDictionary<Type,ImmutableList<Action<object>>> PreBuilders 
            = ImmutableDictionary<Type, ImmutableList<Action<object>>>.Empty;
        protected ImmutableDictionary<Type, ImmutableList<Action<object>>> PostBuilders 
            = ImmutableDictionary<Type, ImmutableList<Action<object>>>.Empty;

        protected static ImmutableDictionary<Type, ImmutableList<Action<object>>> AddBuilder<TBuilder>(
            ImmutableDictionary<Type, ImmutableList<Action<object>>> builders,
            Action<TBuilder> builder)
            where TBuilder : class
        {
            var key = typeof(TBuilder);
            if (!builders.TryGetValue(key, out var list))
                list = ImmutableList<Action<object>>.Empty;
            return builders.SetItem(key, list.Add(b => builder.Invoke((TBuilder) b)));
        }

        protected static void InvokeBuilders<TBuilder>(
            ImmutableDictionary<Type, ImmutableList<Action<object>>> builders,
            TBuilder builder)
            where TBuilder : class
        {
            var list = builders.GetValueOrDefault(typeof(TBuilder)) ?? ImmutableList<Action<object>>.Empty;
            foreach (var action in list) {
                action.Invoke(builder);
            }
        }

        ITestAppHostBuilder ITestAppHostBuilder.InjectPreBuilder<TBuilder>(Action<TBuilder> preBuilder) 
        {
            PreBuilders = AddBuilder(PreBuilders, preBuilder);
            return this;
        }
        ITestAppHostBuilder ITestAppHostBuilder.InjectPostBuilder<TBuilder>(Action<TBuilder> postBuilder) 
        {
            PostBuilders = AddBuilder(PostBuilders, postBuilder);
            return this;
        }

        void ITestAppHostBuilderImpl.InvokePreBuilders<TBuilder>(TBuilder builder)
            => InvokeBuilders(PreBuilders, builder);
        void ITestAppHostBuilderImpl.InvokePostBuilders<TBuilder>(TBuilder builder)
            => InvokeBuilders(PreBuilders, builder);

        #endregion

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
            var testAppHostBuilder = IsTestHost ? (ITestAppHostBuilder) this : null;
            
            state.PluginHostBuilder = CreatePluginHostBuilder();
            ConfigurePluginHostConfiguration();
            ConfigurePluginHostServiceProviderFactory();
            ConfigurePluginHostPluginTypes();
            testAppHostBuilder?.Implementation?.InvokePreBuilders(state.PluginHostBuilder);
            ConfigurePluginHostLogging();
            ConfigurePluginHostServices();
            testAppHostBuilder?.Implementation?.InvokePostBuilders(state.PluginHostBuilder);
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
            
                BuildState.EnvironmentName = baseCfg[HostDefaults.EnvironmentKey] 
                    ?? Environments.Production;
                
                if (IsTestHost)
                    return;

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
                services.TryAddSingleton<IConsole, SystemConsole>();
                if (IsTestHost) {
                    var testClock = new TestClock();
                    services.AddSingleton(testClock);
                    services.AddSingleton((IClock) testClock);
                }
                else {
                    services.AddSingleton(RealTimeClock.Instance);
                }
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
