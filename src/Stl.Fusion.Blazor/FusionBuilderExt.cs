using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.UI;

namespace Stl.Fusion.Blazor
{
    public static class FusionBuilderExt
    {
        public static FusionBuilder AddBlazorUIServices(this FusionBuilder fusion)
        {
            var services = fusion.Services;
            services.TryAddScoped<UICommandRunner>();
            services.TryAddScoped<UICommandFailureList>();
            services.TryAddScoped<BlazorModeHelper>();
            services.TryAddScoped<BlazorCircuitContext>();
            return fusion;
        }
    }
}
