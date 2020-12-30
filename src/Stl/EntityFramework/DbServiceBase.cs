using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Time;

namespace Stl.EntityFramework
{
    public abstract class DbServiceBase<TDbContext>
        where TDbContext : DbContext
    {
        protected IServiceProvider Services { get; }
        protected IDbContextFactory<TDbContext> DbContextFactory { get; }
        protected IDbTransactionRunner<TDbContext> Tx { get; }
        protected IMomentClock Clock { get; }

        protected DbServiceBase(IServiceProvider services)
        {
            Services = services;
            DbContextFactory = services.GetRequiredService<IDbContextFactory<TDbContext>>();
            Tx = services.GetRequiredService<IDbTransactionRunner<TDbContext>>();
            Clock = services.GetService<IMomentClock>() ?? SystemClock.Instance;
        }

        protected virtual TDbContext CreateDbContext(DbContextMode mode = DbContextMode.ReadOnly)
        {
            var dbContext = DbContextFactory.CreateDbContext();
            dbContext.ConfigureMode(mode);
            return dbContext;
        }
    }
}
