using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.Operations;

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
            where TDbOperation : class, IOperation, new()
        {
            // Common services
            Services.TryAddSingleton<AgentInfo>();
            Services.TryAddSingleton<OperationCompletionNotifier.Options>();
            Services.TryAddSingleton<IOperationCompletionNotifier, OperationCompletionNotifier>();
            Services.TryAddSingleton<IDbOperationLog<TDbContext>, DbOperationLog<TDbContext, TDbOperation>>();
            // DbOperationLogWatcher - hosted service!
            Services.TryAddSingleton<DbOperationLogWatcher<TDbContext>.Options>();
            Services.TryAddSingleton<DbOperationLogWatcher<TDbContext>>();
            Services.AddHostedService(c => c.GetRequiredService<DbOperationLogWatcher<TDbContext>>());
            // DbOperationLogTrimmer - hosted service!
            Services.TryAddSingleton<DbOperationLogTrimmer<TDbContext>.Options>();
            Services.TryAddSingleton<DbOperationLogTrimmer<TDbContext>>();
            Services.AddHostedService(c => c.GetRequiredService<DbOperationLogTrimmer<TDbContext>>());
            // DbOperationScope & its CommandR handler
            Services.TryAddTransient<IDbOperationScope<TDbContext>, DbOperationScope<TDbContext>>();
            Services.TryAddSingleton<DbOperationScopeHandler<TDbContext>.Options>();
            Services.TryAddSingleton<DbOperationScopeHandler<TDbContext>>();
            Services.AddCommander().AddHandlers<DbOperationScopeHandler<TDbContext>>();
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
