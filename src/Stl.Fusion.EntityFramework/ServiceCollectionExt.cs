using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework;

public static class ServiceCollectionExt
{
    public static DbContextBuilder<TDbContext> AddDbContextServices<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
        => new(services, null);

    public static IServiceCollection AddDbContextServices<TDbContext>(
        this IServiceCollection services,
        Action<DbContextBuilder<TDbContext>> configure)
        where TDbContext : DbContext
        => new DbContextBuilder<TDbContext>(services, configure).Services;

    // AddTransientDbContextFactory

    public static IServiceCollection AddTransientDbContextFactory<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? optionsAction)
        where TDbContext : DbContext
        => services.AddTransientDbContextFactory<TDbContext>((_, db) => optionsAction?.Invoke(db));

    public static IServiceCollection AddTransientDbContextFactory<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction)
        where TDbContext : DbContext
    {
        services.AddDbContext<TDbContext>(optionsAction, ServiceLifetime.Transient, ServiceLifetime.Singleton);
        services.AddSingleton<IDbContextFactory<TDbContext>>(
            c => new FuncDbContextFactory<TDbContext>(c.GetRequiredService<TDbContext>));
        return services;
    }
}
