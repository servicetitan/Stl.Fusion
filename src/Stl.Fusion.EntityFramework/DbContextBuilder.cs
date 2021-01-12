using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR;

namespace Stl.Fusion.EntityFramework
{
    public readonly struct DbContextBuilder<TDbContext>
        where TDbContext : DbContext
    {
        public IServiceCollection Services { get; }

        internal DbContextBuilder(IServiceCollection services) => Services = services;

        public DbContextBuilder<TDbContext> AddTransactionScope()
            => AddTransactionScope<DbOperation>();
        public DbContextBuilder<TDbContext> AddTransactionScope<TDbOperation>()
            where TDbOperation : class, IDbOperation, new()
        {
            Services.TryAddTransient<IDbTransactionScope<TDbContext>, DbTransactionScope<TDbContext>>();
            Services.TryAddSingleton<IDbOperationLogger<TDbContext>, DbOperationLogger<TDbContext, TDbOperation>>();
            return this;
        }

        public DbContextBuilder<TDbContext> AddEntityResolver<TKey, TEntity>()
            where TKey : notnull
            where TEntity : class
        {
            Services.TryAddSingleton<DbEntityResolver<TDbContext, TKey, TEntity>>();
            return this;
        }
    }
}
