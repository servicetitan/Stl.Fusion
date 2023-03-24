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
        fusion.AddComputeService<IFusionTime, FusionTime>();
        return fusion;
    }

    public static FusionBuilder AddBackendStatus(this FusionBuilder fusion)
        => fusion.AddBackendStatus<BackendStatus>();
    public static FusionBuilder AddBackendStatus<TBackendStatus>(this FusionBuilder fusion)
        where TBackendStatus : BackendStatus
    {
        fusion.AddComputeService<TBackendStatus>();
        fusion.Services.TryAddSingleton<BackendStatus>(c => c.GetRequiredService<TBackendStatus>());
        return fusion;
    }

    public static FusionBuilder AddInMemoryKeyValueStore(this FusionBuilder fusion,
        Func<IServiceProvider, InMemoryKeyValueStore.Options>? optionsFactory = null)
    {
        var services = fusion.Services;
        services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        fusion.AddComputeService<IKeyValueStore, InMemoryKeyValueStore>();
        services.AddHostedService(c => (InMemoryKeyValueStore) c.GetRequiredService<IKeyValueStore>());
        return fusion;
    }

    public static FusionBuilder AddSandboxedKeyValueStore(this FusionBuilder fusion,
        Func<IServiceProvider, SandboxedKeyValueStore.Options>? optionsFactory = null)
    {
        var services = fusion.Services;
        services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        fusion.AddComputeService<ISandboxedKeyValueStore, SandboxedKeyValueStore>();
        return fusion;
    }
}
