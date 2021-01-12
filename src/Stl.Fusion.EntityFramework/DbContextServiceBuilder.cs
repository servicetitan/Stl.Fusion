using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Fusion.EntityFramework
{
    public readonly struct DbContextServiceBuilder<TDbContext>
        where TDbContext : DbContext
    {
        public IServiceCollection Services { get; }

        internal DbContextServiceBuilder(IServiceCollection services) => Services = services;

        public DbContextServiceBuilder<TDbContext> AddDbContext()
        {
            Services.TryAddScoped(c => {
                var factory = c.GetRequiredService<IDbContextFactory<TDbContext>>();
                return factory.CreateDbContext();
            });
            return this;
        }

        public DbContextServiceBuilder<TDbContext> AddTransactionManager()
            => AddTransactionManager<DbOperation>();
        public DbContextServiceBuilder<TDbContext> AddTransactionManager<TDbOperation>()
            where TDbOperation : class, IDbOperation, new()
        {
            Services.TryAddSingleton<
                IDbTransactionManager<TDbContext>,
                DbTransactionManager<TDbContext, TDbOperation>>();
            return this;
        }

        public DbContextServiceBuilder<TDbContext> AddEntityResolver<TKey, TEntity>()
            where TKey : notnull
            where TEntity : class
        {
            Services.TryAddSingleton<DbEntityResolver<TDbContext, TKey, TEntity>>();
            return this;
        }
    }
}
