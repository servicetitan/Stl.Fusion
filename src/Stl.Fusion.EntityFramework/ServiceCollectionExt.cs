using Microsoft.EntityFrameworkCore;

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
}
