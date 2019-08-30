using Microsoft.Extensions.Hosting;
using Stl.Extensibility;

namespace Stl.Plugins.Extensions.Hosting
{
    public interface IHostPlugin
    {
        void Use(HostPluginInvoker invoker);
    }

    public class HostPluginInvoker : InvokerBase<IHostPlugin, HostPluginInvoker>
    {
        public IHostBuilder Builder { get; set; } = default!;
    }
}
