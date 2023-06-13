using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Extensions.Internal;

namespace Stl.Fusion.Extensions;

public static class FusionBuilderExt
{
    public static FusionBuilder AddFusionTime(this FusionBuilder fusion,
        Func<IServiceProvider, FusionTime.Options>? optionsFactory = null)
    {
        var services = fusion.Services;
        services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        fusion.AddService<IFusionTime, FusionTime>();
        return fusion;
    }
}
