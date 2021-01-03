using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.EntityFramework
{
    public static class ServiceCollectionEx
    {
        public static DbContextServiceBuilder<TDbContext> AddDbContextServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
            => new(services);

        public static IServiceCollection AddDbContextServices<TDbContext>(this IServiceCollection services, Action<DbContextServiceBuilder<TDbContext>> configureDbContext)
            where TDbContext : DbContext
        {
            var dbContextServices = services.AddDbContextServices<TDbContext>();
            configureDbContext.Invoke(dbContextServices);
            return services;
        }

    }
}
