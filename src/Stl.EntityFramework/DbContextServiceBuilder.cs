using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.EntityFramework
{
    public readonly struct DbContextServiceBuilder<TDbContext>
        where TDbContext : DbContext
    {
        public IServiceCollection Services { get; }

        internal DbContextServiceBuilder(IServiceCollection services) => Services = services;

        public DbContextServiceBuilder<TDbContext> AddTransactionRunner()
            => this.AddTransactionRunner<DbOperation>();
        public DbContextServiceBuilder<TDbContext> AddTransactionRunner<TDbOperation>()
            where TDbOperation : class, IDbOperation, new()
        {
            Services.AddSingleton<
                IDbTransactionManager<TDbContext>,
                DbTransactionManager<TDbContext, TDbOperation>>();
            return this;
        }

        public DbContextServiceBuilder<TDbContext> AddEntityResolver<TKey, TEntity>()
            where TKey : notnull
            where TEntity : class
        {
            Services.AddSingleton<DbEntityResolver<TDbContext, TKey, TEntity>>();
            return this;
        }
    }
}
