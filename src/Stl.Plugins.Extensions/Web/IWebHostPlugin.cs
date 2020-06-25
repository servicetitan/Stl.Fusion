using Stl.Extensibility;
using Microsoft.AspNetCore.Hosting;

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
