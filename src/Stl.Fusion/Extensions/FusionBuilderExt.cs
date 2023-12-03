using Stl.Fusion.Extensions.Internal;

namespace Stl.Fusion.Extensions;

public static class FusionBuilderExt
{
    public static FusionBuilder AddFusionTime(this FusionBuilder fusion,
        Func<IServiceProvider, FusionTime.Options>? optionsFactory = null)
    {
        var services = fusion.Services;
        services.AddSingleton(optionsFactory, _ => FusionTime.Options.Default);
        if (!services.HasService<IFusionTime>())
            fusion.AddService<IFusionTime, FusionTime>();
        return fusion;
    }
}
