using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Stl.Extensibility;

namespace Stl.Hosting.Plugins
{
    public interface IConfigureWebAppPlugin : IAppHostBuilderPlugin
    {
        void Use(ConfigureWebAppPluginInvoker invoker);
    }

    public class ConfigureWebAppPluginInvoker : InvokerBase<IConfigureWebAppPlugin, ConfigureWebAppPluginInvoker>
    {
        public IAppHostBuilder AppHostBuilder { get; set; } = default!;
        public WebHostBuilderContext Context { get; set; } = default!;
        public IApplicationBuilder AppBuilder { get; set; } = default!;
    }
}
