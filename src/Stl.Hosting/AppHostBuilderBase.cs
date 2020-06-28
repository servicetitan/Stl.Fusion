using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Builder;
using Microsoft.AspNetCore.Hosting;
using System.CommandLine.IO;
using System.Linq;
using System.Reactive.PlatformServices;
using System.Text.RegularExpressions;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Stl.Extensibility;
using Stl.Frozen;
using Stl.Hosting.Internal;
using Stl.Hosting.Plugins;
using Stl.Plugins;
using Stl.Time;
using Stl.Time.Testing;
using CliOption = System.CommandLine.Option;
using SystemClock = Stl.Time.SystemClock;

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
        IAppHostBuilderImpl Implementation { get; }

        IHost? Build(string[]? arguments = null);
    }

    public interface IAppHostBuilderImpl
    {
        CliOption GetArgumentConfigurationOverridesOption();
    }

    public interface IAppHostBuildState
    {
        ReadOnlyMemory<string> Arguments { get; set; }
        string EnvironmentName { get; set; }
        IPluginHostBuilder PluginHostBuilder { get; set; }
        IPluginHost PluginHost { get; set; }
        ReadOnlyMemory<string> HostArguments { get; set; }
        IHost? Host { get; set; }
        Exception? CliException { get; set; }
        int? CliExitCode { get; set; }

        void BuildHost();
    }

    public abstract class AppHostBuilderBase : FrozenHasOptionsBase,
        IAppHostBuilder, IAppHostBuilderImpl, IAppHostBuildState,
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
        Exception? IAppHostBuildState.CliException {
            get => this.GetOption<Exception?>(nameof(IAppHostBuildState.CliException));
            set => SetOption(nameof(IAppHostBuildState.CliException), value);
        }
        int? IAppHostBuildState.CliExitCode {
            get => this.GetOption<int?>(nameof(IAppHostBuildState.CliExitCode));
            set => SetOption(nameof(IAppHostBuildState.CliExitCode), value);
        }

        void IAppHostBuildState.BuildHost() => BuildHost();

        public IAppHostBuildState BuildState => this;
        public IAppHostBuilderImpl Implementation => this;

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
            state.CliExitCode = ProcessCommandLine();
            Freeze();
            return state.Host;
        }

        protected virtual void ConfigurePluginHostConfiguration()
        {
            BuildState.PluginHostBuilder.ConfigureHostConfiguration(cfg => {
                var argumentCfgOverrides = ParseArgumentConfigurationOverrides()
                    .Select(p => KeyValuePair.Create(p.Key, p.Value))
                    .ToList();

                var baseCfg = new ConfigurationBuilder()
                    .SetBasePath(BaseDirectory)
                    .AddInMemoryCollection(argumentCfgOverrides)
                    .AddEnvironmentVariables(EnvironmentVarPrefix)
                    .Build();

                BuildState.EnvironmentName = IsTestHost
                    ? Environments.Development
                    : baseCfg[HostDefaults.EnvironmentKey] ?? Environments.Production;

                cfg.SetBasePath(BaseDirectory);
                cfg.AddEnvironmentVariables($"{EnvironmentVarPrefix}{BuildState.EnvironmentName}_");
                cfg.AddConfiguration(baseCfg);
                if (!IsTestHost)
                    foreach (var fileName in GetPluginHostConfigurationFileNames())
                        cfg.AddFile(fileName);
            });
        }

        protected virtual IEnumerable<(string Key, string Value)> ParseArgumentConfigurationOverrides()
        {
            var cliBuilder = new CommandLineBuilder().UseDefaults();
            cliBuilder.UseExceptionHandler((e, ctx) => {});
            var option = GetArgumentConfigurationOverridesOption();
            cliBuilder.AddOption(option);
            var cliParser = cliBuilder.Build();

            var arguments = BuildState.ParsableArguments();
            var result = cliParser.Parse(arguments);
            var values = result.RootCommandResult.ValueForOption<string[]>(option.Name);
            if (values == null || values.Length == 0)
                yield break;

            var aliases = new Dictionary<string, (string Key, string Value)>() {
                {"dev", (HostDefaults.EnvironmentKey, Environments.Development)},
                {"prod", (HostDefaults.EnvironmentKey, Environments.Production)},
            };

            foreach (var v in values) {
                var p = v.Split('=', 2);
                if (p.Length == 2)
                    yield return (p[0], p[1]);
                if (aliases.TryGetValue(p[0], out var value))
                    yield return value;
            }
        }

        CliOption IAppHostBuilderImpl.GetArgumentConfigurationOverridesOption()
            => GetArgumentConfigurationOverridesOption();
        protected virtual CliOption GetArgumentConfigurationOverridesOption()
            => new CliOption(
                new[] {"-o", "--override"},
                "Configuration property override; use '-o property1=value1 -o property2=value2' notation") {
                    Argument = new Argument<string[]>(),
                };

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
                services.TryAddSingleton<IHostEnvironment>(c => new HostingEnvironment() {
                    // Yes, we add this type solely to provide environment name for plugins
                    EnvironmentName = BuildState.EnvironmentName,
                });
                services.TryAddSingleton<ISectionRegistry>(c => new SectionRegistry());
                services.TryAddSingleton<IConsole, SystemConsole>();
                var clock = SystemClock.Instance;;
                if (IsTestHost) {
                    var testClock = new TestClock();
                    services.TryAddSingleton(testClock);
                    clock = testClock;
                }
                services.TryAddSingleton(clock);
                services.TryAddSingleton((ISystemClock) clock);
                services.TryAddSingleton((Microsoft.Extensions.Internal.ISystemClock) clock);
            });
        }

        protected virtual int ProcessCommandLine()
        {
            var plugins = BuildState.PluginHost;
            var primaryCliPlugin = plugins.GetSingletonPlugin<IPrimaryCliPlugin>();
            return primaryCliPlugin.ProcessCommandLine();
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
