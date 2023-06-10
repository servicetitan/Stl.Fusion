using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Extensions.Services;

namespace Stl.Fusion.Extensions;

public static class FusionBuilderExt
{
    // SandboxedKeyValueStore

    public static FusionBuilder AddSandboxedKeyValueStore(this FusionBuilder fusion, bool expose = true)
        => fusion.AddSandboxedKeyValueStore(null, expose);

    public static FusionBuilder AddSandboxedKeyValueStore(this FusionBuilder fusion,
        Func<IServiceProvider, SandboxedKeyValueStore.Options>? optionsFactory = null,
        bool expose = true)
    {
        var services = fusion.Services;
        services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        if (expose)
            fusion.AddComputeServer<ISandboxedKeyValueStore, SandboxedKeyValueStore>();
        else
            fusion.AddComputeService<ISandboxedKeyValueStore, SandboxedKeyValueStore>();
        return fusion;
    }

    // InMemoryKeyValueStore

    public static FusionBuilder AddInMemoryKeyValueStore(this FusionBuilder fusion,
        Func<IServiceProvider, InMemoryKeyValueStore.Options>? optionsFactory = null)
    {
        var services = fusion.Services;
        services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        fusion.AddComputeService<IKeyValueStore, InMemoryKeyValueStore>();
        services.AddHostedService(c => (InMemoryKeyValueStore)c.GetRequiredService<IKeyValueStore>());
        return fusion;
    }

    // DbKeyValueStore

    public static FusionBuilder AddKeyValueStore<TDbContext>(
        this FusionBuilder fusion,
        Func<IServiceProvider, DbKeyValueTrimmer<TDbContext, DbKeyValue>.Options>? keyValueTrimmerOptionsFactory = null)
        where TDbContext : DbContext
        => fusion.AddKeyValueStore<TDbContext, DbKeyValue>(keyValueTrimmerOptionsFactory);

    public static FusionBuilder AddKeyValueStore<TDbContext, TDbKeyValue>(
        this FusionBuilder fusion,
        Func<IServiceProvider, DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options>? keyValueTrimmerOptionsFactory = null)
        where TDbContext : DbContext
        where TDbKeyValue : DbKeyValue, new()
    {
        var services = fusion.Services;
        var isAlreadyAdded = services.HasService<DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options>();
        if (keyValueTrimmerOptionsFactory != null)
            services.AddSingleton(keyValueTrimmerOptionsFactory);
        if (isAlreadyAdded)
            return fusion;

        var dbContext = services.AddDbContextServices<TDbContext>();
        dbContext.TryAddEntityResolver<string, TDbKeyValue>();
        fusion.AddComputeService<DbKeyValueStore<TDbContext, TDbKeyValue>>();
        services.TryAddSingleton<IKeyValueStore>(c => c.GetRequiredService<DbKeyValueStore<TDbContext, TDbKeyValue>>());

        // DbKeyValueTrimmer - hosted service!
        services.TryAddSingleton<DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options>();
        services.TryAddSingleton<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>();
        services.AddHostedService(c => c.GetRequiredService<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>());
        return fusion;
    }
}
