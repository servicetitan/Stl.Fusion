using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.Extensibility;
using Stl.Plugins.Extensions.Cli;
using Stl.Plugins.Extensions.Web;

namespace Stl.Plugins.Extensions.Hosting
{
    public interface IAppHost
    {
        CancellationToken CancellationToken { get; }
        IServiceProvider Plugins { get; }
        IServiceProvider? Services { get; }
        IWebHost? WebHost { get; }
        
        Task<IWebHost> WebHostReadyTask { get; }
        Task<int> ResultReadyTask { get; }

        IAppHostImpl Implementation { get; }
    }

    public interface IAppHost<TSelf> : IAppHost { }

    public interface IAppHostImpl 
    {
        void BuildFrom(IAppHostBuilder builder);
        void Start(string arguments, CancellationToken cancellationToken = default);
        void BuildWebHost();
    }

    public abstract class AppHostBase<TSelf> : IAppHost<TSelf>, IAppHostImpl
    {
        private readonly TaskCompletionSource<int> _resultReadyTcs = 
            new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<IWebHost> _webHostReadyTcs = 
            new TaskCompletionSource<IWebHost>();

        public CancellationToken CancellationToken { get; protected set; } = default;
        public IServiceProvider Plugins { get; protected set; } = ServiceProviderEx.Unavailable;
        public IServiceProvider? Services { get; protected set; }
        public IWebHost? WebHost { get; protected set; }
        public Task<IWebHost> WebHostReadyTask => _webHostReadyTcs.Task;
        public Task<int> ResultReadyTask => _resultReadyTcs.Task;

        public IAppHostImpl Implementation => this;
        void IAppHostImpl.BuildFrom(IAppHostBuilder builder) => BuildFrom(builder);
        void IAppHostImpl.BuildWebHost() => BuildWebHost();

        public virtual void Start(string arguments, CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;

            var console = Plugins.GetService<IConsole>();
            var cliParser = BuildCliParser();

            var resultReadyTask = cliParser.InvokeAsync(arguments, console);
            // Making sure we propagate resultReadyTask result to _resultReadyTcs 
            resultReadyTask.ContinueWith((t, _) => {
                _resultReadyTcs.TrySetFromTask(resultReadyTask);
            }, null, CancellationToken.None); // No cancellation = intentional

            var webHostReadyTask = WebHostReadyTask;
            // Making sure we cancel _webHostReadyTcs when CLI part completes 
            Task.WhenAny(resultReadyTask, webHostReadyTask).ContinueWith((t, _) => {
                if (resultReadyTask.IsCompleted) {
                    if (!webHostReadyTask.IsCompleted)
                        _webHostReadyTcs.TrySetCanceled();
                };
            }, null, CancellationToken.None); // No cancellation = intentional
        }

        protected virtual void BuildFrom(IAppHostBuilder builder) 
            => Plugins = BuildPlugins(builder);

        protected virtual IServiceProvider BuildPlugins(IAppHostBuilder builder)
        {
            var pluginHostBuilder = builder.PluginHostBuilder.AddPluginTypes(GetPluginTypes());
            pluginHostBuilder = ConfigurePlugins(pluginHostBuilder);
            return pluginHostBuilder.Build();
        }

        protected abstract Type[] GetPluginTypes();

        protected virtual IPluginHostBuilder ConfigurePlugins(IPluginHostBuilder pluginHostBuilder)
            => pluginHostBuilder
                .ConfigureServices(plugins => {
                    plugins.AddSingleton<IAppHost>(this);
                    if (!plugins.HasService<IConsole>())
                        plugins.AddSingleton<IConsole>(new SystemConsole());
                    return plugins;
                });

        protected virtual Parser BuildCliParser()
        {
            var builder = CreateCliBuilder().UsePlugins<ICliPlugin>(Plugins);
            builder = ConfigureCliBuilder(builder);
            return builder.Build();
        }

        protected virtual CommandLineBuilder CreateCliBuilder()
            => new CommandLineBuilder();

        protected virtual CommandLineBuilder ConfigureCliBuilder(CommandLineBuilder builder)
            => builder.UseDefaults();

        protected virtual void BuildWebHost()
        {
            var builder = CreateWebHostBuilder().UsePlugins<IWebHostPlugin>(Plugins);
            builder = ConfigureWebHostBuilder(builder);
            var webHost = builder.Build();
            SetWebHost(webHost);
        }

        protected virtual IWebHostBuilder CreateWebHostBuilder() => new WebHostBuilder();

        protected virtual IWebHostBuilder ConfigureWebHostBuilder(IWebHostBuilder builder)
            => builder
                .ConfigureServices(services => {
                    services.AddSingleton<IAppHost>(this);
                });

        protected virtual void SetWebHost(IWebHost webHost)
        {
            WebHost = webHost;
            Services = webHost.Services;
            _webHostReadyTcs.SetResult(webHost);
        }
    }
}
