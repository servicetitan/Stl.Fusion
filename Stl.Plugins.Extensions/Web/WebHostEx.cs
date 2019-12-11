using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Plugins.Extensions.Web
{
    public static class WebHostEx
    {
        public static IPluginHost Plugins(this IWebHost host)
            => host.Services.GetRequiredService<IPluginHost>();
    }
}
