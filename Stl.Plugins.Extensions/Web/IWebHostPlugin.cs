using Microsoft.AspNetCore.Hosting;
using Stl.Extensibility;

namespace Stl.Plugins.Extensions.Web 
{
    public interface IWebHostPlugin
    {
        void Use(WebHostPluginInvocation invocation);
    }

    public class WebHostPluginInvocation : InvokerBase<IWebHostPlugin, WebHostPluginInvocation>
    {
        public IWebHostBuilder Builder { get; set; } = default!;
    }
}
