using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Stl.Redis;

namespace Stl.Fusion.EntityFramework.Redis;

public static class DbContextBuilderExt
{
    // AddRedisDb

    public static IServiceCollection AddRedisDb<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        Func<IServiceProvider, string> configurationFactory,
        string? keyPrefix = null)
        where TDbContext : DbContext
        => dbContextBuilder.Services.AddRedisDb<TDbContext>(configurationFactory, keyPrefix);

    public static IServiceCollection AddRedisDb<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        string configuration,
        string? keyPrefix = null)
        where TDbContext : DbContext
        => dbContextBuilder.Services.AddRedisDb<TDbContext>(configuration, keyPrefix);

    public static IServiceCollection AddRedisDb<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        ConfigurationOptions configuration,
        string? keyPrefix = null)
        where TDbContext : DbContext
        => dbContextBuilder.Services.AddRedisDb<TDbContext>(configuration, keyPrefix);

    public static IServiceCollection AddRedisDb<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        IConnectionMultiplexer connectionMultiplexer,
        string? keyPrefix = null)
        where TDbContext : DbContext
        => dbContextBuilder.Services.AddRedisDb<TDbContext>(connectionMultiplexer, keyPrefix);
}
