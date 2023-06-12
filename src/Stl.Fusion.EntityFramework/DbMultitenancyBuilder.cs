using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Multitenancy;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework;

public readonly struct DbMultitenancyBuilder<TDbContext>
    where TDbContext : DbContext
{
    public DbContextBuilder<TDbContext> DbContext { get; }
    public IServiceCollection Services => DbContext.Services;

    internal DbMultitenancyBuilder(
        DbContextBuilder<TDbContext> dbContext,
        Action<DbMultitenancyBuilder<TDbContext>>? configure)
    {
        DbContext = dbContext;
        var services = Services;
        if (services.HasService<IMultitenantDbContextFactory<TDbContext>>()) {
            configure?.Invoke(this);
            return;
        }

        // Core multitenancy services
        services.TryAddSingleton<DefaultTenantResolver<TDbContext>.Options>();
        services.TryAddSingleton<ITenantResolver<TDbContext>, DefaultTenantResolver<TDbContext>>();
        services.TryAddSingleton<ITenantRegistry<TDbContext>, SingleTenantRegistry<TDbContext>>();
        services.TryAddSingleton<IMultitenantDbContextFactory<TDbContext>, SingleTenantDbContextFactory<TDbContext>>();

        configure?.Invoke(this);
    }

    // Mode

    public DbMultitenancyBuilder<TDbContext> UseSingleTenantMode()
    {
        Clear();
        AddTenantRegistry<SingleTenantRegistry<TDbContext>>();
        AddMultitenantDbContextFactory<SingleTenantDbContextFactory<TDbContext>>();
        AddTenantResolver<DefaultTenantResolver<TDbContext>>();
        Services.AddSingleton<DefaultTenantResolver<TDbContext>.Options>();
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> UseMultitenantMode()
    {
        Clear();
        AddTenantRegistry<MultitenantRegistry<TDbContext>>();
        AddMultitenantDbContextFactory<MultitenantDbContextFactory<TDbContext>>();
        AddTenantResolver<DefaultTenantResolver<TDbContext>>();
        Services.AddSingleton<DefaultTenantResolver<TDbContext>.Options>();
        return this;
    }

    // AddMultitenantRegistry

    public DbMultitenancyBuilder<TDbContext> AddMultitenantRegistry(params Tenant[] tenants)
        => AddMultitenantRegistry(tenants.AsEnumerable());

    public DbMultitenancyBuilder<TDbContext> AddMultitenantRegistry(IEnumerable<Tenant> tenants)
    {
        var allTenants = tenants.ToImmutableDictionary(t => t.Id);
        AddMultitenantRegistry(_ => new () {
            AllTenantsFetcher = () => allTenants,
        });
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> AddMultitenantRegistry(
        Func<IServiceProvider, MultitenantRegistry<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> AddMultitenantDbContextFactory(
        Action<IServiceProvider, Tenant, DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        AddMultitenantDbContextFactory((c, tenant, services) => {
            services.AddPooledDbContextFactory<TDbContext>(
                db => {
                    // This ensures logging settings from the main container
                    // are applied to tenant DbContexts
                    var loggerFactory = c.GetService<ILoggerFactory>();
                    if (loggerFactory != null)
                        db.UseLoggerFactory(loggerFactory);
                    dbContextOptionsBuilder.Invoke(c, tenant, db);
                });
        });
        return this;
    }

    // AddMultitenantDbContextFactory

    public DbMultitenancyBuilder<TDbContext> AddMultitenantDbContextFactory(
        Action<IServiceProvider, Tenant, ServiceCollection> dbServiceCollectionBuilder)
    {
        AddMultitenantDbContextFactory(_ => new() {
            DbServiceCollectionBuilder = dbServiceCollectionBuilder,
        });
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> AddMultitenantDbContextFactory(
        Func<IServiceProvider, MultitenantDbContextFactory<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    // MakeDefault / UseDefault / UseFrom

    public DbMultitenancyBuilder<TDbContext> MakeDefault()
    {
        Services.AddAlias<ITenantRegistry, ITenantRegistry<TDbContext>>();
        Services.AddAlias<ITenantResolver, ITenantResolver<TDbContext>>();
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> UseDefault()
    {
        Services.AddSingleton<ITenantRegistry<TDbContext>>(
            c => new TenantRegistryAlias<TDbContext>(c.GetRequiredService<ITenantRegistry>()));
        Services.AddSingleton<ITenantResolver<TDbContext>>(
            c => new TenantResolverAlias<TDbContext>(c.GetRequiredService<ITenantResolver>()));
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> UseFrom<TSourceContext>()
    {
        Services.AddSingleton<ITenantRegistry<TDbContext>>(
            c => new TenantRegistryAlias<TSourceContext, TDbContext>(c.GetRequiredService<ITenantRegistry<TSourceContext>>()));
        Services.AddSingleton<ITenantResolver<TDbContext>>(
            c => new TenantResolverAlias<TSourceContext, TDbContext>(c.GetRequiredService<ITenantResolver<TSourceContext>>()));
        return this;
    }

    // AddXxx & Clear helpers

    public DbMultitenancyBuilder<TDbContext> AddTenantRegistry(
        Func<IServiceProvider, ITenantRegistry<TDbContext>> factory)
    {
        Services.AddSingleton(factory);
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> AddTenantRegistry<TTenantRegistry>()
        where TTenantRegistry : class, ITenantRegistry<TDbContext>
    {
        Services.AddSingleton<ITenantRegistry<TDbContext>, TTenantRegistry>();
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> AddTenantResolver(
        Func<IServiceProvider, ITenantResolver<TDbContext>> factory)
    {
        Services.AddSingleton(factory);
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> AddTenantResolver<TTenantResolver>()
        where TTenantResolver : class, ITenantResolver<TDbContext>
    {
        Services.AddSingleton<ITenantResolver<TDbContext>, TTenantResolver>();
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> AddMultitenantDbContextFactory(
        Func<IServiceProvider, IMultitenantDbContextFactory<TDbContext>> factory)
    {
        Services.AddSingleton(factory);
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> AddMultitenantDbContextFactory<TMultitenantDbContextFactory>()
        where TMultitenantDbContextFactory : class, IMultitenantDbContextFactory<TDbContext>
    {
        Services.AddSingleton<IMultitenantDbContextFactory<TDbContext>, TMultitenantDbContextFactory>();
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> Clear()
    {
        Services.RemoveAll<ITenantRegistry<TDbContext>>();
        Services.RemoveAll<ITenantResolver<TDbContext>>();
        Services.RemoveAll<IMultitenantDbContextFactory<TDbContext>>();
        Services.RemoveAll<DefaultTenantResolver<TDbContext>.Options>();
        Services.RemoveAll<MultitenantDbContextFactory<TDbContext>.Options>();
        return this;
    }
}
