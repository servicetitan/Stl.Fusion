using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.EntityFramework.Extensions;
using Stl.Fusion.Extensions;

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
        if (Services.HasService<DbHub<TDbContext>>()) {
            configure?.Invoke(this);
            return;
        }

        Services.TryAddSingleton<DbHub<TDbContext>>();
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
        if (optionsFactory != null)
            Services.AddSingleton(optionsFactory);
        else
            Services.TryAddSingleton<DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>();
        Services.AddSingleton<
            IDbEntityResolver<TKey, TDbEntity>,
            DbEntityResolver<TDbContext, TKey, TDbEntity>>();
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
        if (optionsFactory != null)
            Services.AddSingleton(optionsFactory);
        else
            Services.TryAddSingleton<DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>();
        Services.TryAddSingleton<
            IDbEntityResolver<TKey, TDbEntity>,
            DbEntityResolver<TDbContext, TKey, TDbEntity>>();
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

    // Authentication

    public DbAuthenticationBuilder<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId> 
        AddAuthentication<TDbUserId>()
        where TDbUserId : notnull
        => AddAuthentication<DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>();

    public DbAuthenticationBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> 
        AddAuthentication<TDbSessionInfo, TDbUser, TDbUserId>()
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUser : DbUser<TDbUserId>, new()
        where TDbUserId : notnull
        => new(this, null);

    public DbContextBuilder<TDbContext> AddAuthentication<TDbUserId>(
        Action<DbAuthenticationBuilder<TDbContext, DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>> configure)
        where TDbUserId : notnull
        => AddAuthentication<DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>(configure);

    public DbContextBuilder<TDbContext> AddAuthentication<TDbSessionInfo, TDbUser, TDbUserId>(
        Action<DbAuthenticationBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>> configure)
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUser : DbUser<TDbUserId>, new()
        where TDbUserId : notnull
        => new DbAuthenticationBuilder<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>(this, configure).DbContext;

    // KeyValueStore

    public DbContextBuilder<TDbContext> AddKeyValueStore(
        Func<IServiceProvider, DbKeyValueTrimmer<TDbContext, DbKeyValue>.Options>? keyValueTrimmerOptionsFactory = null)
        => AddKeyValueStore<DbKeyValue>(keyValueTrimmerOptionsFactory);

    public DbContextBuilder<TDbContext> AddKeyValueStore<TDbKeyValue>(
        Func<IServiceProvider, DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options>? keyValueTrimmerOptionsFactory = null)
        where TDbKeyValue : DbKeyValue, new()
    {
        var isConfigured = Services.HasService<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>();

        if (keyValueTrimmerOptionsFactory != null)
            Services.AddSingleton(keyValueTrimmerOptionsFactory);
        if (isConfigured)
            return this;

        TryAddEntityResolver<string, TDbKeyValue>();
        var fusion = Services.AddFusion();
        fusion.AddComputeService<DbKeyValueStore<TDbContext, TDbKeyValue>>();
        Services.TryAddSingleton<IKeyValueStore>(c => c.GetRequiredService<DbKeyValueStore<TDbContext, TDbKeyValue>>());

        // DbKeyValueTrimmer - hosted service!
        Services.TryAddSingleton<DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options>();
        Services.TryAddSingleton<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>();
        Services.AddHostedService(c => c.GetRequiredService<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>());
        return this;
    }
}
