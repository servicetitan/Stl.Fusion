using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Extensions.Services;

namespace Stl.Fusion.Extensions;

public static class FusionBuilderExt
{
    // SandboxedKeyValueStore

    public static FusionBuilder AddSandboxedKeyValueStore(this FusionBuilder fusion,
        Func<IServiceProvider, SandboxedKeyValueStore.Options>? optionsFactory = null)
    {
        var services = fusion.Services;
        services.AddSingleton(optionsFactory, _ => SandboxedKeyValueStore.Options.Default);
        fusion.AddService<ISandboxedKeyValueStore, SandboxedKeyValueStore>();
        return fusion;
    }

    // InMemoryKeyValueStore

    public static FusionBuilder AddInMemoryKeyValueStore(this FusionBuilder fusion,
        Func<IServiceProvider, InMemoryKeyValueStore.Options>? optionsFactory = null)
    {
        var services = fusion.Services;
        services.AddSingleton(optionsFactory, _ => InMemoryKeyValueStore.Options.Default);
        fusion.AddService<IKeyValueStore, InMemoryKeyValueStore>();
        services.AddHostedService(c => (InMemoryKeyValueStore)c.GetRequiredService<IKeyValueStore>());
        return fusion;
    }

    // DbKeyValueStore

    public static FusionBuilder AddDbKeyValueStore<TDbContext>(
        this FusionBuilder fusion,
        Func<IServiceProvider, DbKeyValueTrimmer<TDbContext, DbKeyValue>.Options>? keyValueTrimmerOptionsFactory = null)
        where TDbContext : DbContext
        => fusion.AddDbKeyValueStore<TDbContext, DbKeyValue>(keyValueTrimmerOptionsFactory);

    public static FusionBuilder AddDbKeyValueStore<TDbContext, TDbKeyValue>(
        this FusionBuilder fusion,
        Func<IServiceProvider, DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options>? keyValueTrimmerOptionsFactory = null)
        where TDbContext : DbContext
        where TDbKeyValue : DbKeyValue, new()
    {
        var services = fusion.Services;
        services.AddSingleton(keyValueTrimmerOptionsFactory, _ => DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options.Default);
        if (services.HasService<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>())
            return fusion;

        var dbContext = services.AddDbContextServices<TDbContext>();
        dbContext.TryAddEntityResolver<string, TDbKeyValue>();
        fusion.AddService<IKeyValueStore, DbKeyValueStore<TDbContext, TDbKeyValue>>();

        // DbKeyValueTrimmer - hosted service!
        services.TryAddSingleton<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>();
        services.AddHostedService(c => c.GetRequiredService<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>());
        return fusion;
    }
}
