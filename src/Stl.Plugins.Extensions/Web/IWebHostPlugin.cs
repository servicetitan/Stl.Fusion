using Microsoft.AspNetCore.Hosting;
using Stl.Extensibility;

namespace Stl.Plugins.Extensions.Web 
{
    public interface IWebHostPlugin
    {
        void Use(WebHostPluginInvoker invoker);
    }

    public class WebHostPluginInvoker : InvokerBase<IWebHostPlugin, WebHostPluginInvoker>
    {
        public IWebHostBuilder Builder { get; set; } = default!;
    }
}
