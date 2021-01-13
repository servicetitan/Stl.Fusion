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

        public DbContextBuilder<TDbContext> AddOperations()
            => AddOperations<DbOperation>();
        public DbContextBuilder<TDbContext> AddOperations<TDbOperation>()
            where TDbOperation : class, IDbOperation, new()
        {
            Services.TryAddSingleton<AgentInfo>();
            Services.TryAddTransient<IDbOperationScope<TDbContext>, DbOperationScope<TDbContext>>();
            Services.TryAddSingleton<IDbOperationLogger<TDbContext>, DbOperationLogger<TDbContext, TDbOperation>>();
            Services.TryAddSingleton<DbOperationNotifier<TDbContext, TDbOperation>.Options>();
            Services.TryAddSingleton<IDbOperationNotifier<TDbContext>, DbOperationNotifier<TDbContext, TDbOperation>>();
            Services.TryAddSingleton<DbOperationInvalidationHandler<TDbContext>>();
            Services.AddHostedService(c => c.GetRequiredService<IDbOperationNotifier<TDbContext>>());
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
