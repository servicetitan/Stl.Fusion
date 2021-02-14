using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Fusion.Blazor
{
    public static class FusionBuilderEx
    {
        public static FusionBuilder AddBlazorUIComponents(this FusionBuilder fusion)
        {
            var services = fusion.Services;
            services.TryAddTransient<CommandRunner>();
            services.TryAddScoped<BlazorModeHelper>();
            return fusion;
        }
    }
}
