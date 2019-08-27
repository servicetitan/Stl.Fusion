using Microsoft.AspNetCore.Hosting;

namespace Stl.Plugins.Extensions.Hosting
{
    public interface IAppHostBuilder
    {
        IPluginHostBuilder PluginHostBuilder { get; set; }
        IWebHostBuilder WebHostBuilder { get; set; }
        IAppHostBuilderImpl Implementation { get; }
    }

    public interface IAppHostBuilder<THost> : IAppHostBuilder
        where THost : class, IAppHost
    { }

    public interface IAppHostBuilderImpl 
    { 
        IAppHost CreateHost();
    }

    public abstract class AppHostBuilderBase<THost> : IAppHostBuilder<THost>, IAppHostBuilderImpl
        where THost : class, IAppHost
    {
        public IPluginHostBuilder PluginHostBuilder { get; set; } = new PluginHostBuilder();
        public IWebHostBuilder WebHostBuilder { get; set; } = new WebHostBuilder();
        public IAppHostBuilderImpl Implementation => this;

        IAppHost IAppHostBuilderImpl.CreateHost() => CreateHost();
        protected abstract IAppHost CreateHost();
    }

    public class AppHostBuilder<THost> : AppHostBuilderBase<THost>
        where THost : class, IAppHost, new()
    {
        protected override IAppHost CreateHost() => new THost();
    }
}
