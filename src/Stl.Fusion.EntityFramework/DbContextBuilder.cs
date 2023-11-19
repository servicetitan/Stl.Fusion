using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR.Internal;

namespace Stl.Fusion.EntityFramework;

public readonly struct DbContextBuilder<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext>
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
        TryAddTransientErrorDetector(_ => TransientErrorDetector.DefaultPreferTransient);

        configure?.Invoke(this);
    }

    // Multitenancy

    public DbMultitenancyBuilder<TDbContext> AddMultitenancy()
        => new(this, null);

    public DbContextBuilder<TDbContext> AddMultitenancy(Action<DbMultitenancyBuilder<TDbContext>> configure)
        => new DbMultitenancyBuilder<TDbContext>(this, configure).DbContext;

    // Transient error detector

    public DbContextBuilder<TDbContext> AddTransientErrorDetector(Func<IServiceProvider, ITransientErrorDetector> detectorFactory)
    {
        Services.AddSingleton(c => detectorFactory.Invoke(c).For<TDbContext>());
        return this;
    }

    public DbContextBuilder<TDbContext> TryAddTransientErrorDetector(Func<IServiceProvider, ITransientErrorDetector> detectorFactory)
    {
        Services.TryAddSingleton(c => detectorFactory.Invoke(c).For<TDbContext>());
        return this;
    }

    // Entity converters

    public DbContextBuilder<TDbContext> AddEntityConverter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TConverter>()
        where TDbEntity : class
        where TEntity : notnull
        where TConverter : class, IDbEntityConverter<TDbEntity, TEntity>
    {
        Services.AddSingleton<IDbEntityConverter<TDbEntity, TEntity>, TConverter>();
        return this;
    }

    public DbContextBuilder<TDbContext> TryAddEntityConverter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TConverter>()
        where TDbEntity : class
        where TEntity : notnull
        where TConverter : class, IDbEntityConverter<TDbEntity, TEntity>
    {
        Services.TryAddSingleton<IDbEntityConverter<TDbEntity, TEntity>, TConverter>();
        return this;
    }

    // Entity resolvers

    public DbContextBuilder<TDbContext> AddEntityResolver<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TKey,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbEntity>
        (Func<IServiceProvider, DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>? optionsFactory = null)
        where TKey : notnull
        where TDbEntity : class
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => DbEntityResolver<TDbContext, TKey, TDbEntity>.Options.Default);
        Services.AddSingleton<IDbEntityResolver<TKey, TDbEntity>>(c => new DbEntityResolver<TDbContext, TKey, TDbEntity>(
            c.GetRequiredService<DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>(), c));
        return this;
    }

    public DbContextBuilder<TDbContext> AddEntityResolver<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TKey,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResolver>
        (Func<IServiceProvider, TResolver> resolverFactory)
        where TKey : notnull
        where TDbEntity : class
        where TResolver : class, IDbEntityResolver<TKey, TDbEntity>
    {
        Services.AddSingleton<IDbEntityResolver<TKey, TDbEntity>>(resolverFactory);
        return this;
    }

    public DbContextBuilder<TDbContext> TryAddEntityResolver<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TKey,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbEntity>
        (Func<IServiceProvider, DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>? optionsFactory = null)
        where TKey : notnull
        where TDbEntity : class
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => DbEntityResolver<TDbContext, TKey, TDbEntity>.Options.Default);
        Services.TryAddSingleton<IDbEntityResolver<TKey, TDbEntity>>(c => new DbEntityResolver<TDbContext, TKey, TDbEntity>(
            c.GetRequiredService<DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>(), c));
        return this;
    }

    public DbContextBuilder<TDbContext> TryAddEntityResolver<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TKey,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResolver>
        (Func<IServiceProvider, TResolver> resolverFactory)
        where TKey : notnull
        where TDbEntity : class
        where TResolver : class, IDbEntityResolver<TKey, TDbEntity>
    {
        Services.TryAddSingleton<IDbEntityResolver<TKey, TDbEntity>>(resolverFactory);
        return this;
    }

    // Operations

    [RequiresUnreferencedCode(UnreferencedCode.Commander)]
    public DbOperationsBuilder<TDbContext> AddOperations()
        => new(this, null);

    [RequiresUnreferencedCode(UnreferencedCode.Commander)]
    public DbContextBuilder<TDbContext> AddOperations(Action<DbOperationsBuilder<TDbContext>> configure)
        => new DbOperationsBuilder<TDbContext>(this, configure).DbContext;
}
