using Microsoft.AspNetCore.Hosting;
using Stl.Extensibility;

namespace Stl.Bootstrap.Web 
{
    public interface IWebHostBuilderPlugin
    {
        void Configure(WebHostBuilderPluginConfigureInvocation invocation);
    }

    public class WebHostBuilderPluginConfigureInvocation 
        : ChainInvocationBase<IWebHostBuilderPlugin, WebHostBuilderPluginConfigureInvocation>
    {
        public IWebHostBuilder Builder { get; set; } = default!;
    }
}
