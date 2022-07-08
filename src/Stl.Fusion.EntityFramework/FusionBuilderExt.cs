using System.Data;

namespace Stl.Fusion.EntityFramework;

public static class FusionBuilderExt
{
    // AddGlobalIsolationLevelSelector

    public static FusionBuilder AddGlobalIsolationLevelSelector(
        this FusionBuilder fusion,
        Func<IServiceProvider, GlobalIsolationLevelSelector> globalIsolationLevelSelector)
    {
        fusion.Services.AddSingleton(globalIsolationLevelSelector);
        return fusion;
    }

    public static FusionBuilder AddGlobalIsolationLevelSelector(
        this FusionBuilder fusion,
        Func<IServiceProvider, CommandContext, IsolationLevel> globalIsolationLevelSelector)
    {
        fusion.Services.AddSingleton(c => new GlobalIsolationLevelSelector(
            context => globalIsolationLevelSelector.Invoke(c, context)));
        return fusion;
    }
}
