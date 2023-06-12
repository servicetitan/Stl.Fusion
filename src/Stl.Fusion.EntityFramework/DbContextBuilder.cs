using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Fusion.EntityFramework;

public readonly struct DbContextBuilder<TDbContext>
    where TDbContext : DbContext
{
    public IServiceCollection Services { get; }

    internal DbContextBuilder(
        IServiceCollection services,
        Action<DbContextBuilder<TDbContext>>? configure)
    {
        Services = services;
        if (services.HasService<DbHub<TDbContext>>()) {
            configure?.Invoke(this);
            return;
        }

        services.TryAddSingleton<DbHub<TDbContext>>();
        AddMultitenancy(); // Core multitenancy services

        configure?.Invoke(this);
    }

    // Multitenancy

    public DbMultitenancyBuilder<TDbContext> AddMultitenancy()
        => new(this, null);

    public DbContextBuilder<TDbContext> AddMultitenancy(Action<DbMultitenancyBuilder<TDbContext>> configure)
        => new DbMultitenancyBuilder<TDbContext>(this, configure).DbContext;

    // Entity converters

    public DbContextBuilder<TDbContext> AddEntityConverter<TDbEntity, TEntity, TConverter>()
        where TDbEntity : class
        where TEntity : notnull
        where TConverter : class, IDbEntityConverter<TDbEntity, TEntity>
    {
        Services.AddSingleton<IDbEntityConverter<TDbEntity, TEntity>, TConverter>();
        return this;
    }

    public DbContextBuilder<TDbContext> TryAddEntityConverter<TDbEntity, TEntity, TConverter>()
        where TDbEntity : class
        where TEntity : notnull
        where TConverter : class, IDbEntityConverter<TDbEntity, TEntity>
    {
        Services.TryAddSingleton<IDbEntityConverter<TDbEntity, TEntity>, TConverter>();
        return this;
    }

    // Entity resolvers

    public DbContextBuilder<TDbContext> AddEntityResolver<TKey, TDbEntity>(
        Func<IServiceProvider, DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>? optionsFactory = null)
        where TKey : notnull
        where TDbEntity : class
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => DbEntityResolver<TDbContext, TKey, TDbEntity>.Options.Default);
        Services.AddSingleton<IDbEntityResolver<TKey, TDbEntity>>(c => new DbEntityResolver<TDbContext, TKey, TDbEntity>(
            c.GetRequiredService<DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>(), c));
        return this;
    }

    public DbContextBuilder<TDbContext> AddEntityResolver<TKey, TDbEntity, TResolver>(
        Func<IServiceProvider, TResolver> resolverFactory)
        where TKey : notnull
        where TDbEntity : class
        where TResolver : class, IDbEntityResolver<TKey, TDbEntity>
    {
        Services.AddSingleton<IDbEntityResolver<TKey, TDbEntity>>(resolverFactory);
        return this;
    }

    public DbContextBuilder<TDbContext> TryAddEntityResolver<TKey, TDbEntity>(
        Func<IServiceProvider, DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>? optionsFactory = null)
        where TKey : notnull
        where TDbEntity : class
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => DbEntityResolver<TDbContext, TKey, TDbEntity>.Options.Default);
        Services.TryAddSingleton<IDbEntityResolver<TKey, TDbEntity>>(c => new DbEntityResolver<TDbContext, TKey, TDbEntity>(
            c.GetRequiredService<DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>(), c));
        return this;
    }

    public DbContextBuilder<TDbContext> TryAddEntityResolver<TKey, TDbEntity, TResolver>(
        Func<IServiceProvider, TResolver> resolverFactory)
        where TKey : notnull
        where TDbEntity : class
        where TResolver : class, IDbEntityResolver<TKey, TDbEntity>
    {
        Services.TryAddSingleton<IDbEntityResolver<TKey, TDbEntity>>(resolverFactory);
        return this;
    }

    // Operations

    public DbOperationsBuilder<TDbContext> AddOperations()
        => new(this, null);

    public DbContextBuilder<TDbContext> AddOperations(Action<DbOperationsBuilder<TDbContext>> configure)
        => new DbOperationsBuilder<TDbContext>(this, configure).DbContext;
}
