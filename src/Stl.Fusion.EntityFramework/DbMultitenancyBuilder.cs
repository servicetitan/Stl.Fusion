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

    internal DbMultitenancyBuilder(DbContextBuilder<TDbContext> dbContext)
    {
        DbContext = dbContext;

        // Core multitenancy services
        Services.TryAddSingleton<DefaultTenantResolver<TDbContext>.Options>();
        Services.TryAddSingleton<ITenantResolver<TDbContext>, DefaultTenantResolver<TDbContext>>();
        Services.TryAddSingleton<ITenantRegistry<TDbContext>, SingleTenantRegistry<TDbContext>>();
        Services.TryAddSingleton<IMultitenantDbContextFactory<TDbContext>, SingleTenantDbContextFactory<TDbContext>>();
    }

    // Mode

    public DbMultitenancyBuilder<TDbContext> SingleTenantMode()
    {
        Clear();
        AddTenantRegistry<SingleTenantRegistry<TDbContext>>();
        AddMultitenantDbContextFactory<SingleTenantDbContextFactory<TDbContext>>();
        AddTenantResolver<DefaultTenantResolver<TDbContext>>();
        Services.AddSingleton<DefaultTenantResolver<TDbContext>.Options>();
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> MultitenantMode()
    {
        Clear();
        AddTenantRegistry<MultitenantRegistry<TDbContext>>();
        AddMultitenantDbContextFactory<MultitenantDbContextFactory<TDbContext>>();
        AddTenantResolver<DefaultTenantResolver<TDbContext>>();
        Services.AddSingleton<DefaultTenantResolver<TDbContext>.Options>();
        return this;
    }

    // SetupMultitenantRegistry

    public DbMultitenancyBuilder<TDbContext> SetupMultitenantRegistry(params Tenant[] tenants)
        => SetupMultitenantRegistry(tenants.AsEnumerable());

    public DbMultitenancyBuilder<TDbContext> SetupMultitenantRegistry(IEnumerable<Tenant> tenants)
    {
        var allTenants = tenants.ToImmutableDictionary(t => t.Id);
        SetupMultitenantRegistry(_ => new () {
            AllTenantsFetcher = () => allTenants,
        });
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> SetupMultitenantRegistry(
        Func<IServiceProvider, MultitenantRegistry<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> SetupMultitenantDbContextFactory(
        Action<IServiceProvider, Tenant, DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        SetupMultitenantDbContextFactory((c, tenant, services) => {
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

    // SetupMultitenantDbContextFactory

    public DbMultitenancyBuilder<TDbContext> SetupMultitenantDbContextFactory(
        Action<IServiceProvider, Tenant, ServiceCollection> dbServiceCollectionBuilder)
    {
        SetupMultitenantDbContextFactory(_ => new() {
            DbServiceCollectionBuilder = dbServiceCollectionBuilder,
        });
        return this;
    }

    public DbMultitenancyBuilder<TDbContext> SetupMultitenantDbContextFactory(
        Func<IServiceProvider, MultitenantDbContextFactory<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    // MakeDefault / UseDefault / UseFrom

    public DbMultitenancyBuilder<TDbContext> MakeDefault()
    {
        Services.AddTransient<ITenantRegistry>(c => c.GetRequiredService<ITenantRegistry<TDbContext>>());
        Services.AddTransient<ITenantResolver>(c => c.GetRequiredService<ITenantResolver<TDbContext>>());
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
