using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.UI;

namespace Stl.Fusion.Blazor
{
    public static class FusionBuilderEx
    {
        public static FusionBuilder AddBlazorUIServices(this FusionBuilder fusion)
        {
            var services = fusion.Services;
            services.TryAddScoped<UICommandRunner>();
            services.TryAddScoped<UICommandFailureList>();
            services.TryAddScoped<BlazorModeHelper>();
            return fusion;
        }
    }
}
