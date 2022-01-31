using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Stl.Redis;

public static class ServiceCollectionExt
{
    // Single RedisDb (resolved via RedisDb w/o TContext parameter)

    public static IServiceCollection AddRedisDb(this IServiceCollection services,
        Func<IServiceProvider, string> configurationFactory,
        string? keyPrefix = null)
    {
        services.AddSingleton(c => {
            var configuration = configurationFactory(c);
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb(this IServiceCollection services,
        string configuration,
        string? keyPrefix = null)
    {
        services.AddSingleton(_ => {
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb(
        this IServiceCollection services,
        ConfigurationOptions configuration,
        string? keyPrefix = null)
    {
        services.AddSingleton(_ => {
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb(this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer,
        string? keyPrefix = null)
    {
        services.AddSingleton(new RedisDb(connectionMultiplexer, keyPrefix));
        return services;
    }

    // Multiple RedisDb-s (resolved via RedisDb<TContext>)

    public static IServiceCollection AddRedisDb<TContext>(
        this IServiceCollection services,
        Func<IServiceProvider, string> configurationFactory,
        string? keyPrefix = null)
    {
        keyPrefix ??= typeof(TContext).Name;
        services.AddSingleton(c => {
            var configuration = configurationFactory(c);
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb<TContext>(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb<TContext>(
        this IServiceCollection services,
        string configuration,
        string? keyPrefix = null)
    {
        keyPrefix ??= typeof(TContext).Name;
        services.AddSingleton(_ => {
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb<TContext>(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb<TContext>(
        this IServiceCollection services,
        ConfigurationOptions configuration,
        string? keyPrefix = null)
    {
        keyPrefix ??= typeof(TContext).Name;
        services.AddSingleton(_ => {
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb<TContext>(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb<TContext>(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer,
        string? keyPrefix = null)
    {
        keyPrefix ??= typeof(TContext).Name;
        services.AddSingleton(new RedisDb<TContext>(connectionMultiplexer, keyPrefix));
        return services;
    }
}
