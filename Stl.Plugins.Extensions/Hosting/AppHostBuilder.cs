namespace Stl.Plugins.Extensions.Hosting
{
    public interface IAppHostBuilder
    {
        IPluginHostBuilder PluginHostBuilder { get; set; }
        IAppHostBuilderImpl Implementation { get; }
    }

    public interface IAppHostBuilder<THost> : IAppHostBuilder
        where THost : IAppHost
    { }

    public interface IAppHostBuilderImpl 
    { 
        IAppHost CreateHost();
    }

    public abstract class AppHostBuilderBase<THost> : IAppHostBuilder<THost>, IAppHostBuilderImpl
        where THost : IAppHost
    {
        public IPluginHostBuilder PluginHostBuilder { get; set; } = new PluginHostBuilder();
        public IAppHostBuilderImpl Implementation => this;

        IAppHost IAppHostBuilderImpl.CreateHost() => CreateHost();
        protected abstract IAppHost CreateHost();
    }
}
