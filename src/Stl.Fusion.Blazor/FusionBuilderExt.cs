using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.UI;

namespace Stl.Fusion.Blazor;

public static class FusionBuilderExt
{
    public static FusionBuilder AddBlazorUIServices(this FusionBuilder fusion)
    {
        var services = fusion.Services;
        services.TryAddScoped(c => new UICommander(c));
        services.TryAddScoped(_ => new UIActionFailureTracker.Options());
        services.TryAddScoped(c => new UIActionFailureTracker(
            c.GetRequiredService<UIActionFailureTracker.Options>(), c));
        services.TryAddScoped(c => new BlazorModeHelper(
            c.GetRequiredService<NavigationManager>()));
        services.TryAddScoped(_ => new BlazorCircuitContext());
        return fusion;
    }
}
