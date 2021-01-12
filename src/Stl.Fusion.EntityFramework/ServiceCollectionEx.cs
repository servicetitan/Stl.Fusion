using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.EntityFramework
{
    public static class ServiceCollectionEx
    {
        public static DbContextBuilder<TDbContext> AddDbContextServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
            => new(services);

        public static IServiceCollection AddDbContextServices<TDbContext>(this IServiceCollection services, Action<DbContextBuilder<TDbContext>> configureDbContext)
            where TDbContext : DbContext
        {
            var dbContextServices = services.AddDbContextServices<TDbContext>();
            configureDbContext.Invoke(dbContextServices);
            return services;
        }
    }
}
