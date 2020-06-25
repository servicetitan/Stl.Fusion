using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Stl.Plugins.Extensions.Hosting
{
    public static class HostEx
    {
        public static IPluginHost Plugins(this IHost host)
            => host.Services.GetRequiredService<IPluginHost>();
    }
}
