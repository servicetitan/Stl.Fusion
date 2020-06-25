using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Stl.Plugins.Extensions.Web
{
    public static class WebHostEx
    {
        public static IPluginHost Plugins(this IHost host)
            => host.Services.GetRequiredService<IPluginHost>();
    }
}
