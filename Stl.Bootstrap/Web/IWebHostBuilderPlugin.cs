using Microsoft.AspNetCore.Hosting;
using Stl.Extensibility;
using Stl.Plugins;

namespace Stl.Bootstrap.Web 
{
    public interface IWebHostBuilderPlugin
    {
        IWebHostBuilder Configure(ICallChain<WebHostBuilderPluginConfigureInvocation> chain);
    }

    public class WebHostBuilderPluginConfigureInvocation : 
        PluginInvocation<IWebHostBuilderPlugin, WebHostBuilderPluginConfigureInvocation>
    {
        public IWebHostBuilder Builder { get; set; }
    }
}
